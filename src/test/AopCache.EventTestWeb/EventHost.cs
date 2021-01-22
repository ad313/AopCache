using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.EventTestWeb
{
    public class EventHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;

        public EventHost(IEventBusProvider eventBusProvider, RabbitMqConfig config,ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;
        }
        
        private void Cus1(IConnection connection, IModel channel)
        {
            var exchangeName = "aaa";
            channel.ExchangeDeclare(exchangeName, "fanout");

            // 获取一个临时队列
            var queueName = channel.QueueDeclare().QueueName;
            // 把刚刚获取的队列绑定到logs这个交换中心上，fanout类型忽略routingKey，所以第三个参数为空
            channel.QueueBind(queueName, exchangeName, "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());
                Console.WriteLine(" [1] Received {0}", message);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        /// <summary>
        /// 广播
        /// </summary>
        /// <returns></returns>
        private async Task BroadcastTest()
        {
            var exchangeName = "BroadcastTest";

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [1] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, true);

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [2] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, true);

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [3] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, true);


            for (int i = 0; i < 100; i++)
            {
                await _eventBusProvider.PublishAsync(exchangeName, new EventMessageModel<int>(i), true);
            }
        }

        /// <summary>
        /// 普通多消费者
        /// </summary>
        /// <returns></returns>
        private async Task sample_multi_cus_test()
        {
            var exchangeName = "sample_multi_cus_test";

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [1] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, false);

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [2] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, false);

            _eventBusProvider.Subscribe<int>(exchangeName, async data =>
            {
                Console.WriteLine(" [3] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, false);


            for (int i = 0; i < 100; i++)
            {
                await _eventBusProvider.PublishAsync(exchangeName, new EventMessageModel<int>(i), false);
            }
        }

        private async Task sample_publish_queue()
        {
            var key = "sample_publish_queue";

            _eventBusProvider.SubscribeQueue<int>(key, async func =>
            {
                var data = await func(1000);
                foreach (var v in data)
                {
                    queue.Enqueue(v);
                    Console.WriteLine($"--------------------------------------{v}-----------------1");
                }

                Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  11");

                await Task.CompletedTask;
            });

            _eventBusProvider.SubscribeQueue<int>(key, async func =>
            {
                var data = await func(1000);
                foreach (var v in data)
                {
                    queue.Enqueue(v);
                    Console.WriteLine($"--------------------------------------{v}-----------------2");
                }

                Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  12");

                await Task.CompletedTask;
            });

            Enumerable.Range(0, 1000).AsParallel().ForAll(i =>
            {
                //Console.WriteLine($"{i}");
                _eventBusProvider.PublishQueueAsync(key, new List<int>() { i }).GetAwaiter().GetResult();
            });

            //await _eventBusProvider.PublishQueueAsync(key, new List<int>());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 200).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(200, 200).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(400, 200).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(600, 200).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(800, 200).ToList());
        }

        private async Task sample_publish_queue_all()
        {
            var key = "sample_publish_queue_all";

            var index1 = 0;
            var index2 = 0;
            _eventBusProvider.SubscribeQueue<int>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        Console.WriteLine($"--------------------------------------{v}-----------------1");
                    }

                    index1 += data.Count;
                    await Task.CompletedTask;
                },completed: async () =>
                {
                    Console.WriteLine($"{index1}  1");
                    index1 = 0;
                    await Task.CompletedTask;
                });

            _eventBusProvider.SubscribeQueue<int>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        Console.WriteLine($"--------------------------------------{v}-----------------2");
                    }
                    index2 += data.Count;
                    await Task.CompletedTask;
                }, completed: async () =>
                {

                    Console.WriteLine($"{index2}  2");
                    index2 = 0;
                    await Task.CompletedTask;
                });

            //    //Enumerable.Range(0, 100).AsParallel().ForAll(i =>
            //    //{
            //    //    Console.WriteLine($"{i}");
            //    //    _eventBusProvider.PublishAsync("testevent", new EventMessageModel<string>($"{i}")).GetAwaiter().GetResult();
            //    //});

            //await _eventBusProvider.PublishQueueAsync(key, new List<int>());


            await Task.Delay(2000);

            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 100).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(100, 100).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(200, 100).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(300, 100).ToList());
        }

        private async Task sample_publish_queue_all2()
        {
            var key = "sample_publish_queue_all3";

            //var queue = new ConcurrentQueue<int>();

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

            _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------12");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  12");
                    //index1 = 0;
                    await Task.CompletedTask;
                });

            _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------13");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  13");
                    //index1 = 0;
                    await Task.CompletedTask;
                });


            //await Task.Delay(2000);

            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(100, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(200, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(300, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(400, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(500, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(600, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(700, 100).Select(d => new TestClassModel() { Index = d }).ToList());

        }

        private ConcurrentQueue<int> queue = new ConcurrentQueue<int>();

        private async Task sample_publish_queue_all3()
        {
            var key = "sample_publish_queue_all4";

            //var queue = new ConcurrentQueue<int>();

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

            //_eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
            //    async data =>
            //    {
            //        foreach (var v in data)
            //        {
            //            queue.Enqueue(v.Index);
            //            Console.WriteLine($"--------------------------------------{v.Index}-----------------12");
            //        }

            //        await Task.CompletedTask;
            //    }, completed: async () =>
            //    {
            //        Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  12");
            //        //index1 = 0;
            //        await Task.CompletedTask;
            //    });

            //_eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
            //    async data =>
            //    {
            //        foreach (var v in data)
            //        {
            //            queue.Enqueue(v.Index);
            //            Console.WriteLine($"--------------------------------------{v.Index}-----------------13");
            //        }

            //        await Task.CompletedTask;
            //    }, completed: async () =>
            //    {
            //        Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  13");
            //        //index1 = 0;
            //        await Task.CompletedTask;
            //    });


            //await Task.Delay(2000);

            await _eventBusProvider.PublishQueueAsync(key, new List<TestClassModel>());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1000, 1).Select(d => new TestClassModel() { Index = d }).ToList());

            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1000, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1100, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1200, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1300, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1400, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1500, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1600, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            //await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(1700, 100).Select(d => new TestClassModel() { Index = d }).ToList());

        }

        private async Task sample_publish_queue_all3_big()
        {
            var key = "sample_publish_queue_all3";
            var key2 = "sample_publish_queue_all4";

            var index1 = 0;
            var index2 = 0;
            var index3 = 0;


            var queue = new ConcurrentQueue<int>();

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

            _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------12");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  12");
                    //index1 = 0;
                    await Task.CompletedTask;
                });

            _eventBusProvider.SubscribeQueue<TestClassModel>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------13");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  13");
                    //index1 = 0;
                    await Task.CompletedTask;
                });






            _eventBusProvider.SubscribeQueue<TestClassModel>(key2, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------21");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  21");
                    //index1 = 0;
                    await Task.CompletedTask;
                });

            _eventBusProvider.SubscribeQueue<TestClassModel>(key2, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------22");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  22");
                    //index1 = 0;
                    await Task.CompletedTask;
                });

            _eventBusProvider.SubscribeQueue<TestClassModel>(key2, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        queue.Enqueue(v.Index);
                        Console.WriteLine($"--------------------------------------{v.Index}-----------------23");
                    }

                    await Task.CompletedTask;
                }, completed: async () =>
                {
                    Console.WriteLine($"{queue.Count} {queue.ToArray().GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value).FirstOrDefault().Value.ToString()}  23");
                    //index1 = 0;
                    await Task.CompletedTask;
                });


            await Task.Delay(2000);



            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(0, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(100, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(200, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(300, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(400, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(500, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(600, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key, Enumerable.Range(700, 100).Select(d => new TestClassModel() { Index = d }).ToList());


            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1000, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1100, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1200, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1300, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1400, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1500, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1600, 100).Select(d => new TestClassModel() { Index = d }).ToList());
            await _eventBusProvider.PublishQueueAsync(key2, Enumerable.Range(1700, 100).Select(d => new TestClassModel() { Index = d }).ToList());

            await Task.Delay(20000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var factory = new ConnectionFactory()
            //{
            //    // 这是我这边的配置,自己改成自己用就好
            //    HostName = _config.HostName,
            //    UserName = _config.UserName,
            //    Password = _config.Password,
            //    Port = _config.Port,
            //    VirtualHost = "/"
            //};

            //var connection = factory.CreateConnection();
            //var channel = connection.CreateModel();

            //Cus1(connection, channel);
            //Cus2(connection, channel);

            //广播
            //await BroadcastTest();

            //普通 多消费者
            //await sample_multi_cus_test();

            //普通 队列订阅
            //await sample_publish_queue();

            //普通 队列订阅 消费完
            //await sample_publish_queue_all();
            //await sample_publish_queue_all2();
            await sample_publish_queue_all3();

            //Task.WaitAll(sample_publish_queue_all2(), sample_publish_queue_all3());

            //await Task.Delay(10000);
        }


    }

    public class TestClassModel
    {
        public int Index { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }

   
}
