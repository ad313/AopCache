using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AopCache.Redis
{
    public class RedisCacheProvider : IAopCacheProvider
    {
        public object Get(string key, Type type)
        {
            return string.IsNullOrWhiteSpace(key) ? null : ToObject(RedisHelper.Get(key), type);
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            RedisHelper.Del(key);
        }

        public bool Set(string key, object value, Type type, DateTime absoluteExpiration)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return false;
            }
            return RedisHelper.Set(key, ToString(value, type), absoluteExpiration - DateTime.Now);
        }

        private string ToString(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        private object ToObject(string value, Type type)
        {
            return value == null ? null : JsonConvert.DeserializeObject(value, type);
        }
    }
}
