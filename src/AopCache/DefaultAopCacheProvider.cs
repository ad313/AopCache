using Microsoft.Extensions.Caching.Memory;
using System;

namespace AopCache
{
    /// <summary>
    /// aop 缓存默认实现
    /// </summary>
    public class DefaultAopCacheProvider : IAopCacheProvider
    {
        private static IMemoryCache _cache;
        
        public DefaultAopCacheProvider(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="type">数据类型</param>
        /// <returns></returns>
        public object Get(string key,Type type)
        {
            return string.IsNullOrWhiteSpace(key) ? null : _cache.Get(key);
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <param name="type">数据类型</param>
        /// <param name="absoluteExpiration">绝对过期实现</param>
        /// <returns></returns>
        public bool Set(string key, object value, Type type, DateTime absoluteExpiration)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return false;
            }
            
            _cache.Set(key, FastConvertHelper.Clone(value, type), new DateTimeOffset(absoluteExpiration));

            return true;
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _cache.Remove(key);
        }
    }
}
