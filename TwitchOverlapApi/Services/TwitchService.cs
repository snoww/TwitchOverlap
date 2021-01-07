using System;
using System.Collections.Generic;
using System.Linq;
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

        private const string GameCacheKey = "channel:game";
        
        public TwitchService(ITwitchDatabaseSettings settings, IDistributedCache cache)
        {
            _cache = cache;
            var conventions = new ConventionPack {new LowerCaseElementNameConvention()};
            ConventionRegistry.Register("LowerCaseElementName", conventions, _ => true);
            var client = new MongoClient(settings.ConnectionString);
            IMongoDatabase database = client.GetDatabase(settings.DatabaseName);

            _channels = database.GetCollection<Channel>(settings.CollectionName);
        }

        public async Task<ChannelProjection> Get(string name)
        {
            string cacheKey = name.ToLowerInvariant();

            string channelJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(channelJson))
            {
                // channelJson = Encoding.UTF8.GetString(encodedChannel);
                return JsonSerializer.Deserialize<ChannelProjection>(channelJson);
            }

            Channel channel = await _channels.Find(x => x.Id == cacheKey).FirstOrDefaultAsync();
            if (channel == null)
            {
                return null;
            }

            List<ChannelGames> channelGames = await GetChannelGames();

            ChannelProjection channelProjection = await ProjectChannelsWithGames(channel, channelGames);
            
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

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(5));

            await _cache.SetStringAsync(GameCacheKey, JsonSerializer.Serialize(games), options);

            return games;
        }

        private static async Task<ChannelProjection> ProjectChannelsWithGames(Channel channels, List<ChannelGames> channelGamesList)
        {
            var channelProjection = new ChannelProjection
            {
                Id = channels.Id,
                Timestamp = channels.Timestamp,
                Game = channels.Game,
                Viewers = channels.Viewers,
                Chatters = channels.Chatters
            };

            var data = new Dictionary<string, Data>(channels.Data.Count);

            IEnumerable<Task> tasks = channels.Data.Select(async x =>
            {
                data.Add(x.Key, new Data{Shared = x.Value, Game = channelGamesList.First(y => y.Id == x.Key).Game});
            });

            await Task.WhenAll(tasks);
            channelProjection.Data = data;
            
            return channelProjection;
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