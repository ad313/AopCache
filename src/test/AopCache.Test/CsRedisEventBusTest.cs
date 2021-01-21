using AopCache.Test.Base;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;

namespace AopCache.Test
{
    public class CsRedisEventBusTest : TestBase
    {
        private IEventBusProvider eventBusProvider { get; set; }

        [OneTimeSetUp]
        public void Setup()
        {
            eventBusProvider = GetService<IEventBusProvider>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //var keys = RedisHelper.Keys("EventBusProvider*");
            //foreach (var key in keys)
            //{
            //    RedisHelper.Del(key);
            //}
        }

        [Test]
        public async Task PubSubTest()
        {
            var channel = Guid.NewGuid().ToString();
            var trackId = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            
            eventBusProvider.Subscribe<List<string>>(channel, data =>
            {
                Assert.IsTrue(data != null);
                Assert.IsTrue(data.TraceId == trackId);
                Assert.IsTrue(data.Data.Count == list1.Count);
                Assert.IsTrue(data.Data.All(item => list1.Contains(item)));

                eventBusProvider.UnSubscribe(channel);

                Console.WriteLine("ok");
            });

             await eventBusProvider.PublishAsync(channel, new EventMessageModel<List<string>>(list1, trackId));
        }

        [Test]
        public async Task PubSubAsyncTest()
        {
            var channel = Guid.NewGuid().ToString();
            var trackId = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            
            eventBusProvider.Subscribe<List<string>>(channel, async data =>
            {
                Assert.IsTrue(data != null);
                Assert.IsTrue(data.TraceId == trackId);
                Assert.IsTrue(data.Data.Count == list1.Count);
                Assert.IsTrue(data.Data.All(item => list1.Contains(item)));

                Console.WriteLine("ok");

                eventBusProvider.UnSubscribe(channel);

                await Task.CompletedTask;
            });

            await eventBusProvider.PublishAsync(channel, new EventMessageModel<List<string>>(list1, trackId));
        }

        [Test]
        public async Task PubQueueTest()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            var count = eventBusProvider.GetQueueLength(key);
            Assert.IsTrue(count == list1.Count);

            var list = eventBusProvider.GetQueueItems<string>(key, 100);
            Assert.IsTrue(list.Count == list1.Count);
            Assert.IsTrue(list.All(item => list1.Contains(item)));
        }

        [Test]
        public async Task PubSubQueueTest()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            eventBusProvider.SubscribeQueue<string>(key, func =>
            {
                var list = func(10);

                if (list.Any())
                {
                    Assert.IsTrue(list.Count == list1.Count);
                    Assert.IsTrue(list.All(item => list1.Contains(item)));
                }
                
                //eventBusProvider.UnSubscribe(key);

                Console.WriteLine("ok");
            });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        [Test]
        public async Task PubSubQueueAsyncTest()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            eventBusProvider.SubscribeQueue<string>(key, async func =>
            {
                var list = await func(10);

                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                Console.WriteLine("ok");

                await Task.CompletedTask;
            });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);
        }

        /// <summary>
        /// 一次消费
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTest1()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };

            eventBusProvider.SubscribeQueue<string>(key, 100, 1, ExceptionHandlerEnum.Continue, async list =>
                {
                    Assert.IsTrue(list.Count == list1.Count);
                    Assert.IsTrue(list.All(item => list1.Contains(item)));

                    Console.WriteLine("ok");

                    await Task.CompletedTask;
                },
                null,
                async () =>
                {
                    eventBusProvider.UnSubscribe(key);
                    await Task.CompletedTask;
                });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 多次消费
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTest2()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.Continue, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));

                Assert.IsTrue(list1[index] == item);

                Console.WriteLine("ok");

                index++;

                await Task.CompletedTask;
            },null, async () =>
            {
                Assert.IsTrue(index == 6);

                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 一次消费 有异常 Continue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException_Continue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            eventBusProvider.SubscribeQueue<string>(key, 100, 1, ExceptionHandlerEnum.Continue, async list =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                await Task.CompletedTask;
            }, async () =>
            {
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 一次消费 有异常 PushToSelfQueueAndContinue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException_PushToSelfQueueAndContinue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };

            eventBusProvider.SubscribeQueue<string>(key, 100, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue, async list =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == list1.Count);
                var queueList = eventBusProvider.GetQueueItems<string>(key, 100);
                Assert.IsTrue(queueList.All(item => list1.Contains(item)));

                await Task.CompletedTask;
            }, async () =>
            {
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 一次消费 有异常 PushToSelfQueueAndContinue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException_PushToErrorQueueAndContinue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            eventBusProvider.SubscribeQueue<string>(key, 100, 1, ExceptionHandlerEnum.PushToErrorQueueAndContinue, async list =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == list1.Count);
                Assert.IsTrue(list.All(item => list1.Contains(item)));

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == 0);
                var queueList = eventBusProvider.GetQueueItems<string>(key, 100);
                Assert.IsTrue(queueList.Count == 0);

                //验证错误队列数据
                Assert.IsTrue(eventBusProvider.GetErrorQueueLength(key) == list1.Count);
                var errorQueueList = eventBusProvider.GetErrorQueueItems<string>(key, 100);
                Assert.IsTrue(errorQueueList.All(item => list1.Contains(item)));

                await Task.CompletedTask;
            }, async () =>
            {
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });

            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }







        /// <summary>
        /// 多次消费 有异常 Continue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_Continue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.Continue, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == list1.Count);
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 多次消费 有异常 PushToSelfQueueAndContinue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_PushToSelfQueueAndContinue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };

            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == list1.Count);

                //验证错误队列数据
                Assert.IsTrue(eventBusProvider.GetErrorQueueLength(key) == 0);

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == list1.Count);
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(100000);
        }

        /// <summary>
        /// 多次消费 有异常 PushToErrorQueueAndContinue
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_PushToErrorQueueAndContinue()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.PushToErrorQueueAndContinue, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == list1.Count - index);

                //验证错误队列数据
                Assert.IsTrue(eventBusProvider.GetErrorQueueLength(key) == index);

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == list1.Count);

                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }




        /// <summary>
        /// 多次消费 有异常 Stop
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_Stop()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.Stop, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == 1);
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 多次消费 有异常 PushToSelfQueueAndStop
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_PushToSelfQueueAndStop()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.PushToSelfQueueAndStop, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == list1.Count);

                //验证错误队列数据
                Assert.IsTrue(eventBusProvider.GetErrorQueueLength(key) == 0);

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == 1);
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }

        /// <summary>
        /// 多次消费 有异常 PushToErrorQueueAndStop
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PubSubQueueHandlerTestWithException2_PushToErrorQueueAndStop()
        {
            var key = Guid.NewGuid().ToString();

            var list1 = new List<string>() { "1", "2", "3" };
            var list2 = new List<string>() { "11", "22", "33" };
            
            var index = 0;
            eventBusProvider.SubscribeQueue<string>(key, 1, 1, ExceptionHandlerEnum.PushToErrorQueueAndStop, async list =>
            {
                Assert.IsTrue(list.Count == 1);

                var item = list.First();
                Assert.IsTrue(list1.Contains(item));
                Assert.IsTrue(list1[index] == item);

                index++;

                await Task.CompletedTask;

                throw new Exception("error");
            }, async (ex, list) =>
            {
                Assert.IsTrue(list.Count == 1);
                Assert.IsTrue(list1[index - 1] == list.First());

                //验证队列数据
                Assert.IsTrue(eventBusProvider.GetQueueLength(key) == list1.Count - index);

                //验证错误队列数据
                Assert.IsTrue(eventBusProvider.GetErrorQueueLength(key) == index);

                await Task.CompletedTask;
            }, async () =>
            {
                Assert.IsTrue(index == 1);
                //eventBusProvider.UnSubscribe(key);
                await Task.CompletedTask;
            });
            
            await eventBusProvider.PublishQueueAsync(key, list1);
            await eventBusProvider.PublishQueueAsync(key, list2);

            list1.AddRange(list2);

            await Task.Delay(10000);
        }


    }
}