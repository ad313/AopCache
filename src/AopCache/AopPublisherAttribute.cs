using AopCache.Abstractions;
using AopCache.Extensions;
using AspectCore.DependencyInjection;
using AspectCore.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AopCache
{
    /// <summary>
    /// Aop 发布事件
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AopPublisherAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// 发布或者订阅的Key值
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// 发布事件的消息来源
        /// </summary>
        public MessageSource MessageSource { get; set; } = MessageSource.NoParams;
        
        [FromServiceContext]
        private IEventBusProvider EventBusProvider { get; set; }
        
        /// <summary>
        /// 是否开启
        /// </summary>
        public static bool Enable { get; set; }

        /// <summary>
        /// 处理业务逻辑
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            //执行方法
            await next(context);

            if (!Enable)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Channel))
            {
                return;
            }

            var message = new Dictionary<string, object>();
            switch (MessageSource)
            {
                case MessageSource.InParams:
                    message = context.GetParamsDictionary();
                    break;
                case MessageSource.OutParams:
                    var result = await context.GetReturnValue();
                    message = new Dictionary<string, object>() { { "Result", result } };
                    break;
                case MessageSource.NoParams:

                    break;
                case MessageSource.Other:

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await EventBusProvider.PublishAsync(Channel, new EventMessageModel<Dictionary<string, object>>(message));
        }
    }

   
    /// <summary>
    /// 发布事件消息来源
    /// </summary>
    public enum MessageSource
    {
        /// <summary>
        /// 输入参数
        /// </summary>
        InParams = 1,
        /// <summary>
        /// 输出参数
        /// </summary>
        OutParams = 2,
        /// <summary>
        /// 无需参数
        /// </summary>
        NoParams = 3,
        /// <summary>
        /// 其他参数、自定义参数
        /// </summary>
        Other = 4
    }
}
