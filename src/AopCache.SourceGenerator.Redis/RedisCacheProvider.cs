using System;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using FreeRedis;

namespace AopCache.SourceGenerator.Redis
{
    /// <summary>
    /// Aop CsRedis 缓存实现
    /// </summary>
    public class RedisCacheProvider : IAopCacheProvider
    {
        public static RedisClient RedisClient { get; private set; }

        public static string Conn { get; set; }

        private readonly ISerializerProvider _serializerProvider;

        public RedisCacheProvider(ISerializerProvider serializerProvider)
        {
            _serializerProvider = serializerProvider;
        }
        
        public async Task<object> Get(string key, Type type)
        {
            CheckInit();
            return string.IsNullOrWhiteSpace(key) ? null : _serializerProvider.Deserialize(await RedisClient.GetAsync<byte[]>(key), type);
        }

        public async Task<bool> Set(string key, object value, Type type, DateTime absoluteExpiration)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return false;
            }

            CheckInit();

            await RedisClient.SetAsync(key, _serializerProvider.SerializeBytes(value, type), (int)(absoluteExpiration - DateTime.Now).TotalSeconds);

            return true;
        }

        public void Remove(string key)
        {
            CheckInit();
            if (string.IsNullOrWhiteSpace(key)) return;
            RedisClient.Del(key);
        }

        private void CheckInit()
        {
            if (RedisClient != null)
                return;

            if (string.IsNullOrWhiteSpace(Conn))
                throw new ArgumentNullException(nameof(Conn));

            RedisClient = new RedisClient(Conn)
            {
                Serialize = obj => _serializerProvider.Serialize(obj),
                Deserialize = (json, type) => _serializerProvider.Deserialize(json, type),
            };
        }
    }
}