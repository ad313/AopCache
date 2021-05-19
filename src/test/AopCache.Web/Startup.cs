using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using AopCache.Implements;
using AopCache.Runtime;
using AopCache.Web.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AopCache.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);


            //注入打了标签的Service
            services.AddTransient<ITestService, TestService>();
            services.AddTransient<TestSingleClass>();


            //自定义存储 这里xxx表示 IAopCacheProvider 的实现            
            //services.AddAopCache<xxx>();    


            //默认内存存储
            //返回IServiceProvider，由 AspectCore接管
            //services.AddAopCache();

            //redis实现 
            //Newtonsoft
            //services.AddAopCacheUseCsRedis("192.168.1.120:30985,password=123456,defaultDatabase=5");
            //services.AddAopTriggerWithRedis("192.168.1.120:30985,password=123456,defaultDatabase=5");

            services.AddAopCache(option =>
            {
                //option.UseMemoryCacheProvider();
                //option.AddAopTriggerUseMemoryEventBus();

                option.UseCsRedisCacheProvider("192.168.1.120:30985,password=123456,defaultDatabase=5");


                //option.AddAopTriggerUseRedisEventBus("192.168.1.120:30985,password=123456,defaultDatabase=5");

                option.AddAopTriggerUseRabbitMqEventBus(Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>());
            });

            //MessagePack
            //services.AddAopCacheUseCsRedisWithMessagePack("192.168.1.110:32350,password=123456,defaultDatabase=5");

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,IHostApplicationLifetime hostLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            hostLifetime.ApplicationStarted.Register(async () =>
            {
                var provider = app.ApplicationServices.GetService<IEventBusProvider>();

                provider.Subscribe<int>("aqwe", data =>
                {
                    Console.WriteLine($"{data.Data}--------------------------------1");
                });

                provider.Subscribe<int>("aqwe", data =>
                {
                    Console.WriteLine($"{data.Data}--------------------------------2");
                });

                provider.Subscribe<int>("aqwe", data =>
                {
                    Console.WriteLine($"{data.Data}--------------------------------3");
                });


                provider.SubscribeQueue<UserInfo>("abc", 200, 1, ExceptionHandlerEnum.PushToErrorQueueAndContinue,
                    async data =>
                    {
                        Console.WriteLine($"{DateTime.Now} from queue : {data.Count}");

                            //foreach (var s in data)
                            //{
                            //    Console.WriteLine($"{s}");
                            //}

                            //await Task.Delay(500);

                            //throw new Exception("hahhah");

                            await Task.CompletedTask;
                    },
                    async (ex, data) =>
                    {
                        Console.WriteLine($"报错了------- : {data.Count}");
                        await Task.CompletedTask;

                            //throw new Exception("error error");
                        }, async () =>
                        {
                            Console.WriteLine($"over");
                            await Task.CompletedTask;
                        });


                //await Task.Delay(3000);
                //for (int i = 0; i < 100; i++)
                //{
                //    await provider.PublishAsync("aqwe", new EventMessageModel<int>(i));
                //}

            });

            //hostLifetime.ApplicationStarted.Register(async () =>
            //{
            //    var provider = app.ApplicationServices.GetService<IEventBusProvider>();

            //    //provider.SubscribeQueue<string>("abc", func =>
            //    //{
            //    //    var list = func(10);
            //    //    Console.WriteLine($"from queue : {list.Count}");
            //    //});

            //    provider.SetEnable(false, "abc");

            //    provider.SubscribeQueue<UserInfo>("abc", 200, 1, ExceptionHandlerEnum.PushToErrorQueueAndContinue,
            //        async data =>
            //        {
            //            Console.WriteLine($"{DateTime.Now} from queue : {data.Count}");

            //            //foreach (var s in data)
            //            //{
            //            //    Console.WriteLine($"{s}");
            //            //}

            //            //await Task.Delay(500);

            //            //throw new Exception("hahhah");

            //            await Task.CompletedTask;
            //        }, 
            //        async (ex, data) =>
            //        {
            //            Console.WriteLine($"报错了------- : {data.Count}");
            //            await Task.CompletedTask;

            //            //throw new Exception("error error");
            //        }, async () =>
            //        {
            //            Console.WriteLine($"over");
            //            await Task.CompletedTask;
            //        });

            //    await Task.Delay(5000);
            //    provider.SetEnable(true, "abc");

            //    await provider.PublishQueueAsync("abc", new List<UserInfo>() { new UserInfo() });

            //});

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
