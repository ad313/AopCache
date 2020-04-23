using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            //services.AddAopCacheUseDefaultMemoryProvider();

            //redis实现 
            //Newtonsoft
            //services.AddAopCacheUseCsRedis("192.168.0.243:16379,password=123456,defaultDatabase=3");
            //MessagePack
            services.AddAopCacheUseCsRedisWithMessagePack("192.168.0.243:16379,password=123456,defaultDatabase=3");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
