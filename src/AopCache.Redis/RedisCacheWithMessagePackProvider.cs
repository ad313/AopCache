//using System;
//using System.Threading.Tasks;
//using AopCache.Abstractions;

//namespace AopCache.Redis
//{
//    public class RedisCacheWithMessagePackProvider : IAopCacheProvider
//    {
//        public async Task<object> Get(string key, Type type)
//        {
//            return string.IsNullOrWhiteSpace(key) ? null : SerializerHandler.BytesToObject(RedisHelper.Get<byte[]>(key), type);
//        }

//        public void Remove(string key)
//        {
//            if (string.IsNullOrWhiteSpace(key)) return;
//            RedisHelper.Del(key);
//        }

//        public async Task<bool> Set(string key, object value, Type type, DateTime absoluteExpiration)
//        {
//            if (string.IsNullOrWhiteSpace(key) || value == null)
//            {
//                return false;
//            }
//            return await RedisHelper.SetAsync(key, SerializerHandler.ToBytes(value, type), absoluteExpiration - DateTime.Now);
//        }
//    }
//}