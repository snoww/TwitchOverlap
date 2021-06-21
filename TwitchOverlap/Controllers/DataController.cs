using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TwitchOverlap.Extensions;
using TwitchOverlap.Models;
using TwitchOverlap.Services;

namespace TwitchOverlap.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class DataController : ControllerBase
    {
        private readonly TwitchContext _context;
        private readonly IDatabase _cache;

        private const string ApiChannelsCacheKey = "api:twitch:channels";
        private const string ApiChannelHistoryCacheKey = "api:twitch:history:";
        
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        public DataController(TwitchContext context, IRedisCache cache)
        {
            _context = context;
            _cache = cache.Redis.GetDatabase();
        }

        [Route("channels")]
        public async Task<IActionResult> Channels()
        {
            string cachedChannels = await _cache.StringGetAsync(ApiChannelsCacheKey);
            if (!string.IsNullOrEmpty(cachedChannels))
            {
                return Ok(cachedChannels);
            }

            List<string> channels = await _context.Channels.AsNoTracking().Select(x => x.DisplayName).ToListAsync();
            cachedChannels = JsonSerializer.Serialize(channels, SerializerOptions);
            await _cache.StringSetAsync(ApiChannelsCacheKey, cachedChannels, DateTime.UtcNow.GetCacheDuration());

            return Ok(cachedChannels);
        }

        [Route("history/{name}")]
        public async Task<IActionResult> ChannelHistory(string name)
        {
            name = name.ToLowerInvariant();
            const int points = 48*5;
            string cachedHistory = await _cache.StringGetAsync(ApiChannelHistoryCacheKey + name);
            if (!string.IsNullOrEmpty(cachedHistory))
            {
                return Ok(cachedHistory);
            }
            
            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return NotFound($"data for '{name}' not found");
            }
            
            var rawHistory = await _context.Overlaps.AsNoTracking()
                .Where(x => x.Channel == channel.Id)
                .OrderByDescending(x => x.Timestamp)
                .Take(points)
                .Select(x => new {x.Timestamp, Shared = x.Shared.Take(6)})
                .ToListAsync();
            
            var values = new HashSet<string>{"timestamp"};
            var data = new Dictionary<string, Dictionary<string, object>>();

            foreach (var timestamp in rawHistory)
            {
                var time = timestamp.Timestamp.ToString("MMM dd HH:mm");
                foreach (ChannelOverlap overlap in timestamp.Shared)
                {
                    values.Add(overlap.Name);
                    if (data.ContainsKey(time))
                    {
                        data[time][overlap.Name] = overlap.Shared;
                    }
                    else
                    {
                        data[time] = new Dictionary<string, object>
                        {
                            {"timestamp", time},
                            {overlap.Name,overlap.Shared}
                        };
                    }
                }
            }
            
            cachedHistory = JsonSerializer.Serialize(new {channels = values, history = data.Values.ToList()}, SerializerOptions);
            
            await _cache.StringSetAsync(ApiChannelHistoryCacheKey + name, cachedHistory, DateTime.UtcNow.GetCacheDuration());
            
            return Ok(cachedHistory);
        }
    }
}