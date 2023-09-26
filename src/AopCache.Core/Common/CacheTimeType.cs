using System;

namespace AopCache.Core.Common
{
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

    public class CacheTimeHelper
    {
        /// <summary>
        /// 计算缓存的时间
        /// </summary>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static DateTime GetCacheNewTime(CacheTimeType type, int length)
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