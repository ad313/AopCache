using AopCache.Core.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AopCache.Core.Implements
{
    /// <summary>
    /// Aop 内存缓存实现
    /// </summary>
    public class MemoryCacheProvider : IAopCacheProvider
    {
        private static IMemoryCache _cache;
        private readonly ISerializerProvider _serializerProvider;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="serializerProvider"></param>
        public MemoryCacheProvider(IMemoryCache cache, ISerializerProvider serializerProvider)
        {
            _cache = cache;
            _serializerProvider = serializerProvider;
        }
        
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="type">数据类型</param>
        /// <returns></returns>
        public async Task<object> Get(string key, Type type)
        {
            return await Task.FromResult(string.IsNullOrWhiteSpace(key) ? null : _cache.Get(key));
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <param name="type">数据类型</param>
        /// <param name="absoluteExpiration">绝对过期实现</param>
        /// <returns></returns>
        public async Task<bool> Set(string key, object value, Type type, DateTime absoluteExpiration)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return false;
            }

            _cache.Set(key, _serializerProvider.Clone(value, type), new DateTimeOffset(absoluteExpiration));

            return await Task.FromResult(true);
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