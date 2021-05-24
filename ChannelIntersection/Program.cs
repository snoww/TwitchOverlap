using System;
using System.Collections.Concurrent;
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
using Z.EntityFramework.Plus;

namespace ChannelIntersection
{
    public static class Program
    {
        private static readonly HttpClient Http = new();
        private static string _twitchToken;
        private static string _twitchClient;
        private static string _psqlConnection;

        public static async Task Main()
        {
            using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            {
                _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
                _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
                _psqlConnection = json.RootElement.GetProperty("POSTGRES").GetString();
            }

            DateTime timestamp = DateTime.UtcNow;

            var dbContext = new TwitchContext(_psqlConnection);

            Console.WriteLine($"connected to database at {timestamp:u}");

            var sw = new Stopwatch();
            sw.Start();

            Dictionary<string, ChannelModel> channels = await GetTopChannels();
            await GetChannelAvatar(channels);

            Console.WriteLine($"retrieved {channels.Count} channels in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var channelChatters = new ConcurrentDictionary<ChannelModel, HashSet<string>>();
            var processed = new ConcurrentDictionary<ChannelModel, ConcurrentDictionary<string, int>>();
            var totalIntersectionCount = new ConcurrentDictionary<ChannelModel, ConcurrentDictionary<string, byte>>();

            IEnumerable<Task> processTasks = channels.Select(async channel =>
            {
                (_, ChannelModel ch) = channel;
                HashSet<string> chatters = await GetChatters(ch);
                if (chatters == null)
                {
                    return;
                }

                channelChatters.TryAdd(ch, chatters);
                processed.TryAdd(ch, new ConcurrentDictionary<string, int>());
                totalIntersectionCount.TryAdd(ch, new ConcurrentDictionary<string, byte>());
            });

            await Task.WhenAll(processTasks);

            Console.WriteLine($"retrieved {channelChatters.Count} chatters in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var rootPath = $"./channel-chatters/{timestamp.Month}-{timestamp.Year}";
            Directory.CreateDirectory(rootPath);
            
            IEnumerable<Task> ccTasks = channelChatters.Select(async cc =>
            {
                (ChannelModel channel, HashSet<string> chatters) = cc;
                var path = $"{rootPath}/{channel.Id}.txt";
                if (!File.Exists(path))
                {
                    await File.WriteAllLinesAsync(path, chatters);
                    return;
                }
                
                var existingChatters = new HashSet<string>(await File.ReadAllLinesAsync(path));
                existingChatters.UnionWith(chatters);
                await File.WriteAllLinesAsync(path, existingChatters);
            });

            await Task.WhenAll(ccTasks);
            
            Console.WriteLine($"union completed in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            Parallel.ForEach(GetKCombs(new List<ChannelModel>(channelChatters.Keys), 2), x =>
            {
                ChannelModel[] pair = x.ToArray();
                int count = channelChatters[pair[0]].Count(y =>
                {
                    if (channelChatters[pair[1]].Contains(y))
                    {
                        totalIntersectionCount[pair[0]].TryAdd(y, byte.MaxValue);
                        totalIntersectionCount[pair[1]].TryAdd(y, byte.MaxValue);
                        return true;
                    }

                    return false;
                });
                processed[pair[0]].TryAdd(pair[1].Id, count);
                processed[pair[1]].TryAdd(pair[0].Id, count);
            });

            Console.WriteLine($"calculated intersection in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var channelAddBag = new ConcurrentBag<Channel>();
            var channelUpdateBag = new ConcurrentBag<Channel>();
            var dataBag = new ConcurrentBag<Overlap>();
            
            foreach ((ChannelModel ch, ConcurrentDictionary<string, int> value) in processed)
            {
                Channel dbChannel = await dbContext.Channels.SingleOrDefaultAsync(x => x.Id == ch.Id);
                if (dbChannel == null)
                {
                    channelAddBag.Add(new Channel(ch.Id, ch.Game, ch.Viewers, ch.Chatters, totalIntersectionCount[ch].Count, timestamp, ch.Avatar, ch.DisplayName));
                }
                else
                {
                    dbChannel.Avatar = ch.Avatar;
                    dbChannel.DisplayName = ch.DisplayName;
                    dbChannel.Game = ch.Game;
                    dbChannel.Viewers = ch.Viewers;
                    dbChannel.Chatters = ch.Chatters;
                    dbChannel.Shared = totalIntersectionCount[ch].Count;
                    dbChannel.LastUpdate = timestamp;
                    channelUpdateBag.Add(dbChannel);
                }

                dataBag.Add(new Overlap(ch.Id, timestamp, new Dictionary<string, int>(value)));
            }

            await dbContext.Channels.AddRangeAsync(channelAddBag);
            dbContext.Channels.UpdateRange(channelUpdateBag);
            await dbContext.Overlaps.AddRangeAsync(dataBag);

            // await dbContext.SaveChangesAsync();

            DateTime thirtyDays = timestamp.AddDays(-30);
            // await dbContext.Overlaps.Where(x => x.Timestamp <= thirtyDays).DeleteAsync();

            Console.WriteLine($"inserted into database in {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            DateTime endTime = DateTime.UtcNow;
            Console.WriteLine($"time taken: {(endTime - timestamp).Seconds}s");
        }

        private static async Task<HashSet<string>> GetChatters(ChannelModel channel)
        {
            Stream stream;
            try
            {
                stream = await Http.GetStreamAsync($"https://tmi.twitch.tv/group/user/{channel.Id}/chatters");
            }
            catch
            {
                Console.WriteLine($"Could not retrieve chatters for {channel.Id}");
                return null;
            }
            
            using JsonDocument response = await JsonDocument.ParseAsync(stream);
            await stream.DisposeAsync();

            int chatters = response.RootElement.GetProperty("chatter_count").GetInt32();
            if (chatters < 100)
            {
                return null;
            }

            channel.Chatters = chatters;
            var chatterList = new HashSet<string>(chatters);
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

                    chatterList.Add(username);
                }
            }

            return chatterList;
        }

        private static async Task<Dictionary<string, ChannelModel>> GetTopChannels()
        {
            var channels = new Dictionary<string, ChannelModel>();

            var pageToken = string.Empty;
            do
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                request.Headers.Add("Client-Id", _twitchClient);
                request.RequestUri = string.IsNullOrWhiteSpace(pageToken) ? new Uri("https://api.twitch.tv/helix/streams?first=100") : new Uri($"https://api.twitch.tv/helix/streams?first=100&after={pageToken}");

                using HttpResponseMessage response = await Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    pageToken = json.RootElement.GetProperty("pagination").GetProperty("cursor").GetString();
                    JsonElement.ArrayEnumerator channelEnumerator = json.RootElement.GetProperty("data").EnumerateArray();
                    foreach (JsonElement channel in channelEnumerator)
                    {
                        int viewerCount = channel.GetProperty("viewer_count").GetInt32();
                        if (viewerCount < 1500)
                        {
                            pageToken = null;
                            break;
                        }

                        string id = channel.GetProperty("user_login").GetString()?.ToLowerInvariant();

                        channels.TryAdd(id, new ChannelModel
                        {
                            Id = id,
                            DisplayName = channel.GetProperty("user_name").GetString(),
                            Game = channel.GetProperty("game_name").GetString(),
                            Viewers = viewerCount
                        });
                    }
                }
            } while (pageToken != null);

            return channels;
        }

        private static async Task GetChannelAvatar(Dictionary<string, ChannelModel> channels)
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
                    ChannelModel model = channels[channel.GetProperty("login").GetString()!.ToLowerInvariant()];
                    if (model != null) model.Avatar = channel.GetProperty("profile_image_url").GetString()?.Replace("-300x300", "-70x70").Split('/')[4];
                }
            }
        }

        private static IEnumerable<string> RequestBuilder(IReadOnlyCollection<string> channels)
        {
            var shards = (int) Math.Ceiling(channels.Count / 100.0);
            var list = new List<string>(shards);
            for (int i = 0; i < shards; i++)
            {
                var request = new StringBuilder();
                foreach (string channel in channels.Skip(i * 100).Take(100))
                {
                    request.Append("&login=").Append(channel);
                }

                list.Add(request.ToString()[1..]);
            }

            return list;
        }

        private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new[] {t});
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new[] {t2}));
        }
    }
}