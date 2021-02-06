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
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using TwitchOverlapApi.Models;

namespace TwitchOverlapApi.Services
{
    public class TwitchService
    {
        private readonly IMongoCollection<Channel> _channels;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _factory;
        private readonly Random _random = new();
        private static string _twitchToken;
        private static string _twitchClient;

        private const string GameCacheKey = "channel:games";
        private const string ChannelCacheKey = "channel:list";
        
        public TwitchService(ITwitchDatabaseSettings settings, IDistributedCache cache, IHttpClientFactory factory)
        {
            _cache = cache;
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
            string channelListJson = await _cache.GetStringAsync(ChannelCacheKey);
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

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(5));
            await _cache.SetStringAsync(ChannelCacheKey, JsonSerializer.Serialize(channelSummaries), options);
            
            return channelSummaries.OrderByDescending(x => x.Chatters).ToList();
        }

        public async Task<ChannelProjection> Get(string name)
        {
            string cacheKey = name.ToLowerInvariant();

            string channelJson = await _cache.GetStringAsync(cacheKey);

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

            ChannelProjection channelProjection = await ProjectChannelsWithGames(channel, channelGames);
            
            string displayName = await _cache.GetStringAsync($"channel:{channel.Id}:display");
            string avatar = await _cache.GetStringAsync($"channel:{channel.Id}:avatar");
            if (displayName != null && avatar != null)
            {
                channelProjection.DisplayName = displayName;
                channelProjection.Avatar = avatar;
            }
            else
            {
                var cs = new ChannelSummary{Id = channelProjection.Id};
                await GetChannelData(new List<ChannelSummary> {cs});
                channelProjection.DisplayName = await _cache.GetStringAsync($"channel:{channel.Id}:display");
                channelProjection.Avatar = await _cache.GetStringAsync($"channel:{channel.Id}:avatar");
            }
            
            
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(5));
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(channelProjection), options);
            
            return channelProjection;
        }

        private async Task<List<ChannelGames>> GetChannelGames()
        {
            string channelGames = await _cache.GetStringAsync(GameCacheKey);
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

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(5));

            await _cache.SetStringAsync(GameCacheKey, JsonSerializer.Serialize(games), options);

            return games;
        }

        private static async Task<ChannelProjection> ProjectChannelsWithGames(Channel channels, IReadOnlyCollection<ChannelGames> channelGamesList)
        {
            var channelProjection = new ChannelProjection
            {
                Id = channels.Id,
                Timestamp = channels.Timestamp,
                Game = channels.Game,
                Viewers = channels.Viewers,
                Overlaps = channels.Overlaps,
                Chatters = channels.Chatters,
                Avatar = channels.Avatar
            };

            var data = new ConcurrentDictionary<string, Data>();

            IEnumerable<Task> tasks = channels.Data.Select(async x =>
            {
                data.TryAdd(x.Key, new Data{Shared = x.Value, Game = channelGamesList.FirstOrDefault(y => y.Id == x.Key)?.Game});
            });

            await Task.WhenAll(tasks);
            channelProjection.Data = new Dictionary<string, Data>(data);
            
            return channelProjection;
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
                    
                    DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromDays(_random.Next(3, 8)));
                    await _cache.SetStringAsync($"channel:{summary.Id}:display", summary.DisplayName, options);
                    await _cache.SetStringAsync($"channel:{summary.Id}:avatar", summary.Avatar, options);
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
                string displayName = await _cache.GetStringAsync($"channel:{channel.Id}:display");
                string avatar = await _cache.GetStringAsync($"channel:{channel.Id}:avatar");
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
                list.Add(request.ToString().Substring(1));
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