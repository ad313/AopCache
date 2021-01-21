using AopCache.Core.Abstractions;
using AopCache.Core.Implements;
using AopCache.EventBus.RabbitMQ;
using AopCache.Implements;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AopCache.Test.Base
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            HostingEnvironment = environment;
        }

        /// <summary>
        /// 
        /// </summary>
        public IHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // в╒╡А HttpContext иообнд
            services.AddHttpContextAccessor();

            services.AddSingleton<ISerializerProvider, SerializerProvider>();

            //services.AddAopCache(op =>
            //{
            //    op.UseMemoryCacheProvider();
            //    //op.AddAopTriggerUseMemoryEventBus();

            //    //op.UseCsRedisCacheProvider("192.168.1.110:32350,password=123456,defaultDatabase=5");
            //    op.AddAopTriggerUseRedisEventBus("192.168.1.110:32350,password=123456,defaultDatabase=5");
            //});



            services.AddEventBusUseRabbitMq(option =>
            {
                option.ExchangeName = Configuration.GetValue<string>("RabbitMQ:ExchangeName");
                option.HostName = Configuration.GetValue<string>("RabbitMQ:HostName");
                option.UserName = Configuration.GetValue<string>("RabbitMQ:UserName");
                option.Password = Configuration.GetValue<string>("RabbitMQ:Password");
                option.Port = Configuration.GetValue<int>("RabbitMQ:Port");
                option.VirtualHost = Configuration.GetValue<string>("RabbitMQ:VirtualHost");
                option.PrefetchSize = Configuration.GetValue<uint>("RabbitMQ:PrefetchSize");
                option.PrefetchCount = Configuration.GetValue<ushort>("RabbitMQ:PrefetchCount");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
        }
    }

}
