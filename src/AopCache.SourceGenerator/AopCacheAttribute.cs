using AopCache.Common;
using AopCache.Core.Abstractions;
using AopCache.Core.Common;
using Mic.Aop;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AopCache
{
    /// <summary>
    /// Aop 缓存
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AopCacheAttribute : AopInterceptor
    {
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
        /// 当前完整Key，包含参数
        /// </summary>
        protected string FillKey { get; set; }

        /// <summary>
        /// 缓存操作类
        /// </summary>
        public IAopCacheProvider CacheProvider { get; set; }

        public AopCacheAttribute()
        {
            HasAopNext = false;
            HasActualNext = false;
            HasAfter = false;
        }

        public override AopContext Before(AopContext context)
        {
            CacheProvider = context.ServiceProvider.GetRequiredService<IAopCacheProvider>();

            FillKey = Key.FillValue(context.MethodInputParam);
            FillKey = FormatPrefix(FillKey);
            
            //从缓存取值 
            var cache = CacheProvider.Get(FillKey, context.ReturnType).GetAwaiter().GetResult();
            if (cache != null)
            {
                context.ReturnValue = cache;
                HasAopNext = false;
                HasActualNext = false;
            }
            else
            {
                HasAopNext = true;
                HasActualNext = true;
            }

            return context;
        }

        /// <summary>执行前操作，异步方法调用</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async ValueTask<AopContext> BeforeAsync(AopContext context)
        {
            CacheProvider = context.ServiceProvider.GetRequiredService<IAopCacheProvider>();

            FillKey = Key.FillValue(context.MethodInputParam);
            FillKey = FormatPrefix(FillKey);
            
            //从缓存取值 
            var cache = await CacheProvider.Get(FillKey, context.ReturnType);
            if (cache != null)
            {
                context.ReturnValue = cache;
                HasAopNext = false;
                HasActualNext = false;
            }
            else
            {
                HasAopNext = true;
                HasActualNext = true;
            }

            return context;
        }

        public override AopContext Next(AopContext context)
        {
            context = base.Next(context);
            if (context.Exception != null)
                throw ExceptionDispatchInfo.Capture(context.Exception).SourceException;

            //获取缓存过期时间
            var limitTime = CacheTimeHelper.GetCacheNewTime(Type, Length);

            //加入缓存
            CacheProvider.Set(FillKey, context.ReturnValue, context.ReturnType, limitTime);
            
            return context;
        }

        public override async ValueTask<AopContext> NextAsync(AopContext context)
        {
            context = await base.NextAsync(context);
            if (context.Exception != null)
                throw ExceptionDispatchInfo.Capture(context.Exception).SourceException;

            //获取缓存过期时间
            var limitTime = CacheTimeHelper.GetCacheNewTime(Type, Length);

            //加入缓存
            await CacheProvider.Set(FillKey, context.ReturnValue, context.ReturnType, limitTime);

            return context;
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
    }
}
