using System;
using System.Threading.Tasks;

namespace AopCache.Core.Abstractions
{
    /// <summary>
    /// Aop 缓存接口
    /// </summary>
    public interface IAopCacheProvider
    {
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="type">数据类型</param>
        /// <returns></returns>
        Task<object> Get(string key, Type type);

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <param name="type">数据类型</param>
        /// <param name="absoluteExpiration">绝对过期实现</param>
        /// <returns></returns>
        Task<bool> Set(string key, object value, Type type, DateTime absoluteExpiration);

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        void Remove(string key);
    }
}
