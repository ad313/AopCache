using System;
using System.Collections.Generic;
using System.Text;
using AspectCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AopCache
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static class AopCacheExtentions
    {
        /// <summary>
        /// 注册 AopCache ，默认 MemoryCache
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction">Memory </param>
        /// <returns></returns>
        public static IServiceProvider AddAopCacheUseDefaultMemoryProvider(this IServiceCollection services, Action<MemoryCacheOptions> setupAction = null)
        {
            if (setupAction == null)
            {
                services.AddMemoryCache();
            }
            else
            {
                services.AddMemoryCache(setupAction);
            }

            services.AddSingleton<IAopCacheProvider, DefaultAopCacheProvider>();

            services.ConfigureDynamicProxy();

            return services.BuildAspectInjectorProvider();
        }

        /// <summary>
        /// 注册 AopCache ，默认 自己传入实现缓存类，替换默认的MemoryCache
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceProvider AddAopCache<T>(this IServiceCollection services) where T : class, IAopCacheProvider
        {
            services.AddSingleton<IAopCacheProvider, T>();

            services.ConfigureDynamicProxy();

            return services.BuildAspectInjectorProvider();
        }
    }
}
