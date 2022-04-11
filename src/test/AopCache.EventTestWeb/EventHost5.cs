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

namespace AopCache.EventTestWeb
{
    public class EventHost5 : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly ISerializerProvider _serializerProvider;

        public EventHost5(IEventBusProvider eventBusProvider, ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _serializerProvider = serializerProvider;
        }
        


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _eventBusProvider.Subscribe<int>("aaa", data =>
            {

            });

            await Task.Delay(3000, stoppingToken);

            await _eventBusProvider.PublishAsync("aaa", new EventMessageModel<int>() { Data = 1 });

            //await _eventBusProvider.PublishAsync("aaa", new EventMessageModel<int>() { Data = 1 });
        }


    }


    



}
