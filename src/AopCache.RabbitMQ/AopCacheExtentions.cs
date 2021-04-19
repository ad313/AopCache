using AopCache.EventBus.RabbitMQ;
using AopCache.Implements;
using System;
using DependencyRegistrator = AopCache.Runtime.DependencyRegistrator;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册AopCache
    /// </summary>
    public static partial class AopCacheExtentions
    {
        /// <summary>
        /// 注册 AopCache 缓存清理触发器
        /// </summary>
        /// <param name="option"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static void AddAopTriggerUseRabbitMqEventBus(this AopCacheOption option, Action<RabbitMqConfig> config)
        {
            //处理发布订阅
            new DependencyRegistrator().RegisterServices();
            DependencyRegistrator.ServiceCollection.AddEventBusUseRabbitMq(config);
            DependencyRegistrator.ServiceCollection.AddHostedService<SubscriberWorker>();
        }

        /// <summary>
        /// 注册 AopCache 缓存清理触发器
        /// </summary>
        /// <param name="option"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static void AddAopTriggerUseRabbitMqEventBus(this AopCacheOption option, RabbitMqConfig config)
        {
            //处理发布订阅
            new DependencyRegistrator().RegisterServices();
            DependencyRegistrator.ServiceCollection.AddEventBusUseRabbitMq(config);
            DependencyRegistrator.ServiceCollection.AddHostedService<SubscriberWorker>();
        }
    }
}