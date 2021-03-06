﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AopCache.Core.Common
{
    /// <summary>
    /// 对象转换成字典
    /// </summary>
    public class Helpers
    {
        #region Dictionary

        private static readonly ConcurrentDictionary<Type, Func<object, Dictionary<string, object>>> DictionaryCache =
           new ConcurrentDictionary<Type, Func<object, Dictionary<string, object>>>();


        public static Dictionary<string, object> ToDictionary(object obj)
        {
            var type = obj.GetType();
            if (!DictionaryCache.TryGetValue(type, out Func<object, Dictionary<string, object>> getter))
            {
                getter = CreateDictionaryGenerator(type);

                DictionaryCache.TryAdd(type, getter);
            }

            return getter(obj);
        }


        private static Func<object, Dictionary<string, object>> CreateDictionaryGenerator(Type type)
        {
            var dm = new DynamicMethod($"Dictionary{Guid.NewGuid()}", typeof(Dictionary<string, object>), new[] { typeof(object) }, type, true);
            ILGenerator il = dm.GetILGenerator();
            il.DeclareLocal(typeof(Dictionary<string, object>));
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);

            foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string columnName = item.Name;
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, columnName);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, item.GetGetMethod());
                if (item.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, item.PropertyType);
                }
                il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, object>).GetMethod("Add"));
            }
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            return (Func<object, Dictionary<string, object>>)dm.CreateDelegate(typeof(Func<object, Dictionary<string, object>>));
        }

        #endregion

        #region List

        /// <summary>
        /// 把list按照指定数量分隔
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static List<List<T>> SplitList<T>(List<T> list, int length)
        {
            if (list == null || list.Count <= 0 || length <= 0)
            {
                return new List<List<T>>();
            }

            var result = new List<List<T>>();
            var count = list.Count / length;
            count += list.Count % length > 0 ? 1 : 0;
            for (var i = 0; i < count; i++)
            {
                result.Add(list.Skip(i * length).Take(length).ToList());
            }
            return result;
        } 

        #endregion
    }
    
}
