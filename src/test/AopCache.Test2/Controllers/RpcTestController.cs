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
            //Parallel.For(0, 100, i =>
            //{
            //    var str = _eventBusProvider.RpcClientAsync<string>("rpc-test1").GetAwaiter().GetResult();
            //    Console.WriteLine(i + "-----" + str);
            //});


            var tasks = Enumerable.Range(0, 10).Select(d => _eventBusProvider.RpcClientAsync<string>("rpc-test1")).ToArray();
            await Task.WhenAll(tasks);

            var str = await _eventBusProvider.RpcClientAsync<string>("rpc-test1");

            return string.IsNullOrWhiteSpace(str.Data) ? BadRequest() : Ok(str.Data);

            return Ok();
        }
    }
}