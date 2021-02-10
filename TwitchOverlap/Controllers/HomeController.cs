using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TwitchOverlap.Models;
using TwitchOverlap.Services;

namespace TwitchOverlap.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TwitchService _service;

        public HomeController(ILogger<HomeController> logger, TwitchService service)
        {
            _logger = logger;
            _service = service;
        }

        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            List<ChannelSummary> channelLists = await _service.Get();
            if (channelLists == null)
            {
                return View("NoSummary");
            }

            return View(channelLists);
        }

        [Route("/channel")]
        public IActionResult Channel()
        {
            return Redirect("/");
        }

        [Route("/channel/{name}")]
        public async Task<IActionResult> Channel(string name)
        {
            ChannelProjection channel = await _service.Get(name);
            if (channel == null)
            {
                return View("NoData", name);
            }
            return View(channel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}