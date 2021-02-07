using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace ChannelIntersection
{
    public static class Program
    {
        private static readonly HttpClient Http = new();
        private static string _twitchToken;
        private static string _twitchClient;
        private static string _mongodbConnection;
        private static IMongoCollection<ChannelModel> _channelCollection;

        public static async Task Main(string[] args)
        {
            using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            {
                _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
                _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
                _mongodbConnection = json.RootElement.GetProperty("MONGODB").GetString();
            }
            
            DateTime timestamp = DateTime.UtcNow;

            var conventions = new ConventionPack {new LowerCaseElementNameConvention()};
            ConventionRegistry.Register("LowerCaseElementName", conventions, _ => true);
            var client = new MongoClient(_mongodbConnection);
            IMongoDatabase db = client.GetDatabase("twitch");
            _channelCollection = db.GetCollection<ChannelModel>("channels");
            Console.WriteLine("connected to database");

            var sw = new Stopwatch();
            sw.Start();

            List<ChannelModel> channels = await GetTopChannels();
            
            Console.WriteLine($"retrieved {channels.Count} channels in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var channelChatters = new ConcurrentDictionary<ChannelModel, HashSet<string>>();
            var processed = new ConcurrentDictionary<ChannelModel, ConcurrentDictionary<string, int>>();
            var totalIntersectionCount = new ConcurrentDictionary<ChannelModel, ConcurrentDictionary<string, byte>>();
            
            IEnumerable<Task> processTasks = channels.Select(async channel =>
            {
                channelChatters.TryAdd(channel, await GetChatters(channel));
                processed.TryAdd(channel, new ConcurrentDictionary<string, int>());
                totalIntersectionCount.TryAdd(channel, new ConcurrentDictionary<string, byte>());
            });

            await Task.WhenAll(processTasks);
            
            Console.WriteLine($"retrieved chatters in {sw.ElapsedMilliseconds}ms");
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

            var updateOptions = new ReplaceOptions{IsUpsert = true};
            
            IEnumerable<Task> insertTasks = processed.Select(async channel =>
            {
                (ChannelModel ch, ConcurrentDictionary<string, int> value) = channel;
                ch.Timestamp = timestamp;
                ch.Data = new Dictionary<string, int>(value);
                ch.Overlaps = totalIntersectionCount[ch].Count;
                await _channelCollection.ReplaceOneAsync(x => x.Id == ch.Id, ch, updateOptions);
            });
            
            await Task.WhenAll(insertTasks);
            
            Console.WriteLine($"inserted into database in {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            DateTime endTime = DateTime.UtcNow;
            Console.WriteLine($"time taken: {(endTime - timestamp).Seconds}s");
        }

        private static async Task<HashSet<string>> GetChatters(ChannelModel channelName)
        {
            using JsonDocument response = await JsonDocument.ParseAsync(
                await Http.GetStreamAsync($"http://tmi.twitch.tv/group/user/{channelName.Id}/chatters"));
            
            int chatters = response.RootElement.GetProperty("chatter_count").GetInt32();
            channelName.Chatters = chatters;
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
        
        private static async Task<List<ChannelModel>> GetTopChannels()
        {
            var channels = new List<ChannelModel>();
            
            var pageToken = string.Empty;
            do
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                request.Headers.Add("Client-Id", _twitchClient);
                
                if (string.IsNullOrWhiteSpace(pageToken))
                {
                    request.RequestUri = new Uri("https://api.twitch.tv/helix/streams?first=100");
                }
                else
                {
                    request.RequestUri = new Uri($"https://api.twitch.tv/helix/streams?first=100&after={pageToken}");
                }
                
                using HttpResponseMessage response = await Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    pageToken = json.RootElement.GetProperty("pagination").GetProperty("cursor").GetString();
                    JsonElement.ArrayEnumerator channelEnumerator = json.RootElement.GetProperty("data").EnumerateArray();
                    foreach (JsonElement channel in channelEnumerator)
                    {
                        int viewerCount = channel.GetProperty("viewer_count").GetInt32();
                        if (viewerCount < 1000)
                        {
                            pageToken = null;
                            break;
                        }

                        string username = channel.GetProperty("user_name").GetString()?.ToLower();
                        if (!Regex.IsMatch(username!, "^[a-zA-Z0-9_]*$"))
                        {
                            using var userRequest = new HttpRequestMessage();
                            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                            userRequest.Headers.Add("Client-Id", _twitchClient);
                            userRequest.RequestUri = new Uri($"https://api.twitch.tv/helix/users?id={channel.GetProperty("user_id").GetString()}");
                            using HttpResponseMessage userResponse = await Http.SendAsync(userRequest);
                            JsonDocument userJson = await JsonDocument.ParseAsync(await userResponse.Content.ReadAsStreamAsync());
                            username = userJson.RootElement.GetProperty("data")[0].GetProperty("login").GetString()?.ToLower();
                        }

                        channels.Add(new ChannelModel
                        {
                            Id = username?.ToLower(),
                            Game = channel.GetProperty("game_name").GetString(),
                            Viewers = viewerCount
                        });
                    }
                }
            } while (pageToken != null);

            return channels;
        }

        private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0), 
                    (t1, t2) => t1.Concat(new[] { t2 }));
        }
    }
    
    public class LowerCaseElementNameConvention : IMemberMapConvention 
    {
        public void Apply(BsonMemberMap memberMap) 
        {
            memberMap.SetElementName(memberMap.MemberName.ToLower());
        }

        public string Name => "LowerCaseElementNameConvention";
    }
}