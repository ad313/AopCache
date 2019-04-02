using AspectCore.DynamicProxy;
using AspectCore.Injector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
#if DEBUG
            Console.WriteLine($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name}");
#endif

            //key中附带的参数
            List<string> appendKeyArray = null;
            if (string.IsNullOrWhiteSpace(Key))
            {
                Key = $"{context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name}:{context.ServiceMethod.ToString()}";
            }
            else
            {
                if (!AppendKeyParamters.TryGetValue(context.ImplementationMethod, out appendKeyArray))
                {
                    if (Key.IndexOf("{", StringComparison.Ordinal) > -1)
                    {
                        appendKeyArray = new List<string>();

                        //获取key中附带的参数，格式：用 {} 包裹
                        var matchs = Regex.Matches(Key, @"\{\w*\:?\w*\}", RegexOptions.None);
                        foreach (Match match in matchs)
                        {
                            if (match.Success)
                            {
                                appendKeyArray.Add(match.Value.TrimStart('{').TrimEnd('}'));
                            }
                        }
                    }

                    AppendKeyParamters.TryAdd(context.ImplementationMethod, appendKeyArray);
                }
            }

            var currentCacheKey = Key;

            if (appendKeyArray != null && appendKeyArray.Count > 0)
            {
                //得到方法的参数
                var pars = context.ProxyMethod.GetParameters();

                //设置参数名和值 加入字典
                var dicValue = new Dictionary<string, object>();
                for (var i = 0; i < pars.Length; i++)
                {
                    dicValue.Add(pars[i].Name, context.Parameters[i]);
                }

                foreach (var key in appendKeyArray)
                {
                    //参数包含:
                    if (key.Contains(":"))
                    {
                        var arr = key.Split(':');
                        var keyFirst = arr[0];
                        var keySecond = arr[1];

                        if (!dicValue.TryGetValue(keyFirst, out object v))
                        {
                            throw new Exception($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} " +
                                                $"不包含参数 {keyFirst}");
                        }

                        //var ob = JObject.FromObject(v);
                        var ob = FastConvertHelper.ToDictionary(v);
                        if (!ob.TryGetValue(keySecond, out object tokenValue))
                        {
                            throw new Exception($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} {keyFirst} " +
                                                $"不包含参数 {keySecond}");
                        }

                        currentCacheKey = currentCacheKey.Replace("{" + key + "}", tokenValue.ToString());
                    }
                    else
                    {
                        if (!dicValue.TryGetValue(key, out object value))
                        {
                            throw new Exception($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} " +
                                                $"不包含参数 {key}");
                        }

                        currentCacheKey = currentCacheKey.Replace("{" + key + "}", value.ToString());
                    }
                }
            }

#if DEBUG
            Console.WriteLine($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} 键值：{currentCacheKey}");
#endif

            //返回值类型
            var returnType = context.IsAsync()
                ? context.ServiceMethod.ReturnType.GetGenericArguments().First()
                : context.ServiceMethod.ReturnType;

            //从缓存取值
            var cacheValue = CacheProvider.Get(currentCacheKey, returnType);
            if (cacheValue != null)
            {
#if DEBUG
                Console.WriteLine($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} 缓存有效");
#endif

                context.ReturnValue = context.IsAsync()
                    ? TaskResultMethod.MakeGenericMethod(returnType).Invoke(null, new object[] { cacheValue })
                    : cacheValue;

                return;
            }

            using (await _lock.LockAsync())
            {
                cacheValue = CacheProvider.Get(currentCacheKey, returnType);
                if (cacheValue != null)
                {
                    context.ReturnValue = context.IsAsync()
                        ? TaskResultMethod.MakeGenericMethod(returnType).Invoke(null, new object[] { cacheValue })
                        : cacheValue;

#if DEBUG
                    Console.WriteLine($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} 缓存有效，直接返回");
#endif
                }
                else
                {
                    //执行方法
                    await next(context);

                    //获取缓存过期时间
                    var limitTime = GetCacheNewTime(Type, Length);

                    dynamic returnValue = context.IsAsync() ? await context.UnwrapAsyncReturnValue() : context.ReturnValue;

                    //加入缓存
                    CacheProvider.Set(currentCacheKey, returnValue, returnType, limitTime);

#if DEBUG
                    Console.WriteLine($"--AopCache {context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name} " +
                                                             $"当前无缓存，加入缓存，过期时间：{limitTime.ToString("yyyy-MM-dd HH:mm:ss:fff")}");
#endif
                }
            }
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
