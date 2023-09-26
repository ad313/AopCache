using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;

namespace AopCache
{
    /// <summary>
    /// Aspect 扩展
    /// </summary>
    public static class AspectContextExtensions
    {
        /// <summary>
        /// 获取参数字典
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetParamsDictionary(this AspectContext context)
        {
            //得到方法的参数
            var pars = context.ProxyMethod.GetParameters();

            //设置参数名和值 加入字典
            var dicValue = new Dictionary<string, object>();
            for (var i = 0; i < pars.Length; i++)
            {
                dicValue.Add(pars[i].Name, context.Parameters[i]);
            }
            return dicValue;
        }

        /// <summary>
        /// 获取默认Key值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetDefaultKey(this AspectContext context)
        {
            return $"{context.ServiceMethod.DeclaringType}.{context.ImplementationMethod.Name}:{context.ServiceMethod.ToString()}";
        }

        /// <summary>
        /// 获取返回值类型
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Type GetReturnType(this AspectContext context)
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
        public static async Task<dynamic> GetReturnValue(this AspectContext context)
        {
            return context.IsAsync() ? await context.UnwrapAsyncReturnValue() : context.ReturnValue;
        }
    }
}
