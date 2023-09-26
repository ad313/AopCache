using AopCache.Common;
using AopCache.Core.Abstractions;
using AopCache.Core.Common;
using AspectCore.DependencyInjection;
using AspectCore.DynamicProxy;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AopCache
{
    /// <summary>
    /// Aop 缓存
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AopCacheAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// 保留字段
        /// </summary>
        public bool AopTag { get; set; }

        /// <summary>
        /// 指定缓存键值分组
        /// </summary>
        public string Group { get; set; } = "Default";

        /// <summary>
        /// 指定缓存键值 可以附加参数 如 UserInfo_{model.Name}_{type}
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 缓存时间类型 默认秒
        /// </summary>
        public CacheTimeType Type { get; set; } = CacheTimeType.Second;

        /// <summary>
        /// 时间长度 与时间类型 配合使用 0 表示永不过期
        /// </summary>
        public int Length { get; set; } = 0;

        /// <summary>
        /// 缓存失效后调用方法时 是否使用线程锁，默认true
        /// </summary>
        public bool ThreadLock { get; set; } = false;
        
        /// <summary>
        /// 包装 Task
        /// </summary>
        private static readonly MethodInfo TaskResultMethod;

        /// <summary>
        /// 异步锁
        /// </summary>
        private static readonly AsyncLock Lock = new AsyncLock();

        /// <summary>
        /// 存储 group key
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>> GroupDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>>();

        /// <summary>
        /// 缓存操作类
        /// </summary>
        [FromServiceContext]
        public IAopCacheProvider CacheProvider { get; set; }

        static AopCacheAttribute()
        {
            TaskResultMethod = typeof(Task).GetMethods().FirstOrDefault(p => p.Name == "FromResult" && p.ContainsGenericParameters);
        }

        /// <summary>
        /// 处理业务逻辑
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            var currentCacheKey = Key.FillValue(context.GetParamsDictionary());

            currentCacheKey = FormatPrefix(currentCacheKey);

            //返回值类型
            var returnType = context.GetReturnType();

            //从缓存取值
            var cacheValue = await GetCahceValue(currentCacheKey, returnType, context);
            if (cacheValue != null) return;
            
            //不加锁，直接返回
            if (!ThreadLock)
            {
                await GetDirectValueWithSetCache(context, next, currentCacheKey, returnType);
                return;
            }

            using (await Lock.LockAsync())
            {
                cacheValue = await GetCahceValue(currentCacheKey, returnType, context);
                if (cacheValue != null) return;

                await GetDirectValueWithSetCache(context, next, currentCacheKey, returnType);
            }
        }

        /// <summary>
        /// 处理Key前缀
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string FormatPrefix(string key)
        {
            return $"AopCache:{(string.IsNullOrWhiteSpace(Group) ? "Default" : Group)}:{key}";
        }

        /// <summary>
        /// 获取缓存，并处理返回值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<object> GetCahceValue(string key, Type type, AspectContext context)
        {
            //从缓存取值
            var cacheValue = await CacheProvider.Get(key, type);
            if (cacheValue != null)
            {
                context.ReturnValue = context.IsAsync()
                    ? TaskResultMethod.MakeGenericMethod(type).Invoke(null, new object[] { cacheValue })
                    : cacheValue;
            }
            return cacheValue;
        }

        /// <summary>
        /// 直接调用方法，并把结果加入缓存
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <param name="key">缓存key</param>
        /// <param name="type">缓存值类型</param>
        /// <returns></returns>
        public async Task GetDirectValueWithSetCache(AspectContext context, AspectDelegate next, string key, Type type)
        {
            //执行方法
            await next(context);

            //获取缓存过期时间
            var limitTime = CacheTimeHelper.GetCacheNewTime(Type, Length);
            var value = await context.GetReturnValue();

            //加入缓存
            await CacheProvider.Set(key, value, type, limitTime);
        }
    }
}
