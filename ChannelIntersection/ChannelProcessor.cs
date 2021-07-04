using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable InconsistentlySynchronizedField

namespace ChannelIntersection
{
    public class ChannelProcessor
    {
        private static readonly HttpClient Http = new();
        private static readonly DateTime Timestamp = DateTime.UtcNow;
        private static readonly object DailyChatterLock = new();
        private static readonly object HalfHourlyChatterLock = new();

        private readonly string _psqlConnection;
        private readonly string _twitchClient;
        private readonly string _twitchToken;
        private AggregateFlags _flags;

        private Dictionary<string, Channel> _topChannels;
        private static Dictionary<string, HashSet<string>> _chatters;
        private readonly Dictionary<string, List<string>> _halfHourlyChatters = new();

        private const int MinChatters = 500;
        private const int MinViewers = 1000;
        private const int MinAggregateChatters = 1000;

        public ChannelProcessor(string psqlConnection, string twitchClient, string twitchToken)
        {
            _psqlConnection = psqlConnection;
            _twitchClient = twitchClient;
            _twitchToken = twitchToken;
        }

        public async Task Run()
        {
            Console.WriteLine($"starting channel processor at {Timestamp:u}");

            var sw = new Stopwatch();
            sw.Start();
            var totalSw = new Stopwatch();
            totalSw.Start();

            await GetFlags();
            Console.WriteLine("retrieving channels");
            await FetchChannels();
            Console.WriteLine($"retrieved {_topChannels.Count} channels in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            
            if (_flags.HasFlag(AggregateFlags.Hourly))
            {
                Console.WriteLine("beginning hourly calculation");
                Directory.CreateDirectory("chatters");
                var fileName = $"chatters/{Timestamp.Date.ToShortDateString()}.json";

                if (File.Exists(fileName))
                {
                    await using FileStream fs = File.OpenRead(fileName);
                    _chatters = await JsonSerializer.DeserializeAsync<Dictionary<string, HashSet<string>>>(fs) ?? new Dictionary<string, HashSet<string>>();
                }
                else
                {
                    _chatters = new Dictionary<string, HashSet<string>>();
                }

                sw.Restart();

                int previousSize = _chatters.Count;

                IEnumerable<Task> processTasks = _topChannels.Select(async channel =>
                {
                    (string _, Channel ch) = channel;
                    await GetChatters(ch);
                });

                await Task.WhenAll(processTasks);
                Console.WriteLine($"retrieved {_halfHourlyChatters.Count:N0} chatters\nsaved {_chatters.Count:N0} chatters (+{_chatters.Count - previousSize:N0}) in {sw.Elapsed.TotalSeconds}s");
                sw.Restart();

                await File.WriteAllBytesAsync(fileName, JsonSerializer.SerializeToUtf8Bytes(_chatters));
            }
            else // half hourly
            {
                Console.WriteLine("beginning half hourly calculation");
                IEnumerable<Task> processTasks = _topChannels.Select(async channel =>
                {
                    (string _, Channel ch) = channel;
                    await GetChatters(ch);
                });

                await Task.WhenAll(processTasks);
                Console.WriteLine($"retrieved {_halfHourlyChatters.Count} chatters in {sw.Elapsed.TotalSeconds}s");
                sw.Restart();
            }

            await using (var context = new TwitchContext(_psqlConnection))
            {
                var hh = new HalfHourly(context, _halfHourlyChatters, _topChannels, Timestamp);
                await hh.CalculateShared();
            }

            if (_flags.HasFlag(AggregateFlags.Daily))
            {
                await using var context = new TwitchContext(_psqlConnection);
                await using IDbContextTransaction trans = await context.Database.BeginTransactionAsync();
                var daily = new Daily(context, Timestamp);
                await daily.Aggregate();
                await trans.CommitAsync();
            }

            sw.Stop();
            Console.WriteLine($"total time taken: {totalSw.Elapsed.TotalSeconds}s");
        }

        private async Task GetFlags()
        {
            _flags = AggregateFlags.HalfHourly;
            var backup = false;

            if (Timestamp.Minute is 5 or 35)
            {
                await using (var context = new TwitchContext(_psqlConnection))
                {
                    DateTime lastUpdate = await context.Channels.AsNoTracking().MaxAsync(x => x.LastUpdate);

                    if (Timestamp - lastUpdate <= TimeSpan.FromMinutes(10))
                    {
                        Console.WriteLine("intersection already calculated, exiting.");
                        return;
                    }
                }

                backup = true;
                Console.WriteLine("latest calculation not found, starting backup calculation");
            }
            else if (Timestamp.Minute == 0)
            {
                _flags = AggregateFlags.Hourly;
            }

            if (!backup && (Timestamp - TimeSpan.FromMinutes(15)).Day != Timestamp.Day)
            {
                _flags = AggregateFlags.Daily;
            }
        }

        private async Task FetchChannels()
        {
            _topChannels = await GetTopChannels();
            await GetChannelAvatars(_topChannels);
        }

        private async Task<Dictionary<string, Channel>> GetTopChannels()
        {
            var channels = new Dictionary<string, Channel>();
            var newChannels = new Dictionary<string, Channel>();
            var pageToken = string.Empty;
            await using var context = new TwitchContext(_psqlConnection);
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

                        Channel dbChannel = await context.Channels.SingleOrDefaultAsync(x => x.LoginName == login);
                        if (dbChannel == null)
                        {
                            dbChannel = new Channel(login, channel.GetProperty("user_name").GetString(), channel.GetProperty("game_name").GetString(), viewerCount, Timestamp);
                            newChannels.TryAdd(login, dbChannel);
                        }
                        else
                        {
                            dbChannel.DisplayName = channel.GetProperty("user_name").GetString();
                            dbChannel.Game = channel.GetProperty("game_name").GetString();
                            dbChannel.Viewers = viewerCount;
                            dbChannel.LastUpdate = Timestamp;
                        }

                        channels.TryAdd(login, dbChannel);
                    }
                }
            } while (pageToken != null);

            await context.AddRangeAsync(newChannels.Values);
            await context.SaveChangesAsync();
            return channels;
        }

        private async Task GetChannelAvatars(Dictionary<string, Channel> channels)
        {
            using var http = new HttpClient();
            foreach (string reqString in Helper.RequestBuilder(channels.Keys))
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

        private async Task GetChatters(Channel channel)
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
            if (chatters >= MinAggregateChatters && _flags.HasFlag(AggregateFlags.Hourly))
            {
                IterateHourly(channel, response.RootElement);
            }
            else
            {
                IterateHalfHourly(channel, response.RootElement);
            }
        }

        private void IterateHalfHourly(Channel channel, JsonElement root)
        {
            JsonElement.ObjectEnumerator viewerTypes = root.GetProperty("chatters").EnumerateObject();
            foreach (JsonProperty viewerType in viewerTypes)
            {
                foreach (JsonElement viewer in viewerType.Value.EnumerateArray())
                {
                    string username = viewer.GetString()?.ToLower();
                    if (username == null || username.EndsWith("bot", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    lock (HalfHourlyChatterLock)
                    {
                        if (!_halfHourlyChatters.ContainsKey(username))
                        {
                            _halfHourlyChatters.TryAdd(username, new List<string> {channel.LoginName});
                        }
                        else
                        {
                            _halfHourlyChatters[username].Add(channel.LoginName);
                        }
                    }
                }
            }
        }

        private void IterateHourly(Channel channel, JsonElement root)
        {
            JsonElement.ObjectEnumerator viewerTypes = root.GetProperty("chatters").EnumerateObject();
            foreach (JsonProperty viewerType in viewerTypes)
            {
                foreach (JsonElement viewer in viewerType.Value.EnumerateArray())
                {
                    string username = viewer.GetString()?.ToLower();
                    if (username == null || username.EndsWith("bot", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    lock (DailyChatterLock)
                    {
                        if (!_chatters.ContainsKey(username))
                        {
                            _chatters.TryAdd(username, new HashSet<string> {channel.LoginName});
                        }
                        else
                        {
                            _chatters[username].Add(channel.LoginName);
                        }
                    }

                    lock (HalfHourlyChatterLock)
                    {
                        if (!_halfHourlyChatters.ContainsKey(username))
                        {
                            _halfHourlyChatters.TryAdd(username, new List<string> {channel.LoginName});
                        }
                        else
                        {
                            _halfHourlyChatters[username].Add(channel.LoginName);
                        }
                    }
                }
            }
        }
    }
}