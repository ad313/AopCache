using System;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;

namespace AopCache.Redis
{
    /// <summary>
    /// Aop CsRedis 缓存实现
    /// </summary>
    public class RedisCacheProvider : IAopCacheProvider
    {
        private readonly ISerializerProvider _serializerProvider;

        public RedisCacheProvider(ISerializerProvider serializerProvider)
        {
            _serializerProvider = serializerProvider;
        }

        public async Task<object> Get(string key, Type type)
        {
            return string.IsNullOrWhiteSpace(key) ? null : _serializerProvider.Deserialize(await RedisHelper.GetAsync<byte[]>(key), type);
        }

        public async Task<bool> Set(string key, object value, Type type, DateTime absoluteExpiration)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return false;
            }

            return await RedisHelper.SetAsync(key, _serializerProvider.SerializeBytes(value, type), absoluteExpiration - DateTime.Now);
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            RedisHelper.Del(key);
        }
    }
}

