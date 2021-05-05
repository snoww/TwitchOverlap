using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TwitchGraph.Models;

namespace TwitchGraph
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

            Dictionary<string, Channel> channels = await GetTopChannels();

            Console.WriteLine($"retrieved {channels.Count} channels in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var channelChatters = new ConcurrentDictionary<Channel, HashSet<string>>();
            var processed = new ConcurrentDictionary<Channel, ConcurrentDictionary<string, int>>();
            var totalIntersectionCount = new ConcurrentDictionary<Channel, ConcurrentDictionary<string, byte>>();

            IEnumerable<Task> processTasks = channels.Select(async channel =>
            {
                (_, Channel ch) = channel;
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

            Parallel.ForEach(GetKCombs(new List<Channel>(channelChatters.Keys), 2), x =>
            {
                Channel[] pair = x.ToArray();
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

            var nodesAdd = new List<Node>();
            var nodesUpdate = new List<Node>();
            var edgesAdd = new List<Edge>();
            var edgesUpdate = new List<Edge>();
            
            foreach ((Channel ch, ConcurrentDictionary<string, int> value) in processed)
            {
                Node node = await dbContext.Nodes.SingleOrDefaultAsync(x => x.Id == ch.Id);
                if (node == null)
                {
                    nodesAdd.Add(new Node(ch.Id, ch.DisplayName, ch.Chatters));
                }
                else
                {
                    node.Size += ch.Chatters;
                    nodesUpdate.Add(node);
                }

                foreach ((string channel2, int overlap) in value)
                {
                    Edge edge = await dbContext.Edges.SingleOrDefaultAsync(x => x.Source == ch.Id && x.Target == channel2);
                    if (edge == null)
                    {
                        edgesAdd.Add(new Edge(ch.Id, channel2, overlap));
                    }
                    else
                    {
                        edge.Weight += overlap;
                        edgesUpdate.Add(edge);
                    }
                }
            }

            await dbContext.Nodes.AddRangeAsync(nodesAdd);
            dbContext.Nodes.UpdateRange(nodesUpdate);
            await dbContext.Edges.AddRangeAsync(edgesAdd);
            dbContext.Edges.UpdateRange(edgesUpdate);

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"inserted into database in {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            DateTime endTime = DateTime.UtcNow;
            Console.WriteLine($"time taken: {(endTime - timestamp).Seconds}s");
        }

        private static async Task<HashSet<string>> GetChatters(Channel channel)
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

        private static async Task<Dictionary<string, Channel>> GetTopChannels()
        {
            var channels = new Dictionary<string, Channel>();

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
                        channels.TryAdd(id, new Channel(id, channel.GetProperty("user_name").GetString()));
                    }
                }
            } while (pageToken != null);

            return channels;
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