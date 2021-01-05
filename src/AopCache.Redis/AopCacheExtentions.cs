using AopCache.Abstractions;
using AopCache.Implements;
using AopCache.Redis;
using AopCache.Runtime;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtentions
    {
        public static void UseCsRedisCacheProvider(this AopCacheOption option, string connectionString)
        {
            InitCsRedis(connectionString);
            DependencyRegistrator.ServiceCollection.AddSingleton<IAopCacheProvider, RedisCacheProvider>();
        }

        /// <summary>
        /// 注册 AopCache 缓存清理触发器
        /// </summary>
        /// <param name="option"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static void AddAopTriggerUseRedisEventBus(this AopCacheOption option, string connectionString = null)
        {
            InitCsRedis(connectionString);

            //处理发布订阅
            new DependencyRegistrator().RegisterServices();
            DependencyRegistrator.ServiceCollection.AddSingleton<IEventBusProvider, RedisEventBusProvider>();
            DependencyRegistrator.ServiceCollection.AddHostedService<SubscriberWorker>();
        }

        private static void InitCsRedis(string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                RedisHelper.Initialization(new CSRedis.CSRedisClient(connectionString));
        }
    }
}
