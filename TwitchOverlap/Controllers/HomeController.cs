using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TwitchOverlap.Extensions;
using TwitchOverlap.Models;
using TwitchOverlap.Services;

namespace TwitchOverlap.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TwitchContext _context;
        private readonly IDatabase _cache;

        private const string IndexCacheKey = "twitch:index";
        private const string ChannelDataCacheKey = "twitch:data:";

        public HomeController(ILogger<HomeController> logger, TwitchContext context, IRedisCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache.Redis.GetDatabase();
        }

        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            string channelSummaries = await _cache.StringGetAsync(IndexCacheKey);
            if (!string.IsNullOrEmpty(channelSummaries))
            {
                return View(JsonSerializer.Deserialize<ChannelIndex>(channelSummaries));
            }

            DateTime latest = await _context.Channels.MaxAsync(x => x.LastUpdate);

            List<ChannelSummary> channelLists = await _context.Channels.FromSqlInterpolated($@"select *
            from channel
            where last_update = {latest}
              and chatters > 0
            order by chatters desc").AsNoTracking()
                .Select(x => new ChannelSummary(x.LoginName, x.DisplayName, x.Avatar, x.Chatters))
                .ToListAsync();

            if (channelLists.Count == 0)
            {
                return View("NoSummary");
            }

            var index = new ChannelIndex
            {
                Channels = channelLists,
                LastUpdate = latest
            };

            await _cache.StringSetAsync(IndexCacheKey, JsonSerializer.Serialize(index), DateTime.UtcNow.GetCacheDuration());

            return View(index);
        }

        [Route("/atlas")]
        public IActionResult Atlas()
        {
            return View("Atlas");
        }

        [Route("/channel/{name}")]
        public IActionResult ChannelRedirect(string name)
        {
            return Redirect($"/{name}");
        }

        [Route("/{name}")]
        public async Task<IActionResult> Channel(string name)
        {
            name = name.ToLowerInvariant();
            string cachedChannelData = await _cache.StringGetAsync(ChannelDataCacheKey + name);
            if (!string.IsNullOrEmpty(cachedChannelData))
            {
                return View(JsonSerializer.Deserialize<ChannelData>(cachedChannelData));
            }

            Channel channel = await _context.Channels.AsNoTracking()
                .Include(x => x.History
                    .OrderByDescending(y => y.Timestamp))
                .SingleOrDefaultAsync(x => x.LoginName == name);

            if (channel == null)
            {
                return View("NoData", name);
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
                return View("NoData", name);
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

            await _cache.StringSetAsync(ChannelDataCacheKey + name, JsonSerializer.Serialize(channelData), DateTime.UtcNow.GetCacheDuration());

            return View(channelData);
        }

        [Route("/{name}/{days:int}")]
        public async Task<IActionResult> ChannelDailyAggregates(string name, int days)
        {
            if (days != 1 && days != 3 && days != 7)
            {
                return Redirect($"/{name}");
            }

            name = name.ToLowerInvariant();
            string cacheKey = name + days;
            string cachedChannelData = await _cache.StringGetAsync(ChannelDataCacheKey + cacheKey);
            if (!string.IsNullOrEmpty(cachedChannelData))
            {
                return View("ChannelAggregates", JsonSerializer.Deserialize<ChannelAggregateData>(cachedChannelData));
            }

            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return View("NoData", name);
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
                Channel noResultChannelData = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
                if (noResultChannelData == null)
                {
                    return View("NoData", name);
                }

                return View("NoAggregateData", new ChannelAggregateData(noResultChannelData) { Type = type });
            }

            var latestOrder = new Dictionary<string, int>();
            var previousOrder = new Dictionary<string, int>();
            var aggregateChange = new ChannelAggregateChange();
            if (overlaps.Count == 2)
            {
                aggregateChange.TotalChatterChange = overlaps[0].ChannelTotalUnique - overlaps[1].ChannelTotalUnique;
                aggregateChange.TotalChatterPercentageChange = (double)(overlaps[0].ChannelTotalUnique - overlaps[1].ChannelTotalUnique) / overlaps[1].ChannelTotalUnique;
                aggregateChange.TotalOverlapChange = overlaps[0].ChannelTotalOverlap - overlaps[1].ChannelTotalOverlap;
                aggregateChange.TotalOverlapPercentageChange = (double)(overlaps[0].ChannelTotalOverlap - overlaps[1].ChannelTotalOverlap) / overlaps[1].ChannelTotalOverlap;
                aggregateChange.OverlapPercentChange =
                    (double)overlaps[0].ChannelTotalOverlap / overlaps[0].ChannelTotalUnique - (double)overlaps[1].ChannelTotalOverlap / overlaps[1].ChannelTotalUnique;

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

            await _cache.StringSetAsync(ChannelDataCacheKey + cacheKey, JsonSerializer.Serialize(channelData), DateTime.UtcNow.GetDailyCacheDuration());
            return View("ChannelAggregates", channelData);
        }
    }
}