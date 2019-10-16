using AspectCore.DynamicProxy;
using AspectCore.Injector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AopCache
{
    /// <summary>
    /// aop 缓存
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AopCacheAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// 指定缓存键值 可以附加参数 如 UserInfo_{model:Name}_{type}
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
        /// 每个方法 key中的参数
        /// </summary>
        private static readonly ConcurrentDictionary<MethodInfo, List<string>> AppendKeyParamters =
            new ConcurrentDictionary<MethodInfo, List<string>>();

        /// <summary>
        /// 包装 Task
        /// </summary>
        private static readonly MethodInfo TaskResultMethod;

        /// <summary>
        /// 异步锁
        /// </summary>
        private readonly AsyncLock _lock = new AsyncLock();

        [FromContainer]
        public IAopCacheProvider CacheProvider { get; set; }

        static AopCacheAttribute()
        {
            TaskResultMethod = typeof(Task).GetMethods().FirstOrDefault(p => p.Name == "FromResult" && p.ContainsGenericParameters);
        }

        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            var currentCacheKey = Key.FillValue(context.GetParamsDictionary(), context.GetDefaultKey());

            //返回值类型
            var returnType = context.GetReturnType();

            //从缓存取值
            var cacheValue = GetCahceValue(currentCacheKey, returnType, context);
            if (cacheValue != null) return;

            using (await _lock.LockAsync())
            {
                cacheValue = GetCahceValue(currentCacheKey, returnType, context);
                if (cacheValue == null)
                {
                    //执行方法
                    await next(context);

                    //获取缓存过期时间
                    var limitTime = GetCacheNewTime(Type, Length);

                    dynamic returnValue = await context.GetReturnValue();

                    //加入缓存
                    CacheProvider.Set(currentCacheKey, returnValue, returnType, limitTime);
                }
            }
        }
              
        /// <summary>
        /// 获取缓存，并处理返回值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private object GetCahceValue(string key, Type type, AspectContext context)
        {
            //从缓存取值
            var cacheValue = CacheProvider.Get(key, type);
            if (cacheValue != null)
            {
                context.ReturnValue = context.IsAsync()
                    ? TaskResultMethod.MakeGenericMethod(type).Invoke(null, new object[] { cacheValue })
                    : cacheValue;
            }
            return cacheValue;
        }

        /// <summary>
        /// 计算缓存的时间
        /// </summary>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DateTime GetCacheNewTime(CacheTimeType type, int length)
        {
            if (length <= 0) return DateTime.MaxValue;

            var limitTime = DateTime.Now;
            switch (type)
            {
                case CacheTimeType.Day:
                    limitTime = limitTime.AddDays(length);
                    break;
                case CacheTimeType.Hour:
                    limitTime = limitTime.AddHours(length);
                    break;
                case CacheTimeType.Minute:
                    limitTime = limitTime.AddMinutes(length);
                    break;
                case CacheTimeType.Second:
                    limitTime = limitTime.AddSeconds(length);
                    break;
            }

            return limitTime;
        }
    }
}
