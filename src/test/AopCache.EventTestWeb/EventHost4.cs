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
    public class EventHost4 : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;

        public EventHost4(IEventBusProvider eventBusProvider, RabbitMqConfig config,ISerializerProvider serializerProvider)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;
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

            await Task.Delay(2000);

            for (int i = 0; i < 1; i++)
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

            //await Task.Delay(2000);

            //for (int i = 0; i < 100; i++)
            //{
            //    await _eventBusProvider.PublishAsync(exchangeName, new EventMessageModel<int>(i), false);
            //}
        }
        
        /// <summary>
        /// 延迟队列
        /// </summary>
        /// <returns></returns>
        private async Task DelayQueueTest()
        {
            var key = "aaa";
            _eventBusProvider.DelaySubscribe<int>(key, data =>
            {
                Console.WriteLine($"{DateTime.Now} [1] Received {0}", data.Data);
            });

            await Task.Delay(2000);

            //for (var i = 0; i < 10; i++)
            //{
            //    await _eventBusProvider.DelayPublishAsync(key, 10, new EventMessageModel<int>(i));
            //    Console.WriteLine($"{DateTime.Now}");

            //    await Task.Delay(10);
            //}

            //await _eventBusProvider.DelayPublishAsync(key, DateTime.Parse("2021-03-09 17:39:11"), new EventMessageModel<int>(111));

            await _eventBusProvider.DelayPublishAsync(key, 3, new EventMessageModel<int>(111));
        }


        private async Task CreateRpcService()
        {
            RPCServer.Start();

            await Task.Delay(2000);

            var client = new RpcClient();
            var result = await client.CallAsync("3");
            Console.WriteLine("111 " + result);


            result = await client.CallAsync("4");
            Console.WriteLine("222 " + result);

            result = await client.CallAsync("5");
            Console.WriteLine("333 " + result);
        }

        private async Task CreateRpcService2()
        {
            _eventBusProvider.RpcServer<int>("aaa", async data =>
            {
                await Task.CompletedTask;

                //throw new Exception($"error in {data}");

                //await Task.Delay(5000);
                return new RpcResult($"aaa from {data}");
            });
            
            await Task.Delay(2000);

            Console.WriteLine($"begin---");

            var watch = Stopwatch.StartNew();
            watch.Start();

            var w2 = Stopwatch.StartNew();
            for (int i = 0; i < 1; i++)
            {
                w2.Restart();
                var result = await _eventBusProvider.RpcClientAsync("aaa", i);
                Console.WriteLine($"{result.Data}--{i} {w2.ElapsedMilliseconds}");
            }
            w2.Stop();


            //var tasks1 = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
            //{
            //    var result = await _eventBusProvider.RpcClientAsync("aaa", i);
            //    Console.WriteLine($"{result.Data}--{i}");
            //})).ToList();


            //watch.Start();

            //Task.WaitAll(tasks1.ToArray());
            watch.Stop();

            Console.WriteLine($"end {watch.ElapsedMilliseconds}");
        }

        private async Task CreateRpcService3()
        {
            _eventBusProvider.RpcServer<int>("aaa", async data =>
            {
                await Task.CompletedTask;

                //throw new Exception($"error in {data}");

                //await Task.Delay(5000);
                return new RpcResult($"aaa from {data}");
            });

            _eventBusProvider.RpcServer<int>("aaa", async data =>
            {
                await Task.CompletedTask;

                //throw new Exception($"error in {data}");

                //await Task.Delay(5000);
                return new RpcResult($"aaa from {data}");
            });



            _eventBusProvider.RpcServer<int>("bbb", async data =>
            {
                await Task.CompletedTask;

                //throw new Exception($"error in {data}");

                //await Task.Delay(5000);
                return new RpcResult($"bbb from {data}");
            });


            _eventBusProvider.RpcServer<int>("ccc", async data =>
            {
                await Task.CompletedTask;

                //throw new Exception($"error in {data}");

                //await Task.Delay(5000);
                return new RpcResult($"ccc from {data}");
            });

            await Task.Delay(2000);

            Console.WriteLine($"begin---");

            var watch = Stopwatch.StartNew();
            //watch.Start();

            var w2 = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                w2.Restart();
                var result = await _eventBusProvider.RpcClientAsync("aaa", i);
                Console.WriteLine($"{result.Data}--{i} {w2.ElapsedMilliseconds}");
            }
            w2.Stop();

            for (int i = 100; i < 110; i++)
            {
                w2.Restart();
                var result = await _eventBusProvider.RpcClientAsync("bbb", i);
                Console.WriteLine($"{result.Data}--{i} {w2.ElapsedMilliseconds}");
            }

            for (int i = 1000; i < 1010; i++)
            {
                w2.Restart();
                var result = await _eventBusProvider.RpcClientAsync("ccc", i);
                Console.WriteLine($"{result.Data}--{i} {w2.ElapsedMilliseconds}");
            }


            //var tasks1 = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
            //{
            //    var result = await _eventBusProvider.RpcClientAsync("aaa", i);
            //    Console.WriteLine($"{result.Data}--{i}");
            //})).ToList();

            //var tasks2 = Enumerable.Range(100, 10).Select(i => Task.Run(async () =>
            //{
            //    var result = await _eventBusProvider.RpcClientAsync("bbb", i);
            //    Console.WriteLine($"{result.Data}--{i}");
            //})).ToList();

            //var tasks3 = Enumerable.Range(1000, 10).Select(i => Task.Run(async () =>
            //{
            //    var result = await _eventBusProvider.RpcClientAsync("ccc", i);
            //    Console.WriteLine($"{result.Data}--{i}");
            //})).ToList();

            //tasks1.AddRange(tasks2);
            //tasks1.AddRange(tasks3);

            //watch.Start();

            //Task.WaitAll(tasks1.ToArray());
            //watch.Stop();

            Console.WriteLine($"end {watch.ElapsedMilliseconds}");
        }


        private async Task TimerFactory()
        {
            //var token = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            ////Task.Run(async () =>
            ////{
            ////    await Task.Delay(3000);
            ////    token.Cancel();
            ////});

            //using (await TimerPoolFactory.GetTimer(5000, () => { Console.WriteLine($"{DateTime.Now}"); }, token.Token))
            //{

            //}

            //Console.WriteLine($"22-{DateTime.Now}");
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //广播
            //await BroadcastTest();

            //普通 多消费者
            //await sample_multi_cus_test();

            //延迟队列
            //await DelayQueueTest();

            //rpc
            //await CreateRpcService();
            await CreateRpcService2();
            //await CreateRpcService3();


        }


    }


    



}
