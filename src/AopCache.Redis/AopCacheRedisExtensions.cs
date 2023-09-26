using AopCache.Core.Abstractions;
using AopCache.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheRedisExtensions
    {
        public static void UseRedisCacheProvider(this AopCacheOption option, string connectionString)
        {
            RedisCacheProvider.Conn = connectionString;
            AopCacheExtensions.ServiceCollection.AddSingleton<IAopCacheProvider, RedisCacheProvider>();
        }
    }
}
