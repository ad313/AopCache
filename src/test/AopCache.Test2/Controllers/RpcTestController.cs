using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AopCache.Test2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RpcTestController : ControllerBase
    {
        private readonly IEventBusProvider _eventBusProvider;

        public RpcTestController(IEventBusProvider eventBusProvider)
        {
            _eventBusProvider = eventBusProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var str = await _eventBusProvider.RpcClientAsync<string>("rpc-test1");

            return string.IsNullOrWhiteSpace(str.Data) ? BadRequest() : Ok(str.Data);
        }
    }
}