using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddControllersWithViews().SetCompatibilityVersion(CompatibilityVersion.Latest);


            //注入打了标签的Service
            services.AddTransient<ITestService, TestService>();
            services.AddTransient<TestSingleClass>();

            ////注入打了标签的Service
            //services.AddTransient<ITestService, TestService_g>();
            //services.AddTransient<TestSingleClass, TestSingleClass_g>();


            services.AddAopCache(option =>
            {
                option.UseMemoryCacheProvider();
                //option.UseRedisCacheProvider("192.168.1.80:30985,password=123456,defaultDatabase=5");
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

            //hostLifetime.ApplicationStarted.Register(async () =>
            //{

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
