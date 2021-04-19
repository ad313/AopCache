using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AopCache.Core.Implements;
using AopCache.EventBus.RabbitMQ.Attributes;

namespace AopCache.Test1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly TestClass _testClass;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,TestClass testClass)
        {
            _logger = logger;
            _testClass = testClass;
        }

        [HttpGet]
        [RpcServer("rpc-test1")]
        public IEnumerable<WeatherForecast> Get()
        {
            //var v = _testClass.Get();
            //v = _testClass.Get();
            //var ss = AopCacheProviderInstance.Get<Guid>("aaaaa","b").GetAwaiter().GetResult();

            //AopCacheProviderInstance.Remove("aaaaa","b");

            //v = _testClass.Get();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
