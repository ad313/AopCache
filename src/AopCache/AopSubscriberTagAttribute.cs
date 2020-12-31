using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AopCache.Common;

namespace AopCache
{
    /// <summary>
    /// aop 订阅标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AopSubscriberTagAttribute : Attribute
    {
        /// <summary>
        /// 订阅的频道值
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// 触发类型
        /// </summary>
        public ActionType ActionType { get; set; } = ActionType.DeleteByKey;
        
        /// <summary>
        /// 输入参数映射
        /// </summary>
        public string Map { get; set; }

        public static readonly Dictionary<string, Dictionary<string, string>> MapDictionary = new Dictionary<string, Dictionary<string, string>>();

        public static List<string> ChannelList { get; set; } = new List<string>();

        public static List<MethodInfo> MethodList { get; set; } = new List<MethodInfo>();

        public string GetMapDictionaryKey() => $"{Channel}_{Map}";

        public string GetKey(string key, Dictionary<string, object> dic)
        {
            if (dic == null || !dic.Any())
                return key;

            if (!MapDictionary.TryGetValue(GetMapDictionaryKey(), out Dictionary<string, string> mapDic))
                return key.FillValue(dic);

            return ParamsHelper.FillParamValues(key, mapDic, dic);
        }
    }

    /// <summary>
    /// 操作类型
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// 通过 key 删除缓存 key
        /// </summary>
        DeleteByKey = 1,

        /// <summary>
        /// 通过 group 删除缓存 group
        /// </summary>
        DeleteByGroup = 2
    }
}