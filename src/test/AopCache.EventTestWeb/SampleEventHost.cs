using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AopCache.EventTestWeb
{
    public class SampleEventHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;
        //private ConcurrentQueue<int> queue = new ConcurrentQueue<int>();

        private IModel SubChannel = null;
        private IModel PubChannel = null;

        private ConnectionFactory factory;

        public SampleEventHost(IEventBusProvider eventBusProvider, RabbitMqConfig config,ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;

            factory = new ConnectionFactory()
            {
                // 这是我这边的配置,自己改成自己用就好
                HostName = _config.HostName,
                UserName = "admin",
                Password = "123456",
                Port = _config.Port,
                VirtualHost = "/"
            };
        }

        //private async Task sample_publish_queue_all2()
        //{
        //    var key = "aaaaaaa";

        //    _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
        //        async data =>
        //        {
        //            foreach (var v in data)
        //            {
        //                //queue.Enqueue(v.Index);
        //                Console.WriteLine($"--------------------------------------{v.Index}-----------------11");
        //            }

        //            await Task.CompletedTask;
        //        }, completed: async () =>
        //        {
        //            //Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  11");
        //            //index1 = 0;
        //            await Task.CompletedTask;
        //        });

        //    await Task.Delay(2000);

        //    await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 10).Select(d => new TestClassModel() { Index = d }).ToList());
        //}

        private async Task sample_publish_queue_all2()
        {
            var key = "bbb";

            _eventBusProvider.Subscribe<TestClassModel>(key, async data =>
            {
                Console.WriteLine($"--------------------------------------{data.Data.Index}-----------------");

                await Task.CompletedTask;
            });

            await Task.Delay(2000);

            await _eventBusProvider.PublishAsync(key, new EventMessageModel<TestClassModel>(new TestClassModel() { Index = 123 }));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await sample_publish_queue_all2();

            //await AllSample();

            var key = "ccc";
            var exchange = _config.ExchangeName;

            //_eventBusProvider.Subscribe<string>(key, async data =>
            //{
            //    Console.WriteLine($"--------------------------------------{data.Data}-----------------");

            //    await Task.CompletedTask;
            //});
            //await Task.Delay(2000);
            //await Publish(key, exchange, message: "aaabbbccc");

            await Task.Run(async () => await Subscribe(key, exchange));

            await Task.Delay(2000);

            await _eventBusProvider.PublishAsync(key, new EventMessageModel<TestClassModel>(new TestClassModel() { Index = 123 }));

        }

        private async Task AllSample()
        {
            var key = "ccc";
            var exchange = _config.ExchangeName;

            await Task.Run(async () => await Subscribe(key, exchange));

            await Task.Delay(2000);

            await Publish(key, exchange, message: "aaabbbccc");
        }

        public async Task Publish(string key, string exchangeName, string type = "topic", string message = "")
        {
            var connection = factory.CreateConnection();
            PubChannel = connection.CreateModel();
            PubChannel.ConfirmSelect();

            //持久化
            var properties = PubChannel.CreateBasicProperties();
            properties.Persistent = true;

            PubChannel.ExchangeDeclare(
                exchange: exchangeName,
                durable: true,
                type: type,
                autoDelete: false);

            var body = _serializerProvider.SerializeBytes(new EventMessageModel<string>(message));

            PubChannel.BasicPublish(
                exchange: exchangeName,
                routingKey: key,
                basicProperties: properties,
                body: body);

            PubChannel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            

            await Task.CompletedTask;
        }

        public async Task Subscribe(string key, string exchangeName, string type = "topic")
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var connection = factory.CreateConnection();
            SubChannel = connection.CreateModel();

            SubChannel.ExchangeDeclare(
                exchange: exchangeName,
                durable: true,
                type: type,
                autoDelete: false);

            //队列
            var queueName = $"temp_{key}";
            SubChannel.QueueDeclare(queueName, true, false, false, null);

            SubChannel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: key);

            //限流
            SubChannel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

            var consumer = new EventingBasicConsumer(SubChannel);
            consumer.Received += async (ch, ea) =>
            {
                try
                {
                    var msg = Encoding.Default.GetString(ea.Body.ToArray());
                    Console.WriteLine($"{DateTime.Now}：频道【{key}】 收到消息： {msg}");
                }
                catch (Exception e)
                {
                    
                }
                finally
                {
                    SubChannel.BasicAck(ea.DeliveryTag, false);
                }
            };

            SubChannel.BasicConsume(queueName, false, consumer);

            await Task.CompletedTask;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            SubChannel?.Dispose();
            PubChannel?.Dispose();
            base.Dispose();
        }
    }

    
}
