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
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);


            //注入打了标签的Service
            services.AddTransient<ITestService, TestService>();
            services.AddTransient<TestSingleClass>();

            //

            //自定义存储 这里xxx表示 IAopCacheProvider 的实现            
            //services.AddAopCache<xxx>();    


            //默认内存存储
            //返回IServiceProvider，由 AspectCore接管
            return services.AddAopCacheUseDefaultMemoryProvider();

            //此方法的内部实现，这里包装一层
            //if (setupAction == null)
            //{
            //    services.AddMemoryCache();
            //}
            //else
            //{
            //    services.AddMemoryCache(setupAction);
            //}
            //services.AddSingleton<IAopCacheProvider, DefaultAopCacheProvider>();
            //services.ConfigureDynamicProxy();
            //return services.BuildAspectInjectorProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
