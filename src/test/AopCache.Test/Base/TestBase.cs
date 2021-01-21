using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AopCache.Test.Base
{
    public class TestBase
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public TestBase()
        {
            TestServer = new TestServer(WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config => { config.AddJsonFile("config/config.json", true, true); })
                .UseStartup<Startup>());
        }

        public TestServer TestServer { get; set; }

        public IServiceProvider ServiceProvider => TestServer.Services;

        protected T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

    }
}