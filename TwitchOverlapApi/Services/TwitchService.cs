using System;
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

        public async Task<Channel> Get(string name)
        {
            string cacheKey = name.ToLowerInvariant();

            byte[] encodedChannel = await _cache.GetAsync(cacheKey);
            string channelJson;

            if (encodedChannel != null)
            {
                channelJson = Encoding.UTF8.GetString(encodedChannel);
                return JsonSerializer.Deserialize<Channel>(channelJson);
            }
            
            Channel channel = await _channels.Find(x => x.Id == cacheKey).FirstOrDefaultAsync();

            if (channel == null)
            {
                return null;
            }

            channelJson = JsonSerializer.Serialize(channel);
            encodedChannel = Encoding.UTF8.GetBytes(channelJson);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(20));
            
            await _cache.SetAsync(cacheKey, encodedChannel, options);
            return channel;
        }

        // public Channel GetFromDate(string name, DateTime start)
        // {
        //     Channel channel = Get(name); 
        //     return channel.FilterFromDate(start);
        // }
        //
        // public Channel GetFromRange(string name, DateTime start, DateTime end)
        // {
        //     Channel channel = Get(name); 
        //     return channel.FilterDateRange(start, end);
        // }
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