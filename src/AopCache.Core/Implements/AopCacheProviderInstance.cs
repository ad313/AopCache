using AopCache.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AopCache.Core.Implements
{
    public static class AopCacheProviderInstance
    {
        public static IServiceProvider ServiceProvider { get; set; }
        
        private static IAopCacheProvider _aopCacheProvider;

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="group">分组 默认 Default</param>
        /// <returns></returns>
        public static async Task<T> Get<T>(string key, string group = "Default")
        {
            Init();
            var data = await _aopCacheProvider.Get(FormatKey(key, group), typeof(T));
            return data == null ? default : (T)data;
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public static void Remove(string key, string group = "Default")
        {
            Init();
            _aopCacheProvider.Remove(FormatKey(key, group));
        }

        private static void Init()
        {
            if (ServiceProvider == null)
                throw new ArgumentNullException(nameof(ServiceProvider));

            _aopCacheProvider ??= ServiceProvider.GetService<IAopCacheProvider>();
        }

        private static string FormatKey(string key, string group)
        {
            return $"AopCache:{(string.IsNullOrWhiteSpace(group) ? "Default" : group)}:{key}";
        }
    }
}