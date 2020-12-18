using AopCache.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtentions
    {
        

        ///// <summary>
        ///// 注册 AopCache ，默认 自己传入实现缓存类，替换默认的MemoryCache
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="connectionString"></param>
        ///// <returns></returns>
        //public static void AddAopCacheUseCsRedisWithMessagePack(this IServiceCollection services, string connectionString)
        //{
        //    var csredis = new CSRedis.CSRedisClient(connectionString);
        //    RedisHelper.Initialization(csredis);
        //    services.AddSingleton<IAopCacheProvider, RedisCacheWithMessagePackProvider>();
        //    services.ConfigureDynamicProxy();
        //}
    }
}
