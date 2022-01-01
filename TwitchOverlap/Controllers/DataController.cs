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
    [Route("v1")]
    public class DataController : ControllerBase
    {
        private readonly TwitchContext _context;
        private readonly IDatabase _cache;

        private const string ApiIndexCacheKey = "api:twitch:index";
        private const string ApiChannelCacheKey = "api:twitch:channel";
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

        [Route("index")]
        public async Task<IActionResult> Index()
        {
            string channelSummaries = await _cache.StringGetAsync(ApiIndexCacheKey);
            if (!string.IsNullOrEmpty(channelSummaries))
            {
                return Ok(channelSummaries);
            }

            DateTime latest = await _context.Channels.MaxAsync(x => x.LastUpdate);

            List<ChannelSummary> channelLists = await _context.Channels.FromSqlInterpolated($@"select *
            from channel
            where last_update = (select max(last_update) from channel)
              and chatters > 0
            order by chatters desc").AsNoTracking()
                .Select(x => new ChannelSummary(x.LoginName, x.DisplayName, x.Avatar, x.Chatters))
                .ToListAsync();
            
            if (channelLists.Count == 0)
            {
                return NotFound("error");
            }
            
            var index = new ChannelIndex
            {
                Channels = channelLists,
                LastUpdate = latest
            };
            
            await _cache.StringSetAsync(ApiIndexCacheKey, JsonSerializer.Serialize(index, SerializerOptions), DateTime.UtcNow.GetCacheDuration());

            return Ok(index);
        }

        [Route("channel/{name}")]
        public async Task<IActionResult> Channel(string name)
        {
            name = name.ToLowerInvariant();
            string cachedChannelData = await _cache.StringGetAsync(ApiChannelCacheKey + name);
            if (!string.IsNullOrEmpty(cachedChannelData))
            {
                return Ok(cachedChannelData);
            }

            Channel channel = await _context.Channels.AsNoTracking()
                .Include(x => x.History
                    .OrderByDescending(y => y.Timestamp))
                .SingleOrDefaultAsync(x => x.LoginName == name);

            if (channel == null)
            {
                return NotFound($"no data for {name}");
            }

            List<Overlap> overlaps = await _context.Overlaps.AsNoTracking()
                .Where(x => x.Channel == channel.Id)
                .OrderByDescending(x => x.Timestamp)
                .Take(2)
                .Include(x => x.ChannelNavigation)
                .Select(x => new Overlap { Shared = x.Shared.Take(100).ToList() })
                .ToListAsync();

            if (overlaps.Count == 0)
            {
                return NotFound($"no overlap data for {name}");
            }

            var latestOrder = new Dictionary<string, int>();
            var previousOrder = new Dictionary<string, int>();
            if (overlaps.Count == 2)
            {
                List<ChannelOverlap> latest = overlaps[0].Shared;
                List<ChannelOverlap> previous = overlaps[1].Shared;

                for (int i = 0; i < Math.Max(latest.Count, previous.Count); i++)
                {
                    if (i < latest.Count)
                    {
                        latestOrder[latest[i].Name] = i;
                    }

                    if (i < previous.Count)
                    {
                        previousOrder[previous[i].Name] = i;
                    }
                }
            }

            var overlappedChannelsData = await _context.Channels.AsNoTracking()
                .Where(x => overlaps[0].Shared.Select(y => y.Name).Contains(x.LoginName))
                .Select(x => new { x.LoginName, x.DisplayName, x.Game })
                .ToDictionaryAsync(x => x.LoginName);

            var channelData = new ChannelData(channel);

            foreach (ChannelOverlap overlap in overlaps[0].Shared)
            {
                int? change = null;
                if (previousOrder.ContainsKey(overlap.Name))
                {
                    change = previousOrder[overlap.Name] - latestOrder[overlap.Name];
                }

                var data = overlappedChannelsData[overlap.Name];
                channelData.Data.Add(new Data
                {
                    LoginName = overlap.Name,
                    DisplayName = data.DisplayName,
                    Game = data.Game,
                    Shared = overlap.Shared,
                    Change = change
                });
            }

            await _cache.StringSetAsync(ApiChannelCacheKey + name, JsonSerializer.Serialize(channelData), DateTime.UtcNow.GetCacheDuration());

            return Ok(channelData);
        }
        
        [Route("channel/{name}/{days:int}")]
        public async Task<IActionResult> ChannelDailyAggregates(string name, int days)
        {
            if (days != 1 && days != 3 && days != 7)
            {
                return NotFound("invalid day, only 1, 3, or 7 day aggregates available");
            }

            name = name.ToLowerInvariant();
            string cacheKey = name + days;
            string cachedChannelData = await _cache.StringGetAsync(ApiChannelCacheKey + cacheKey);
            if (!string.IsNullOrEmpty(cachedChannelData))
            {
                return Ok(cachedChannelData);
            }

            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return NotFound($"no aggregate data available for {name}");
            }

            List<OverlapAggregate> overlaps;
            AggregateDays type;
            switch (days)
            {
                case 1:
                    overlaps = await _context.OverlapsDaily.AsNoTracking()
                        .Where(x => x.Channel == channel.Id)
                        .OrderByDescending(x => x.Date)
                        .Take(2)
                        .Select(x => new OverlapAggregate { Date = x.Date, ChannelTotalOverlap = x.ChannelTotalOverlap, ChannelTotalUnique = x.ChannelTotalUnique, Shared = x.Shared })
                        .ToListAsync();
                    type = AggregateDays.OneDay;
                    break;
                case 3:
                    overlaps = await _context.OverlapRolling3Days.AsNoTracking()
                        .Where(x => x.Channel == channel.Id)
                        .OrderByDescending(x => x.Date)
                        .Take(2)
                        .Select(x => new OverlapAggregate { Date = x.Date, ChannelTotalOverlap = x.ChannelTotalOverlap, ChannelTotalUnique = x.ChannelTotalUnique, Shared = x.Shared })
                        .ToListAsync();
                    type = AggregateDays.ThreeDays;
                    break;
                default:
                    overlaps = await _context.OverlapRolling7Days.AsNoTracking()
                        .Where(x => x.Channel == channel.Id)
                        .OrderByDescending(x => x.Date)
                        .Take(2)
                        .Select(x => new OverlapAggregate { Date = x.Date, ChannelTotalOverlap = x.ChannelTotalOverlap, ChannelTotalUnique = x.ChannelTotalUnique, Shared = x.Shared })
                        .ToListAsync();
                    type = AggregateDays.SevenDays;
                    break;
            }

            if (overlaps.Count == 0)
            {
                return NotFound($"no aggregate data available for {name}");
            }

            var latestOrder = new Dictionary<string, int>();
            var previousOrder = new Dictionary<string, int>();
            var aggregateChange = new ChannelAggregateChange();
            if (overlaps.Count == 2)
            {
                aggregateChange.TotalChatterChange = overlaps[0].ChannelTotalUnique - overlaps[1].ChannelTotalUnique;
                aggregateChange.TotalChatterPercentageChange = Math.Round((double)(overlaps[0].ChannelTotalUnique - overlaps[1].ChannelTotalUnique) / overlaps[1].ChannelTotalUnique, 5);
                aggregateChange.TotalOverlapChange = overlaps[0].ChannelTotalOverlap - overlaps[1].ChannelTotalOverlap;
                aggregateChange.TotalOverlapPercentageChange = Math.Round((double)(overlaps[0].ChannelTotalOverlap - overlaps[1].ChannelTotalOverlap) / overlaps[1].ChannelTotalOverlap, 5);
                aggregateChange.OverlapPercentChange =
                    Math.Round((double)overlaps[0].ChannelTotalOverlap / overlaps[0].ChannelTotalUnique - (double)overlaps[1].ChannelTotalOverlap / overlaps[1].ChannelTotalUnique, 5);

                List<ChannelOverlap> latest = overlaps[0].Shared;
                List<ChannelOverlap> previous = overlaps[1].Shared;

                for (int i = 0; i < Math.Max(latest.Count, previous.Count); i++)
                {
                    if (i < latest.Count)
                    {
                        latestOrder[latest[i].Name] = i;
                    }

                    if (i < previous.Count)
                    {
                        previousOrder[previous[i].Name] = i;
                    }
                }
            }

            var overlappedChannelsData = await _context.Channels.AsNoTracking()
                .Where(x => overlaps[0].Shared.Select(y => y.Name).Contains(x.LoginName))
                .Select(x => new { x.LoginName, x.DisplayName })
                .ToDictionaryAsync(x => x.LoginName);

            var channelData = new ChannelAggregateData(channel)
            {
                Type = type,
                Date = overlaps[0].Date,
                Change = aggregateChange,
                ChannelTotalOverlap = overlaps[0].ChannelTotalOverlap,
                ChannelTotalUnique = overlaps[0].ChannelTotalUnique
            };

            foreach (ChannelOverlap overlap in overlaps[0].Shared)
            {
                int? change = null;
                if (previousOrder.ContainsKey(overlap.Name))
                {
                    change = previousOrder[overlap.Name] - latestOrder[overlap.Name];
                }

                if (overlappedChannelsData.ContainsKey(overlap.Name))
                {
                    channelData.Data.Add(new Data
                    {
                        LoginName = overlap.Name,
                        DisplayName = overlappedChannelsData[overlap.Name].DisplayName,
                        Shared = overlap.Shared,
                        Change = change
                    });
                }
            }

            await _cache.StringSetAsync(ApiChannelCacheKey + cacheKey, JsonSerializer.Serialize(channelData), DateTime.UtcNow.GetDailyCacheDuration());
            return Ok(channelData);
        }

        [Route("channels")]
        public async Task<IActionResult> Channels()
        {
            string cachedChannels = await _cache.StringGetAsync(ApiChannelsCacheKey);
            if (!string.IsNullOrEmpty(cachedChannels))
            {
                return Ok(cachedChannels);
            }

            List<string> channels = await _context.Channels.AsNoTracking()
                .Where(x => x.LastUpdate >= DateTime.UtcNow.AddDays(-14))
                .OrderByDescending(x => x.Chatters)
                .Select(x => x.LoginName)
                .ToListAsync();
            cachedChannels = JsonSerializer.Serialize(channels, SerializerOptions);
            await _cache.StringSetAsync(ApiChannelsCacheKey, cachedChannels, DateTime.UtcNow.GetDailyCacheDuration());

            return Ok(cachedChannels);
        }
        
        [Route("channels/{num:int}")]
        public async Task<IActionResult> TopChannels(int num)
        {
            if (num < 0)
            {
                return BadRequest("need at least 1 channel");
            }
            
            string cachedChannels = await _cache.StringGetAsync($"{ApiChannelsCacheKey}:{num}");
            if (!string.IsNullOrEmpty(cachedChannels))
            {
                return Ok(cachedChannels);
            }

            List<string> channels = await _context.Channels.AsNoTracking()
                .Where(x => x.LastUpdate >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(x => x.Chatters)
                .Take(num)
                .Select(x => x.LoginName)
                .ToListAsync();
            cachedChannels = JsonSerializer.Serialize(channels, SerializerOptions);
            await _cache.StringSetAsync($"{ApiChannelsCacheKey}:{num}", cachedChannels, DateTime.UtcNow.GetCacheDuration());

            return Ok(cachedChannels);
        }

        [Route("history/{name}")]
        public async Task<IActionResult> ChannelHistory(string name)
        {
            name = name.ToLowerInvariant();
            const int points = 24*3;
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
                var time = timestamp.Timestamp.ToUniversalTime().ToString("O");
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
            
            cachedHistory = JsonSerializer.Serialize(new {channels = values, history = data.Values}, SerializerOptions);
            
            await _cache.StringSetAsync(ApiChannelHistoryCacheKey + name, cachedHistory, DateTime.UtcNow.GetCacheDuration());
            
            return Ok(cachedHistory);
        }

        [Route("history/{name}/{days:int}")]
        public async Task<IActionResult> ChannelHistory(string name, int days)
        {
            if (days != 1 && days != 3 && days != 7)
            {
                return NotFound("Invalid day, parameter must be 1, 3, 7");
            }
            
            name = name.ToLowerInvariant();
            const int points = 14;
            string cacheKey = name + days;
            string cachedHistory = await _cache.StringGetAsync(ApiChannelHistoryCacheKey + cacheKey);
            if (!string.IsNullOrEmpty(cachedHistory))
            {
                return Ok(cachedHistory);
            }
            
            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return NotFound($"data for '{name}' not found");
            }

            List<OverlapHistory> rawHistory = days switch
            {
                1 => await _context.OverlapsDaily.AsNoTracking()
                    .Where(x => x.Channel == channel.Id)
                    .OrderByDescending(x => x.Date)
                    .Take(points)
                    .Select(x => new OverlapHistory(x.Date, x.Shared.Take(6)))
                    .ToListAsync(),
                3 => await _context.OverlapRolling3Days.AsNoTracking()
                    .Where(x => x.Channel == channel.Id)
                    .OrderByDescending(x => x.Date)
                    .Take(points)
                    .Select(x => new OverlapHistory(x.Date, x.Shared.Take(6)))
                    .ToListAsync(),
                _ => await _context.OverlapRolling7Days.AsNoTracking()
                    .Where(x => x.Channel == channel.Id)
                    .OrderByDescending(x => x.Date)
                    .Take(points)
                    .Select(x => new OverlapHistory(x.Date, x.Shared.Take(6)))
                    .ToListAsync(),
            };

            var values = new HashSet<string>{"timestamp"};
            var data = new Dictionary<string, Dictionary<string, object>>();

            foreach (OverlapHistory timestamp in rawHistory)
            {
                var time = timestamp.Timestamp.ToString("MMM dd");
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
            
            cachedHistory = JsonSerializer.Serialize(new {channels = values, history = data.Values}, SerializerOptions);
            
            await _cache.StringSetAsync(ApiChannelHistoryCacheKey + cacheKey, cachedHistory, DateTime.UtcNow.GetDailyCacheDuration());
            
            return Ok(cachedHistory);
        }
    }
}