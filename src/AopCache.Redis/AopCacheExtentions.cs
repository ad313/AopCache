using AopCache.Abstractions;
using AopCache.Implements;
using AopCache.Redis;
using AspectCore.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtentions
    {
        /// <summary>
        /// 注册 AopCache ，默认 自己传入实现缓存类，替换默认的MemoryCache
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static void AddAopCacheUseCsRedis(this IServiceCollection services, string connectionString)
        {
            var csredis = new CSRedis.CSRedisClient(connectionString);
            RedisHelper.Initialization(csredis);

            services.AddSingleton<ISerializerProvider, SerializerProvider>();
            services.AddSingleton<IAopCacheProvider, RedisCacheProvider>();

            services.ConfigureDynamicProxy();
        }

        /// <summary>
        /// 注册 AopCache ，默认 自己传入实现缓存类，替换默认的MemoryCache
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static void AddAopEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IAopEventBusProvider, RedisEventBusProvider>();
        }

    }
}
