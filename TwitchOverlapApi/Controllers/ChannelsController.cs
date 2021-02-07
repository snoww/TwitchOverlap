using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TwitchOverlapApi.Models;
using TwitchOverlapApi.Services;

namespace TwitchOverlapApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChannelsController : ControllerBase
    {
        private readonly TwitchService _service;

        public ChannelsController(TwitchService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ChannelSummary>>> Get()
        {
            List<ChannelSummary> channelLists = await _service.Get();
            if (channelLists == null)
            {
                return NotFound();
            }

            return channelLists;
        }
        
        [HttpGet("{name}")]
        public async Task<ActionResult<ChannelProjection>> GetChannel(string name)
        {
            ChannelProjection channel = await _service.Get(name);
            if (channel == null)
            {
                return NotFound();
            }
        
            return channel;
        }

        [HttpGet("history/{name}")]
        public async Task<ActionResult<ChannelHistory>> GetHistory(string name)
        {
            ChannelHistory history = await _service.GetChannelHistory(name);
            if (history == null)
            {
                return NotFound();
            }
        
            return history;
        }
    }
}