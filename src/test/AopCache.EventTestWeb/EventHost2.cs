using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.EventTestWeb
{
    public class EventHost2 : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;
        private ConcurrentQueue<int> queue = new ConcurrentQueue<int>();

        public EventHost2(IEventBusProvider eventBusProvider, RabbitMqConfig config,ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;
        }
        
        private async Task sample_publish_queue_all2()
        {
            var key = "aaaaaaa";
            
            _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------11");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  11");
                    //index1 = 0;
                    await Task.CompletedTask;
                });
            
            //await Task.Delay(2000);
            await _eventBusProvider.PublishQueueAsync(key, new List<TestClassModel>());

            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(100, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(200, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(300, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(400, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(500, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(600, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(700, 100).Select(d => new TestClassModel() { Index = d }).ToList());

        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await sample_publish_queue_all2();
        }
    }
}
