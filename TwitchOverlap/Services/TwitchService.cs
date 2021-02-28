using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using StackExchange.Redis;
using TwitchOverlap.Models;
using TwitchOverlapApi.Models;

namespace TwitchOverlap.Services
{
public class TwitchService
    {
        private readonly IMongoCollection<Channel> _channels;
        private readonly IDatabase _cache;
        private readonly IHttpClientFactory _factory;
        private readonly Random _random = new();
        private static string _twitchToken;
        private static string _twitchClient;

        private const string GameCacheKey = "channel:games";
        private const string ChannelCacheKey = "channel:list";
        
        public TwitchService(ITwitchDatabaseSettings settings, IRedisCache cache, IHttpClientFactory factory)
        {
            _cache = cache.Redis.GetDatabase();
            _factory = factory;
            var conventions = new ConventionPack {new LowerCaseElementNameConvention()};
            ConventionRegistry.Register("LowerCaseElementName", conventions, _ => true);
            var client = new MongoClient(settings.ConnectionString);
            IMongoDatabase database = client.GetDatabase(settings.DatabaseName);
            _channels = database.GetCollection<Channel>(settings.CollectionName);
            
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText("config.json"));
            _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
            _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
        }
        
        public async Task<List<ChannelSummary>> Get()
        {
            string channelListJson = await _cache.StringGetAsync(ChannelCacheKey);
            List<ChannelSummary> channelList;
            if (!string.IsNullOrEmpty(channelListJson))
            {
                channelList = JsonSerializer.Deserialize<List<ChannelSummary>>(channelListJson);
            }
            else
            {
                DateTime latestHalfHour = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(35));
                channelList = await _channels.Find(x => x.Timestamp >= latestHalfHour)
                    .Project(x => new ChannelSummary {Id = x.Id, Chatters = x.Chatters})
                    .ToListAsync();
            }
            
            if (channelList == null)
            {
                return null;
            }

            List<ChannelSummary> channelSummaries = await GetChannelData(channelList);
            
            await _cache.StringSetAsync(ChannelCacheKey, JsonSerializer.Serialize(channelSummaries), TimeSpan.FromMinutes(5));
            
            return channelSummaries.OrderByDescending(x => x.Chatters).ToList();
        }

        public async Task<ChannelProjection> Get(string name)
        {
            string cacheKey = name.ToLowerInvariant();

            string channelJson = await _cache.StringGetAsync($"channel:{cacheKey}");

            if (!string.IsNullOrEmpty(channelJson))
            {
                return JsonSerializer.Deserialize<ChannelProjection>(channelJson);
            }

            Channel channel = await _channels.Find(x => x.Id == cacheKey).FirstOrDefaultAsync();
            if (channel == null)
            {
                return null;
            }

            List<ChannelGames> channelGames = await GetChannelGames();

            ChannelProjection channelProjection = ProjectChannelDataWithGames(channel, channelGames);
            
            string displayName = await _cache.StringGetAsync($"channel:{channel.Id}:display");
            string avatar = await _cache.StringGetAsync($"channel:{channel.Id}:avatar");
            if (displayName != null && avatar != null)
            {
                channelProjection.DisplayName = displayName;
                channelProjection.Avatar = avatar;
            }
            else
            {
                var cs = new ChannelSummary{Id = channelProjection.Id};
                await GetChannelData(new List<ChannelSummary> {cs});
                channelProjection.DisplayName = await _cache.StringGetAsync($"channel:{channel.Id}:display");
                channelProjection.Avatar = await _cache.StringGetAsync($"channel:{channel.Id}:avatar");
            }
            
            await _cache.StringSetAsync($"channel:{cacheKey}", JsonSerializer.Serialize(channelProjection), TimeSpan.FromMinutes(5));
            
            return channelProjection;
        }

        private async Task<List<ChannelGames>> GetChannelGames()
        {
            string channelGames = await _cache.StringGetAsync(GameCacheKey);
            if (!string.IsNullOrEmpty(channelGames))
            {
                return JsonSerializer.Deserialize<List<ChannelGames>>(channelGames);
            }
            
            List<ChannelGames> games = await _channels.Find(x => x.Game != null)
                .Project(x => new ChannelGames {Id = x.Id, Game = x.Game})
                .ToListAsync();

            if (games == null)
            {
                return null;
            }

            await _cache.StringSetAsync(GameCacheKey, JsonSerializer.Serialize(games), TimeSpan.FromMinutes(5));

            return games;
        }

        private static ChannelProjection ProjectChannelDataWithGames(Channel channel, IReadOnlyCollection<ChannelGames> channelGamesList)
        {
            var channelProjection = new ChannelProjection
            {
                Id = channel.Id,
                Timestamp = channel.Timestamp,
                Game = channel.Game,
                Viewers = channel.Viewers,
                Overlaps = channel.Overlaps,
                Chatters = channel.Chatters,
                Avatar = channel.Avatar,
            };

            var data = new ConcurrentDictionary<string, Data>();

            Parallel.ForEach(channel.Data, x =>
            {
                data.TryAdd(x.Key, new Data{Shared = x.Value, Game = channelGamesList.FirstOrDefault(y => y.Id == x.Key)?.Game});
            });

            channelProjection.Data = new Dictionary<string, Data>(data);
            (List<string> history, Dictionary<string, List<int?>> dataPoints) = ProjectHistory(channel.History);
            channelProjection.History = history;
            channelProjection.OverlapPoints = dataPoints;
            
            return channelProjection;
        }

        private static (List<string> times, Dictionary<string, List<int?>> dataPoints) ProjectHistory(List<Dictionary<string, Dictionary<string, int>>> history)
        {
            if (history == null)
            {
                return (null, null);
            }
            var times = new List<string>();
            var dataPoints = new Dictionary<string, List<int?>>();
            List<int?> first = null;
            foreach (Dictionary<string,Dictionary<string,int>> snapshot in history)
            {
                (string timestamp, Dictionary<string, int> data) = snapshot.First();
                times.Add(timestamp);
                foreach ((string channel, int? overlap) in data)
                {
                    if (!dataPoints.ContainsKey(channel))
                    {
                        dataPoints[channel] = new List<int?> {overlap};
                        first ??= dataPoints[channel];
                        continue;
                    }
                    
                    dataPoints[channel].Add(overlap);
                }

                foreach ((string channel, List<int?> points) in dataPoints)
                {
                    if (data.ContainsKey(channel))
                    {
                        continue;
                    }
                    
                    points.Add(null);
                }
            }

            foreach ((string _, List<int?> points) in dataPoints)
            {
                if (points.Count == first!.Count) continue;
                List<int?> newPoints = Enumerable.Range(1, first.Count - points.Count).Select(_ => (int?) null).ToList();
                newPoints.AddRange(points);
                points.Clear();
                points.AddRange(newPoints);
            }

            return (times, dataPoints);
        }

        private async Task<List<ChannelSummary>> GetChannelData(IReadOnlyCollection<ChannelSummary> channels)
        {
            (List<ChannelSummary> channelSummaries, List<ChannelSummary> notCached) = await GetCachedChannelData(channels);
            List<string> requests = RequestBuilder(notCached);
            
            using HttpClient http = _factory.CreateClient();
            foreach (string reqString in requests)
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
                    var summary = new ChannelSummary
                    {
                        Id = channel.GetProperty("login").GetString(), 
                        DisplayName = channel.GetProperty("display_name").GetString(), 
                        Avatar = channel.GetProperty("profile_image_url").GetString()?.Replace("-300x300", "-70x70")
                    };
                    summary.Chatters = channels.First(x => x.Id.Equals(summary.Id, StringComparison.Ordinal)).Chatters;
                    
                    await _cache.StringSetAsync($"channel:{summary.Id}:display", summary.DisplayName, TimeSpan.FromDays(_random.Next(3, 8)));
                    await _cache.StringSetAsync($"channel:{summary.Id}:avatar", summary.Avatar, TimeSpan.FromDays(_random.Next(3, 8)));
                    channelSummaries.Add(summary);
                }
            }

            return channelSummaries;
        }

        private async Task<(List<ChannelSummary>, List<ChannelSummary>)> GetCachedChannelData(IReadOnlyCollection<ChannelSummary> channels)
        {
            var summaries = new List<ChannelSummary>(channels.Count);
            var notFound = new List<ChannelSummary>(channels.Count);

            foreach (ChannelSummary channel in channels)
            {
                string displayName = await _cache.StringGetAsync($"channel:{channel.Id}:display");
                string avatar = await _cache.StringGetAsync($"channel:{channel.Id}:avatar");
                if (displayName != null && avatar != null)
                {
                    channel.DisplayName = displayName;
                    channel.Avatar = avatar;
                    summaries.Add(channel);
                }
                else
                {
                    notFound.Add(channel);
                }
            }

            return (summaries, notFound);
        }
        
        private static List<string> RequestBuilder(IReadOnlyCollection<ChannelSummary> channels)
        {
            var shards = (int) Math.Ceiling(channels.Count / 100.0);
            var list = new List<string>(shards);
            for (int i = 0; i < shards; i++)
            {
                var request = new StringBuilder();
                foreach (ChannelSummary channel in channels.Skip(i * 100).Take(100))
                {
                    request.Append("&login=").Append(channel.Id);
                }
                list.Add(request.ToString()[1..]);
            }

            return list;
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