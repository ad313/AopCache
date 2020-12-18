using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AopCache.Common
{
    /// <summary>
    /// 处理参数辅助类
    /// </summary>
    public static class ParamsHelper
    {
        /// <summary>
        /// 每个方法 key中的参数
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<string>> KeyParamtersCache = new ConcurrentDictionary<string, List<string>>();

        /// <summary>
        /// 参数分隔符
        /// </summary>
        private const char Separator = '.';

        /// <summary>
        /// 格式化字符串中的占位符，用给定的参数值填充
        /// </summary>
        /// <param name="source">原始字符串</param>
        /// <param name="paramDictionary">参数数据字典</param>
        /// <returns></returns>
        public static string FillValue(this string source, Dictionary<string, object> paramDictionary)
        {
            if (string.IsNullOrWhiteSpace(source) || paramDictionary == null || paramDictionary.Count <= 0)
                return source;

            source = source.Replace(":", Separator.ToString());

            //key中的参数
            var keys = GetKeyParamters(source);

            //处理参数 填充值
            return FillParamValues(source, keys, paramDictionary);
        }

        /// <summary>
        /// 处理附加的参数
        /// </summary>
        /// <param name="source"></param>
        private static List<string> GetKeyParamters(string source)
        {
            if (KeyParamtersCache.TryGetValue(source, out List<string> keyArray))
                return keyArray;

            keyArray = GetKeyParamtersInternal(source);

            KeyParamtersCache.TryAdd(source, keyArray);

            return keyArray;
        }

        /// <summary>
        /// 正则匹配参数，返回参数数组
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static List<string> GetKeyParamtersInternal(string source)
        {
            var keyArray = new List<string>();

            //获取key中附带的参数，格式：用 {} 包裹
            var matchs = Regex.Matches(source, @"\{\w*\" + Separator + @"?\w*\}", RegexOptions.None);
            foreach (Match match in matchs)
            {
                if (!match.Success)
                {
                    continue;
                }
                keyArray.Add(match.Value.TrimStart('{').TrimEnd('}'));
            }

            return keyArray;
        }

        /// <summary>
        /// 处理附加参数，给占位符填充值
        /// </summary>
        /// <param name="source">原始字符串</param>
        /// <param name="keys">附加的参数名称数组</param>
        /// <param name="pars">参数字段</param>
        /// <returns></returns>
        public static string FillParamValues(string source, List<string> keys, Dictionary<string, object> pars)
        {
            if (keys == null || keys.Count <= 0) 
                return source;

            foreach (var key in keys)
            {
                //参数包含:
                if (key.Contains(Separator))
                {
                    var parts = key.Split(Separator);
                    var firstKey = parts[0];
                    var secondKey = parts[1];

                    if (!pars.TryGetValue(firstKey, out object firstValue) || firstValue == null)
                        continue;

                    var secondValue = "";

                    if (firstValue is JsonElement json)
                    {
                        if (!json.TryGetProperty(secondKey, out JsonElement value))
                            continue;

                        secondValue = value.ToString();
                    }
                    else
                    {
                        if (!FastConvertHelper.ToDictionary(firstValue).TryGetValue(secondKey, out object second) || second == null)
                            continue;

                        secondValue = second.ToString();
                    }
                    
                    source = source.Replace("{" + key + "}", secondValue);
                }
                else
                {
                    if (!pars.TryGetValue(key, out object value) || value == null)
                        continue;

                    source = source.Replace("{" + key + "}", value.ToString());
                }
            }

            return source;
        }

        /// <summary>
        /// 处理附加参数，给占位符填充值
        /// </summary>
        /// <param name="source">原始字符串</param>
        /// <param name="dicMaps">附加的参数名称数组</param>
        /// <param name="pars">参数字段</param>
        /// <returns></returns>
        public static string FillParamValues(string source, Dictionary<string,string> dicMaps, Dictionary<string, object> pars)
        {
            if (dicMaps == null || dicMaps.Count <= 0)
                return source;

            foreach (var key in dicMaps)
            {
                //参数包含:
                if (key.Value.Contains(Separator))
                {
                    var parts = key.Value.Split(Separator);
                    var firstKey = parts[0];
                    var secondKey = parts[1];

                    if (!pars.TryGetValue(firstKey, out object firstValue) || firstValue == null)
                        continue;

                    var secondValue = "";

                    if (firstValue is JsonElement json)
                    {
                        if (!json.TryGetProperty(secondKey, out JsonElement value))
                            continue;

                        secondValue = value.ToString();
                    }
                    else
                    {
                        if (!FastConvertHelper.ToDictionary(firstValue).TryGetValue(secondKey, out object second) || second == null)
                            continue;

                        secondValue = second.ToString();
                    }

                    source = source.Replace("{" + key.Key + "}", secondValue);
                }
                else
                {
                    if (!pars.TryGetValue(key.Value, out object value) || value == null)
                        continue;

                    source = source.Replace("{" + key.Key + "}", value.ToString());
                }
            }

            return source;
        }

        ///// <summary>
        ///// 处理附加参数，给占位符填充值
        ///// </summary>
        ///// <param name="source">原始字符串</param>
        ///// <param name="keys">附加的参数名称数组</param>
        ///// <param name="pars">参数字段</param>
        ///// <returns></returns>
        //public static string FillParamValues2(string source, List<string> keys, Dictionary<string, object> pars)
        //{
        //    if (keys == null || keys.Count <= 0)
        //        return source;

        //    foreach (var key in keys)
        //    {
        //        //参数包含:
        //        if (key.Contains(Separator))
        //        {
        //            var parts = key.Split(Separator);
        //            var firstKey = parts[0];
        //            var secondKey = parts[1];

        //            if (!pars.TryGetValue(firstKey, out object firstValue) || firstValue == null)
        //                continue;

        //            if (firstValue is JsonElement json)
        //            {
        //                if (!json.TryGetProperty(secondKey, out JsonElement value))
        //                    continue;

        //                source = source.Replace("{" + key + "}", value.ToString());
        //            }
        //            else
        //            {
        //                if (!FastConvertHelper.ToDictionary(firstValue).TryGetValue(secondKey, out object secondValue) || secondValue == null)
        //                    continue;

        //                source = source.Replace("{" + key + "}", secondValue.ToString());
        //            }
        //        }
        //        else
        //        {
        //            if (!pars.TryGetValue(key, out object value) || value == null)
        //                continue;

        //            source = source.Replace("{" + key + "}", value.ToString());
        //        }
        //    }

        //    return source;
        //}
    }
}
