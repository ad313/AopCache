using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AopCache.EventBus.RabbitMQ
{
    public class RpcServerInitHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;

        public RpcServerInitHost(IEventBusProvider eventBusProvider)
        {
            _eventBusProvider = eventBusProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (DependencyRegistrator.RpcServerMethodList.Any())
            {
                foreach (var method in DependencyRegistrator.RpcServerMethodList)
                {
                    var tag = method.GetCustomAttribute<RpcServerAttribute>();
                    _eventBusProvider.RpcServer<string>(tag.GetFormatKey(), async data =>
                    {
                        var instance = ActivatorUtilities.GetServiceOrCreateInstance(_eventBusProvider.ServiceProvider, method.DeclaringType);
                        
                        var result = method.Invoke(instance, null);

                        return new RpcResult(result as string);
                    });
                }
            }
        }
    }
}