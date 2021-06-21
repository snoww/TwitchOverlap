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
            
            Channel channel = await _context.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.LoginName == name);
            if (channel == null)
            {
                return View("NoData", name);
            }

            Overlap overlaps = await _context.Overlaps.FromSqlInterpolated($@"select *
            from overlap
            where channel = {channel.Id}
              and timestamp = (select max(timestamp)
                               from overlap
                               where channel = {channel.Id})").AsNoTracking()
                .Include(x => x.ChannelNavigation)
                .SingleOrDefaultAsync();

            if (overlaps == null)
            {
                return View("NoData", name);
            }
            
            var overlappedChannelsData = await _context.Channels.AsNoTracking()
                .Where(x => overlaps.Shared.Select(y => y.Name).Contains(x.LoginName))
                .Select(x => new {x.LoginName, x.DisplayName, x.Game})
                .ToDictionaryAsync(x => x.LoginName);

            var channelData = new ChannelData(channel);

            foreach (ChannelOverlap overlap in overlaps.Shared)
            {
                var data = overlappedChannelsData[overlap.Name];
                channelData.Data[overlap.Name] = new Data(data.Game, overlap.Shared, data.DisplayName);
            }

            await _cache.StringSetAsync(ChannelDataCacheKey + name, JsonSerializer.Serialize(channelData), DateTime.UtcNow.GetCacheDuration());

            return View(channelData);
        }
    }
}