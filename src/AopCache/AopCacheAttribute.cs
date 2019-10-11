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
            //key中附带的参数
            List<string> appendKeyArray = null;
            if (string.IsNullOrWhiteSpace(Key))
            {
                Key = GetDefaultKey(context);
            }
            else
            {
                //处理附带的参数
                FormatAppendKeyParamters(context.ImplementationMethod, out appendKeyArray);
            }

            //处理参数 填充值
            var currentCacheKey = FormatCurrentCacheKey(context, appendKeyArray, Key);

            //返回值类型
            var returnType = GetReturnType(context);

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

                    dynamic returnValue = await GetReturnValue(context);

                    //加入缓存
                    CacheProvider.Set(currentCacheKey, returnValue, returnType, limitTime);
                }
            }
        }

        /// <summary>
        /// 获取默认Key值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string GetDefaultKey(AspectContext context)
        {
            return $"{context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name}:{context.ServiceMethod.ToString()}";
        }
        
        /// <summary>
        /// 处理附加的参数
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="appendKeyArray"></param>
        private void FormatAppendKeyParamters(MethodInfo methodInfo, out List<string> appendKeyArray)
        {
            if (AppendKeyParamters.TryGetValue(methodInfo, out appendKeyArray)) return;
            if (Key.IndexOf("{", StringComparison.Ordinal) > -1)
            {
                appendKeyArray = new List<string>();

                //获取key中附带的参数，格式：用 {} 包裹
                var matchs = Regex.Matches(Key, @"\{\w*\:?\w*\}", RegexOptions.None);
                foreach (Match match in matchs)
                {
                    if (!match.Success)
                    {
                        continue;
                    }
                    appendKeyArray.Add(match.Value.TrimStart('{').TrimEnd('}'));
                }
            }

            AppendKeyParamters.TryAdd(methodInfo, appendKeyArray);
        }

        /// <summary>
        /// 处理附加参数，给占位符填充值
        /// </summary>
        /// <param name="context"></param>
        /// <param name="appendKeyArray"></param>
        /// <param name="currentCacheKey"></param>
        /// <returns></returns>
        private string FormatCurrentCacheKey(AspectContext context, List<string> appendKeyArray, string currentCacheKey)
        {
            if (appendKeyArray == null || appendKeyArray.Count <= 0) return currentCacheKey;
            
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

            return currentCacheKey;
        }

        /// <summary>
        /// 获取返回值类型
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Type GetReturnType(AspectContext context)
        {
            return context.IsAsync()
                ? context.ServiceMethod.ReturnType.GetGenericArguments().First()
                : context.ServiceMethod.ReturnType;
        }

        /// <summary>
        /// 获取返回值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<dynamic> GetReturnValue(AspectContext context)
        {
            return context.IsAsync() ? await context.UnwrapAsyncReturnValue() : context.ReturnValue;
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
