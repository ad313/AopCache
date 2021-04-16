using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AopCache.Test2
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AopCache.Test2", Version = "v1" });
            });

            services.AddHttpClient();

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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AopCache.Test2 v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
