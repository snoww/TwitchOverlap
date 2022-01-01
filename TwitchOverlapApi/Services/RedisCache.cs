using StackExchange.Redis;

namespace TwitchOverlapApi.Services
{
    public interface IRedisCache
    {
        public ConnectionMultiplexer Redis { get; }
    }

    public class RedisCache : IRedisCache
    {
        public ConnectionMultiplexer Redis { get; }

        public RedisCache(string credentials)
        {
            Redis = ConnectionMultiplexer.Connect(credentials);
        }
    }
}