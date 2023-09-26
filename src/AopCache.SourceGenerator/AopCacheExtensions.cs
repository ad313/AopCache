using AopCache;
using AopCache.Core.Abstractions;
using AopCache.Core.Implements;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtensions
    {
        public static IServiceCollection ServiceCollection { get; set; }

        public static IServiceCollection AddAopCache(this IServiceCollection services, Action<MemoryCacheOptions> memoryCacheOptions = null)
        {
            services.AddAopCacheUseMemory(memoryCacheOptions);
            return services.AddAopCacheCore();
        }

        public static IServiceCollection AddAopCache(this IServiceCollection services, Action<AopCacheOption> option)
        {
            ServiceCollection = services;

            if (option == null)
                return services.AddAopCache();

            option.Invoke(new AopCacheOption());

            return services.AddAopCacheCore();
        }

        public static void UseMemoryCacheProvider(this AopCacheOption option, Action<MemoryCacheOptions> setupAction = null)
        {
            ServiceCollection.AddAopCacheUseMemory(setupAction);
        }

        private static IServiceCollection AddAopCacheUseMemory(this IServiceCollection services, Action<MemoryCacheOptions> setupAction = null)
        {
            if (setupAction == null)
                services.AddMemoryCache();
            else
                services.AddMemoryCache(setupAction);

            return services.AddSingleton<IAopCacheProvider, MemoryCacheProvider>();
        }

        private static IServiceCollection AddAopCacheCore(this IServiceCollection services)
        {
            services.AddSingleton<ISerializerProvider, SerializerProvider>();
            services.AddTransient<AopCacheAttribute>();

            return services;
        }
    }

    public class AopCacheOption
    {

    }
}
