using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Bson;
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

            Channel channel = await _channels.Find(x => x.Id == name).FirstOrDefaultAsync();
            if (channel == null)
            {
                return null;
            }
            
            List<ChannelGames> games = await _channels.Find(x => x.Game != null)
                .Project(x => new ChannelGames {Id = x.Id, Game = x.Game})
                .ToListAsync();

            ChannelProjection channelProjection = await ProjectChannelsWithGames(channel, games);

            channelJson = JsonSerializer.Serialize(channelProjection);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(10));
            await _cache.SetStringAsync(cacheKey, channelJson, options);
            
            return channelProjection;
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