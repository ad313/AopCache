using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AopCache.Common
{
    /// <summary>
    /// 对象转换成字典
    /// </summary>
    public class FastConvertHelper
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
    }
    
}
