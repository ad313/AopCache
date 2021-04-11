using AopCache.Core.Abstractions;
using AopCache.Core.Implements;
using AopCache.EventBus.RabbitMQ;
using System;
using AopCache.EventBus.RabbitMQ.Rpc;
using AopCache.EventBus.RabbitMQ.Runtime;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 注册 RabbitMQ EventBus
    /// </summary>
    public static partial class EventExtensions
    {
        /// <summary>
        /// 注册 RabbitMQ EventBus
        /// </summary>
        /// <param name="service"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static void AddEventBusUseRabbitMq(this IServiceCollection service, Action<RabbitMqConfig> option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            var config = new RabbitMqConfig()
            {
                VirtualHost = "/",
                PrefetchSize = 0,
                PrefetchCount = 1
            };
            option.Invoke(config);
            config.Check();

            service.AddSingleton(config);
            service.AddSingleton<ISerializerProvider, SerializerProvider>();
            service.AddTransient<RabbitMqClientProvider>();
            service.AddSingleton<IEventBusProvider, RabbitEventBusProvider>();

            //init rpc
            new DependencyRegistrator().RegisterServices();
            service.AddHostedService<RpcServerInitHost>();
        }
    }
}