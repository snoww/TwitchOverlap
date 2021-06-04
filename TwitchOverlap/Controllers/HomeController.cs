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

            List<ChannelSummary> channelLists = await _context.Channels.FromSqlInterpolated($@"select *
            from channel
            where last_update = (
                select max(last_update)
                from channel)
            order by chatters desc").AsNoTracking()
                .Select(x => new ChannelSummary(x.LoginName, x.DisplayName, x.Avatar, x.Chatters))
                .ToListAsync();
            
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
            
            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return View("NoData", name);
            }

            var overlaps = await _context.Overlaps.FromSqlInterpolated($@"select *
            from overlap
            where (source = {channel.Id}
                or target = {channel.Id})
              and timestamp = (
                select max(timestamp)
                from overlap
                where source = {channel.Id}
                   or target = {channel.Id})").AsNoTracking()
                .Include(x => x.SourceNavigation)
                .Include(x => x.TargetNavigation)
                .OrderByDescending(x => x.Overlapped)
                .Select(x => new { 
                    Source = x.SourceNavigation.LoginName, 
                    SourceGame = x.SourceNavigation.Game, 
                    Target = x.TargetNavigation.LoginName,
                    TargetGame = x.TargetNavigation.Game,
                    x.Overlapped
                })
                .ToListAsync();

            if (overlaps.Count == 0)
            {
                return View("NoData", name);
            }

            var channelData = new ChannelData(channel);

            foreach (var overlap in overlaps)
            {
                if (overlap.Source == name)
                {
                    channelData.Data[overlap.Target] = new Data(overlap.Target, overlap.Overlapped);
                }
                else
                {
                    channelData.Data[overlap.Source] = new Data(overlap.Source, overlap.Overlapped);
                }
            }

            await _cache.StringSetAsync(ChannelDataCacheKey + name, JsonSerializer.Serialize(channelData), TimeSpan.FromMinutes(5));

            return View(channelData);
        }

        [Route("/api/history/{name}")]
        public async Task<IActionResult> ChannelHistory(string name)
        {
            name = name.ToLowerInvariant();
            const int top = 6;
            const int points = 24*7;
            string cachedHistory = await _cache.StringGetAsync(ChannelHistoryCacheKey + name);
            if (!string.IsNullOrEmpty(cachedHistory))
            {
                return Ok(cachedHistory);
            }
            
            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return NotFound($"data for '{name}' not found");
            }

            var overlapHistory = await _context.Overlaps.FromSqlInterpolated($@"select *
            from (
                select *, rank() over (partition by ""timestamp"" order by ""overlap"" desc) as rank
                from overlap
                where source = {channel.Id}
                or target = {channel.Id}) r
            where rank <= {top}
            order by timestamp desc, rank
            limit {top * points}").AsNoTracking()
                .Include(x => x.SourceNavigation)
                .Include(x => x.TargetNavigation)
                .Select(x =>  new {x.Timestamp, Source = x.SourceNavigation.LoginName, Target = x.TargetNavigation.LoginName, x.Overlapped})
                .ToListAsync();

            var values = new HashSet<string>{"timestamp"};
            var data = new Dictionary<string, Dictionary<string, object>>();
            
            for (int i = 0; i < overlapHistory.Count / top; i++)
            {
                for (int j = 0; j < top; j++)
                {
                    int index = i * top + j;
                    string channelName = overlapHistory[index].Source == name ? overlapHistory[index].Target : overlapHistory[index].Source;
                    string time = overlapHistory[index].Timestamp.ToString("MMM dd HH:mm");
                    values.Add(channelName);
                    
                    if (data.ContainsKey(time))
                    {
                        data[time][channelName] = overlapHistory[index].Overlapped;
                    }
                    else
                    {
                        data[time] = new Dictionary<string, object>
                        {
                            {"timestamp", time},
                            {channelName,overlapHistory[index].Overlapped}
                        };
                    }
                }
            }

            var history = new {channels = values, history = data.Values.ToList()};
            
            await _cache.StringSetAsync(ChannelHistoryCacheKey + name, JsonSerializer.Serialize(history), TimeSpan.FromMinutes(5));

            return Ok(history);
        }
    }
}