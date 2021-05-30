using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TwitchOverlap.Models;
using TwitchOverlap.Services;

namespace TwitchOverlap.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TwitchContext _context;
        private readonly IDatabase _cache;

        private const string ChannelSummaryCacheKey = "twitch:summary";
        private const string ChannelDataCacheKey = "twitch:data:";
        private const string ChannelHistoryCacheKey = "twitch:history:";

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
            string channelSummaries = await _cache.StringGetAsync(ChannelSummaryCacheKey);
            if (!string.IsNullOrEmpty(channelSummaries))
            {
                return View(JsonSerializer.Deserialize<List<ChannelSummary>>(channelSummaries));
            }

            DateTime latestHalfHour = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(35));

            List<ChannelSummary> channelLists = await _context.Channels.AsNoTracking()
                .Where(x => x.LastUpdate >= latestHalfHour)
                .OrderByDescending(x => x.Chatters)
                .Select(x => new ChannelSummary(x.Id, x.DisplayName, x.Avatar, x.Chatters))
                .ToListAsync();

            // something went wrong with last ChannelIntersection run, data was not updated
            if (channelLists.Count == 0)
            {
                // try to find latest update time
                DateTime lastUpdate = await _context.Channels.AsNoTracking()
                    .OrderByDescending(x => x.LastUpdate)
                    .Take(1)
                    .Select(x => x.LastUpdate)
                    .SingleAsync();

                // use latest update time
                channelLists = await _context.Channels.AsNoTracking()
                    .Where(x => x.LastUpdate >= lastUpdate)
                    .OrderByDescending(x => x.Chatters)
                    .Select(x => new ChannelSummary(x.Id, x.DisplayName, x.Avatar, x.Chatters))
                    .ToListAsync();
            }

            if (channelLists.Count == 0)
            {
                return View("NoSummary");
            }

            await _cache.StringSetAsync(ChannelSummaryCacheKey, JsonSerializer.Serialize(channelLists), TimeSpan.FromMinutes(5));

            return View(channelLists);
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

            List<Overlap> overlaps = await _context.Overlaps.FromSqlInterpolated($@"select *
            from overlap
            where (source = {name}
                or target = {name})
              and timestamp = (
                select max(timestamp)
                from overlap
                where source = {name}
                   or target = {name})
            order by overlap desc;").AsNoTracking().ToListAsync();

            if (overlaps.Count == 0)
            {
                return View("NoData", name);
            }

            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == name);
            var games = await _context.Channels.AsNoTracking()
                .Where(x => overlaps.Select(y => y.Source == name ? y.Target : y.Source).Contains(x.Id))
                .Select(x => new {x.Id, x.Game})
                .ToDictionaryAsync(x => x.Id);

            var channelData = new ChannelData(channel);

            foreach (Overlap overlap in overlaps)
            {
                string channelName = overlap.Source == name ? overlap.Target : overlap.Source;
                channelData.Data[channelName] = new Data(games[channelName].Game, overlap.Overlapped);
            }

            await _cache.StringSetAsync(ChannelDataCacheKey + name.ToLowerInvariant(), JsonSerializer.Serialize(channelData), TimeSpan.FromMinutes(5));

            return View(channelData);
        }

        [Route("/api/history/{name}")]
        public async Task<IActionResult> ChannelHistory(string name)
        {
            name = name.ToLowerInvariant();
            const int top = 6;
            const int points = 24*7;
            // string cachedHistory = await _cache.StringGetAsync(ChannelHistoryCacheKey + name);
            // if (!string.IsNullOrEmpty(cachedHistory))
            // {
            //     return Ok(cachedHistory);
            // }

            List<Overlap> w = await _context.Overlaps.FromSqlInterpolated($@"select *
            from (
                select *, dense_rank() over (partition by ""timestamp"" order by ""overlap"" desc) as rank
                from overlap
                where source = {name}
                or target = {name}) r
            where rank <= {top}
            order by timestamp desc, rank
            limit {top * points}").AsNoTracking().ToListAsync();

            var values = new HashSet<string>{"timestamp"};
            var data = new Dictionary<string, Dictionary<string, object>>();
            
            for (int i = 0; i < w.Count / top; i++)
            {
                for (int j = 0; j < top; j++)
                {
                    int index = i * top + j;
                    string channelName = w[index].Source == name ? w[index].Target : w[index].Source;
                    string time = w[index].Timestamp.ToString("MMM dd HH:mm");
                    values.Add(channelName);
                    
                    if (data.ContainsKey(time))
                    {
                        data[time][channelName] = w[index].Overlapped;
                    }
                    else
                    {
                        data[time] = new Dictionary<string, object>
                        {
                            {"timestamp", time},
                            {channelName,w[index].Overlapped}
                        };
                    }
                }
            }

            var history = new {channels = values, history = data.Values.ToList()};

            // await _cache.StringSetAsync(ChannelHistoryCacheKey + name.ToLowerInvariant(), JsonSerializer.Serialize(history), TimeSpan.FromMinutes(5));

            return Ok(history);
            // List<Overlap> rawHistory = await _context.Overlaps.AsNoTracking().Include(x => x.Channel)
            //     .Where(x => x.Id == name.ToLowerInvariant())
            //     .OrderByDescending(x => x.Timestamp)
            //     .Take(24)
            //     .ToListAsync();

            // if (rawHistory.Count == 0)
            // {
            //     return NotFound($"No data for channel: {name}");
            // }
            //
            // rawHistory.Reverse();
            //
            // var channels = new HashSet<string>();
            // var history = new List<Dictionary<string, object>>();
            //
            // foreach (Overlap overlap in rawHistory)
            // {
            //     var data = new Dictionary<string, object> {{"date", overlap.Timestamp.ToString("MMM dd HH:mm")}};
            //     
            //     foreach ((string ch, int ov) in overlap.Data.OrderByDescending(x => x.Value).Take(6))
            //     {
            //         data.Add(ch, ov);
            //         channels.Add(ch);
            //     }
            //
            //     history.Add(data);
            // }
            //
            // var ret = new {channels, history};
            //
            //
            // return Ok(ret);
        }
    }
}