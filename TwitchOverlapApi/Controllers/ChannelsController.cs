using System;
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

        [HttpGet("{name}")]
        public async Task<ActionResult<ChannelProjection>> Get(string name)
        {
            ChannelProjection channel = await _service.Get(name);
            if (channel == null)
            {
                return NotFound();
            }
        
            return channel;
        }

        // [HttpGet("{name}/from")]
        // public ActionResult<Channel> Get(string name, [FromQuery(Name = "start")] DateTime start)
        // {
        //     Channel channel = _service.GetFromDate(name, start);
        //     if (channel == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     return channel;
        // }
        //
        // [HttpGet("{name}/range")]
        // public ActionResult<Channel> Get(string name, [FromQuery(Name = "start")] DateTime start, [FromQuery(Name = "end")] DateTime end)
        // {
        //     Channel channel = _service.GetFromRange(name, start, end);
        //     if (channel == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     return channel;
        // }
    }
}