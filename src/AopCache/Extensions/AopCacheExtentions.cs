using AopCache.Abstractions;
using AopCache.Implements;
using AspectCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System;
using AopCache.Runtime;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtentions
    {
        public static IServiceCollection AddAopCache(this IServiceCollection services, Action<MemoryCacheOptions> memoryCacheOptions = null)
        {
            services.AddAopCacheUssMemory(memoryCacheOptions);
            return services.AddAopCacheCore();
        }

        public static IServiceCollection AddAopCache(this IServiceCollection services, Action<AopCacheOption> option)
        {
            if (option == null)
                return services.AddAopCache();

            DependencyRegistrator.ServiceCollection = services;

            option.Invoke(new AopCacheOption());

            return services.AddAopCacheCore();
        }
        

        public static void AddCacheProviderUseMemory(this AopCacheOption option, Action<MemoryCacheOptions> memoryCacheOptions = null)
        {
            DependencyRegistrator.ServiceCollection.AddAopCacheUssMemory(memoryCacheOptions);
        }


        private static IServiceCollection AddAopCacheUssMemory(this IServiceCollection services, Action<MemoryCacheOptions> setupAction = null)
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
            return services.ConfigureDynamicProxy();
        }
    }

    public class AopCacheOption
    {

    }
}
