using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.EventBus.RabbitMQ.Rpc
{
    public class RpcServerInitHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly ISerializerProvider _serializerProvider;

        public RpcServerInitHost(IEventBusProvider eventBusProvider,ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _serializerProvider = serializerProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (DependencyRegistrator.RpcServerMethodList.Any())
            {
                foreach (var method in DependencyRegistrator.RpcServerMethodList)
                {
                    var tag = method.GetCustomAttribute<RpcServerAttribute>();
                    _eventBusProvider.RpcServer<JsonElement[]>(tag.GetFormatKey(), async data =>
                    {
                        try
                        {
                            var instance = GetInstance(_eventBusProvider.ServiceProvider, method.DeclaringType);
                            if (instance == null)
                                throw new Exception($"faild to get {method.DeclaringType?.FullName} instance");

                            var pars = new List<dynamic>();
                            var methodPars = method.GetParameters();
                            for (var i = 0; i < methodPars.Length; i++)
                            {
                                pars.Add(_serializerProvider.Deserialize(data?.Length >= i + 1 ? data[i].GetRawText() : null, methodPars[i].ParameterType));
                            }

                            var result = method.Invoke(instance, pars.ToArray());

                            return new RpcResult(result is string
                                ? result.ToString()
                                : _serializerProvider.Serialize(result));
                        }
                        catch (Exception e)
                        {
                            return new RpcResult(null, e);
                        }
                        finally
                        {
                            await Task.CompletedTask;
                        }
                    });
                }
            }
        }

        private object GetInstance(IServiceProvider serviceProvider, Type type)
        {
            return serviceProvider.GetService(type) ?? ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);
        }
    }
}