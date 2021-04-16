using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AopCache.Test2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HttpTestController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient();
            var str = await client.GetStringAsync("http://localhost:5000/WeatherForecast");

            return string.IsNullOrWhiteSpace(str) ? BadRequest() : Ok(str);
        }
    }
}