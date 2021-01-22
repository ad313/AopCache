using AopCache.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AopCache
{
    /// <summary>
    /// Aop 缓存订阅标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AopSubscriberAttribute : Attribute
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

        /// <summary>
        /// 参数映射字典
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> MapDictionary = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// 订阅的频道列表
        /// </summary>
        public static List<string> ChannelList { get; set; } = new List<string>();

        /// <summary>
        /// 订阅的方法列表
        /// </summary>
        public static List<MethodInfo> MethodList { get; set; } = new List<MethodInfo>();

        /// <summary>
        /// 获取 频道-输入参数映射 Key
        /// </summary>
        /// <returns></returns>
        public string GetMapDictionaryKey() => $"{Channel}_{Map}";

        /// <summary>
        /// 获取处理参数后的key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dic"></param>
        /// <returns></returns>
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

        ///// <summary>
        ///// 通过 group 删除缓存 group [暂未支持]
        ///// </summary>
        //DeleteByGroup = 2
    }
}