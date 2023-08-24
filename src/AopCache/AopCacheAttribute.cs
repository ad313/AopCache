using AopCache.Common;
using AopCache.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SaiLing.Aop;
using System;
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
            CacheProvider = context.ServiceProvider.GetService<IAopCacheProvider>();

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
            CacheProvider = context.ServiceProvider.GetService<IAopCacheProvider>();

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

            //获取缓存过期时间
            var limitTime = GetCacheNewTime(Type, Length);

            //加入缓存
            CacheProvider.Set(FillKey, context.ReturnValue, context.ReturnType, limitTime);
            
            return context;
        }

        public override async ValueTask<AopContext> NextAsync(AopContext context)
        {
            context = await base.NextAsync(context);

            //获取缓存过期时间
            var limitTime = GetCacheNewTime(Type, Length);

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

    /// <summary>
    /// 缓存的时间类型
    /// </summary>
    public enum CacheTimeType
    {
        /// <summary>
        /// 天
        /// </summary>
        Day = 1,
        /// <summary>
        /// 小时
        /// </summary>
        Hour = 2,
        /// <summary>
        /// 分钟
        /// </summary>
        Minute = 3,
        /// <summary>
        /// 秒
        /// </summary>
        Second = 4
    }
}
