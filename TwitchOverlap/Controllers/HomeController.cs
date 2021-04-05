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
            
            await _cache.StringSetAsync(ChannelSummaryCacheKey, JsonSerializer.Serialize(channelLists), TimeSpan.FromMinutes(5));

            return channelLists == null ? View("NoSummary") : View(channelLists);
        }

        [Route("/channel/{name}")]
        public IActionResult ChannelRedirect(string name)
        {
            return Redirect($"/{name}");
        }

        [Route("/{name}")]
        public async Task<IActionResult> Channel(string name)
        {
            string cachedChannelData = await _cache.StringGetAsync(ChannelDataCacheKey + name.ToLowerInvariant());
            if (!string.IsNullOrEmpty(cachedChannelData))
            {
                return View(JsonSerializer.Deserialize<ChannelData>(cachedChannelData));
            }
            
            Overlap channel = await _context.Overlaps.AsNoTracking().Include(x => x.Channel)
                .Where(x => x.Id == name.ToLowerInvariant())
                .OrderByDescending(x => x.Timestamp)
                .Take(1)
                .SingleOrDefaultAsync();

            if (channel == null)
            {
                return View("NoData", name);
            }

            var channelData = new ChannelData(channel.Channel);
            List<string> channels = channel.Data.Keys.ToList();

            var games = await _context.Channels.AsNoTracking().Where(x => channels.Contains(x.Id)).Select(x => new {x.Id, x.Game}).ToListAsync();

            foreach ((string ch, int shared) in channel.Data.OrderByDescending(x => x.Value))
            {
                channelData.Data[ch] = new Data(games.First(x => x.Id == ch).Game, shared);
            }
            
            await _cache.StringSetAsync(ChannelDataCacheKey + name.ToLowerInvariant(), JsonSerializer.Serialize(channelData), TimeSpan.FromMinutes(5));
            
            return View(channelData);
        }

        [Route("/api/history/{name}")]
        public async Task<IActionResult> ChannelHistory(string name)
        {
            string channelHistory = await _cache.StringGetAsync(ChannelHistoryCacheKey + name.ToLowerInvariant());
            if (!string.IsNullOrEmpty(channelHistory))
            {
                return Ok(channelHistory);
            }

            List<Overlap> rawHistory = await _context.Overlaps.AsNoTracking().Include(x => x.Channel)
                .Where(x => x.Id == name.ToLowerInvariant())
                .OrderByDescending(x => x.Timestamp)
                .Take(24)
                .ToListAsync();

            if (rawHistory.Count == 0)
            {
                return NotFound($"No data for channel: {name}");
            }

            rawHistory.Reverse();
            
            var channels = new HashSet<string>();
            var history = new List<Dictionary<string, object>>();
            
            foreach (Overlap overlap in rawHistory)
            {
                var data = new Dictionary<string, object> {{"date", overlap.Timestamp.ToString("MMM dd HH:mm")}};
                
                foreach ((string ch, int ov) in overlap.Data.OrderByDescending(x => x.Value).Take(6))
                {
                    data.Add(ch, ov);
                    channels.Add(ch);
                }

                history.Add(data);
            }

            var ret = new {channels, history};
            
            await _cache.StringSetAsync(ChannelHistoryCacheKey + name.ToLowerInvariant(), JsonSerializer.Serialize(ret), TimeSpan.FromMinutes(5));

            return Ok(ret);
        }
    }
}