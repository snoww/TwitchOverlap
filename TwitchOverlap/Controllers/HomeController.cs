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
        
        private const string ChannelSummaryCacheKey = "channel:summaries";

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
                .Select(x => new ChannelSummary(x.Id, x.DisplayName, x.Avatar, x.Chatters))
                .ToListAsync();
            
            await _cache.StringSetAsync(ChannelSummaryCacheKey, JsonSerializer.Serialize(channelSummaries), TimeSpan.FromMinutes(5));

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
            Overlap channel = await _context.Overlaps.AsNoTracking().Include(x => x.Channel)
                .OrderByDescending(x => x.Timestamp)
                .Take(1)
                .SingleOrDefaultAsync();

            if (channel == null)
            {
                return View("NoData");
            }

            var channelData = new ChannelData(channel.Channel);

            foreach ((string ch, int shared) in channel.Data)
            {
                channelData.Data[ch] = new Data(await _context.Channels.AsNoTracking().Where(x => x.Id == ch).Select(x => x.Game).SingleOrDefaultAsync(), shared);
            }
            
            return View(channelData);
        }
    }
}