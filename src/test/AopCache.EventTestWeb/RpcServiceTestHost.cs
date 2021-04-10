using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AopCache.Core.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AopCache.EventTestWeb
{
    public class RpcServiceTestHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitmqRpcService _rabbitmqRpcService;

        public RpcServiceTestHost(IEventBusProvider eventBusProvider, RabbitMqConfig config,ISerializerProvider serializerProvider,IServiceProvider serviceProvider, IRabbitmqRpcService rabbitmqRpcService)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;
            _serviceProvider = serviceProvider;
            _rabbitmqRpcService = rabbitmqRpcService;
        }
        


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            //var o = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, typeof(TestRpcService));

            //var method = typeof(TestRpcService).GetMethod("Test1");



            //var result = method.Invoke(o, null);


            var result = await _eventBusProvider.RpcClientAsync("aaaaa", "");

            var result2 = await _eventBusProvider.RpcClientAsync("bbbbb", "");
        }


    }





    


    public interface IRabbitmqRpcService
    {

    }

    public class BaseClass: IRabbitmqRpcService
    {
        private readonly ISerializerProvider _provider;

        public BaseClass(ISerializerProvider provider)
        {
            _provider = provider;
        }
    }

    public class TestRpcService : BaseClass
    {
        private readonly ISerializerProvider _provider;

        public TestRpcService(ISerializerProvider provider):base(provider)
        {
            _provider = provider;
        }

        [RpcServer("aaaaa")]
        public string Test1()
        {
            return Guid.NewGuid().ToString();
        }


        [RpcServer("bbbbb")]
        public string Test2()
        {
            return "bbbbb";
        }
    }
}
