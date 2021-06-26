using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
// ReSharper disable InconsistentlySynchronizedField

namespace TwitchMatrix
{
    public static class Program
    {
        private static readonly HttpClient Http = new();
        private static Dictionary<string, HashSet<string>> _chatters;
        private static readonly Dictionary<string, HashSet<string>> HalfHourlyChatters = new();
        private static string _twitchToken;
        private static string _twitchClient;
        private static string _psqlConnection;

        private static readonly object WriteLock = new();
        private static AggregateFlags _flags;
        private static TwitchContext _context;
        private static DateTime _timestamp;

        private const int MinChatters = 500;
        private const int MinViewers = 1000;


        public static async Task Main(string[] args)
        {
            using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            {
                _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
                _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
                _psqlConnection = json.RootElement.GetProperty("POSTGRES").GetString();
            }

            _context = new TwitchContext(_psqlConnection);
            IDbContextTransaction trans = await _context.Database.BeginTransactionAsync();

            _timestamp = DateTime.UtcNow;
            Console.WriteLine($"starting twitch matrix at {_timestamp:u}");

            _flags = AggregateFlags.HalfHourly;
            var backup = false;

            if (_timestamp.Minute is 5 or 35)
            {
                DateTime lastUpdate = await _context.Channels.AsNoTracking().MaxAsync(x => x.LastUpdate);

                if (_timestamp - lastUpdate <= TimeSpan.FromMinutes(10))
                {
                    Console.WriteLine("intersection already calculated, exiting.");
                    return;
                }

                backup = true;
                Console.WriteLine("latest calculation not found, starting backup calculation");
            }
            else if (_timestamp.Minute == 0)
            {
                _flags = AggregateFlags.Hourly;
            }
            
            if (!backup && (_timestamp - TimeSpan.FromMinutes(15)).Day != _timestamp.Day)
            {
                _flags = AggregateFlags.Daily;
            }

            var sw = new Stopwatch();
            sw.Start();
            var totalSw = new Stopwatch();
            totalSw.Start();

            Dictionary<string, Channel> topChannels = await GetTopChannels();
            await GetChannelAvatars(topChannels);

            Console.WriteLine($"retrieved {topChannels.Count} channels in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            if (_flags.HasFlag(AggregateFlags.Hourly))
            {
                Console.WriteLine("beginning hourly calculation");
                Directory.CreateDirectory("chatters");
                var fileName = $"chatters/{_timestamp.Date.ToShortDateString()}.json";

                if (File.Exists(fileName))
                {
                    await using FileStream fs = File.OpenRead(fileName);
                    _chatters = await JsonSerializer.DeserializeAsync<Dictionary<string, HashSet<string>>>(fs) ?? new Dictionary<string, HashSet<string>>();
                }
                else
                {
                    _chatters = new Dictionary<string, HashSet<string>>();
                }

                int previousSize = _chatters.Count;

                IEnumerable<Task> processTasks = topChannels.Select(async channel =>
                {
                    (string _, Channel ch) = channel;
                    await GetChattersHourly(ch);
                });

                await Task.WhenAll(processTasks);
                Console.WriteLine($"retrieved {HalfHourlyChatters.Count:N0} chatters\nsaved {_chatters.Count:N0} chatters (+{_chatters.Count - previousSize:N0}) in {sw.Elapsed.TotalSeconds}s");
                sw.Restart();
                
                await File.WriteAllBytesAsync(fileName, JsonSerializer.SerializeToUtf8Bytes(_chatters));
            }
            else // half hourly
            {
                Console.WriteLine("beginning half hourly calculation");
                IEnumerable<Task> processTasks = topChannels.Select(async channel =>
                {
                    (string _, Channel ch) = channel;
                    await GetChatters(ch);
                });

                await Task.WhenAll(processTasks);
                Console.WriteLine($"retrieved {HalfHourlyChatters.Count} chatters in {sw.Elapsed.TotalSeconds}s");
                sw.Restart();
            }

            var hh = new HalfHourly(_context, HalfHourlyChatters, topChannels, _timestamp);
            await hh.CalculateShared();
            await trans.CommitAsync();
            // dispose context and transaction, so we can safely create new instance
            await _context.DisposeAsync();
            await trans.DisposeAsync();

            if (_flags.HasFlag(AggregateFlags.Daily))
            {
                // new context for daily aggregation
                _context = new TwitchContext(_psqlConnection);
                trans = await _context.Database.BeginTransactionAsync();
                var daily = new Daily(_context, _timestamp);
                await daily.Aggregate();
                await trans.CommitAsync();
                
                await _context.DisposeAsync();
                await trans.DisposeAsync();
            }

            sw.Stop();
            Console.WriteLine($"total time taken: {totalSw.Elapsed.TotalSeconds}s");
        }

        private static async Task<Dictionary<string, Channel>> GetTopChannels()
        {
            var channels = new Dictionary<string, Channel>();
            var newChannels = new Dictionary<string, Channel>();
            var pageToken = string.Empty;
            do
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                request.Headers.Add("Client-Id", _twitchClient);
                request.RequestUri = string.IsNullOrWhiteSpace(pageToken)
                    ? new Uri("https://api.twitch.tv/helix/streams?first=100")
                    : new Uri($"https://api.twitch.tv/helix/streams?first=100&after={pageToken}");

                using HttpResponseMessage response = await Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    pageToken = json.RootElement.GetProperty("pagination").GetProperty("cursor").GetString();
                    JsonElement.ArrayEnumerator channelEnumerator = json.RootElement.GetProperty("data").EnumerateArray();
                    foreach (JsonElement channel in channelEnumerator)
                    {
                        int viewerCount = channel.GetProperty("viewer_count").GetInt32();
                        if (viewerCount < MinViewers)
                        {
                            pageToken = null;
                            break;
                        }

                        string login = channel.GetProperty("user_login").GetString()?.ToLowerInvariant();
                        Channel dbChannel = await _context.Channels.SingleOrDefaultAsync(x => x.LoginName == login);
                        if (dbChannel == null)
                        {
                            dbChannel = new Channel(login, channel.GetProperty("user_name").GetString(), channel.GetProperty("game_name").GetString(), viewerCount, _timestamp);
                            newChannels.TryAdd(login, dbChannel);
                        }
                        else
                        {
                            dbChannel.DisplayName = channel.GetProperty("user_name").GetString();
                            dbChannel.Game = channel.GetProperty("game_name").GetString();
                            dbChannel.Viewers = viewerCount;
                            dbChannel.LastUpdate = _timestamp;
                        }

                        channels.TryAdd(login, dbChannel);
                    }
                }
            } while (pageToken != null);

            await _context.AddRangeAsync(newChannels.Values);
            await _context.SaveChangesAsync();
            return channels;
        }

        private static async Task GetChatters(Channel channel)
        {
            Stream stream;
            try
            {
                stream = await Http.GetStreamAsync($"https://tmi.twitch.tv/group/user/{channel.LoginName}/chatters");
            }
            catch
            {
                Console.WriteLine($"Could not retrieve chatters for {channel.LoginName}");
                return;
            }

            using JsonDocument response = await JsonDocument.ParseAsync(stream);
            await stream.DisposeAsync();

            int chatters = response.RootElement.GetProperty("chatter_count").GetInt32();
            if (chatters < MinChatters)
            {
                return;
            }

            channel.Chatters = chatters;
            JsonElement.ObjectEnumerator viewerTypes = response.RootElement.GetProperty("chatters").EnumerateObject();
            foreach (JsonProperty viewerType in viewerTypes)
            {
                foreach (JsonElement viewer in viewerType.Value.EnumerateArray())
                {
                    string username = viewer.GetString()?.ToLower();
                    if (username == null || username.EndsWith("bot", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    lock (WriteLock)
                    {
                        if (!HalfHourlyChatters.ContainsKey(username))
                        {
                            HalfHourlyChatters.TryAdd(username, new HashSet<string> {channel.LoginName});
                        }
                        else
                        {
                            HalfHourlyChatters[username].Add(channel.LoginName);
                        }
                    }
                }
            }
        }

        private static async Task GetChattersHourly(Channel channel)
        {
            Stream stream;
            try
            {
                stream = await Http.GetStreamAsync($"https://tmi.twitch.tv/group/user/{channel.LoginName}/chatters");
            }
            catch
            {
                Console.WriteLine($"Could not retrieve chatters for {channel.LoginName}");
                return;
            }

            using JsonDocument response = await JsonDocument.ParseAsync(stream);
            await stream.DisposeAsync();

            int chatters = response.RootElement.GetProperty("chatter_count").GetInt32();
            if (chatters < 500)
            {
                return;
            }

            channel.Chatters = chatters;
            JsonElement.ObjectEnumerator viewerTypes = response.RootElement.GetProperty("chatters").EnumerateObject();
            foreach (JsonProperty viewerType in viewerTypes)
            {
                foreach (JsonElement viewer in viewerType.Value.EnumerateArray())
                {
                    string username = viewer.GetString()?.ToLower();
                    if (username == null || username.EndsWith("bot", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    lock (WriteLock)
                    {
                        if (!_chatters.ContainsKey(username))
                        {
                            _chatters.TryAdd(username, new HashSet<string> {channel.LoginName});
                        }
                        else
                        {
                            _chatters[username].Add(channel.LoginName);
                        }

                        if (!HalfHourlyChatters.ContainsKey(username))
                        {
                            HalfHourlyChatters.TryAdd(username, new HashSet<string> {channel.LoginName});
                        }
                        else
                        {
                            HalfHourlyChatters[username].Add(channel.LoginName);
                        }
                    }
                }
            }
        }


        private static async Task GetChannelAvatars(Dictionary<string, Channel> channels)
        {
            using var http = new HttpClient();
            foreach (string reqString in RequestBuilder(channels.Keys.ToList()))
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                request.Headers.Add("Client-Id", _twitchClient);
                request.RequestUri = new Uri($"https://api.twitch.tv/helix/users?{reqString}");
                using HttpResponseMessage response = await http.SendAsync(request);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                JsonElement.ArrayEnumerator data = json.RootElement.GetProperty("data").EnumerateArray();
                foreach (JsonElement channel in data)
                {
                    Channel model = channels[channel.GetProperty("login").GetString()!.ToLowerInvariant()];
                    if (model != null) model.Avatar = channel.GetProperty("profile_image_url").GetString()?.Replace("-300x300", "-70x70").Split('/')[4];
                }
            }
        }

        private static IEnumerable<string> RequestBuilder(IReadOnlyCollection<string> channels)
        {
            var shards = (int) Math.Ceiling(channels.Count / 100.0);
            var list = new List<string>(shards);
            var request = new StringBuilder();
            for (int i = 0; i < shards; i++)
            {
                foreach (string channel in channels.Skip(i * 100).Take(100))
                {
                    request.Append("&login=").Append(channel);
                }

                list.Add(request.ToString()[1..]);
                request.Clear();
            }

            return list;
        }
    }

    [Flags]
    public enum AggregateFlags
    {
        HalfHourly = 1,
        Hourly = 2 | HalfHourly,
        Daily = 4 | Hourly,
    }
}