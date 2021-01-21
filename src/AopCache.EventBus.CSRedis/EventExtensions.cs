using AopCache.Core.Abstractions;
using AopCache.Core.Implements;
using AopCache.EventBus.CSRedis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册 CSRedis EventBus
    /// </summary>
    public static partial class EventExtensions
    {
        /// <summary>
        /// 注册 AopCache 缓存清理触发器
        /// </summary>
        /// <param name="service"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static void AddEventBusUseCsRedis(this IServiceCollection service, string connectionString = null)
        {
            InitCsRedis(connectionString);

            service.AddSingleton<ISerializerProvider, SerializerProvider>();
            service.AddSingleton<IEventBusProvider, RedisEventBusProvider>();
        }

        private static void InitCsRedis(string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                RedisHelper.Initialization(new CSRedis.CSRedisClient(connectionString));
        }
    }
}
