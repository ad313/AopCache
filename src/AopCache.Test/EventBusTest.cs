using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AopCache.Abstractions;
using AopCache.Test.Base;
using NUnit.Framework;

namespace AopCache.Test
{
    public class EventBusTest : TestBase
    {
        private IAopEventBusProvider eventBusProvider { get; set; }

        [SetUp]
        public void Setup()
        {
            eventBusProvider = GetService<IAopEventBusProvider>();
        }

        [Test]
        public async Task QueueTest()
        {
            var key = Guid.NewGuid().ToString();


            await eventBusProvider.PublishToQueueAsync(key, new List<string>() { "1", "2", "3" });
            await eventBusProvider.PublishToQueueAsync(key, new List<string>() { "11", "22", "33" });

            Task.Run(() =>
            {
                eventBusProvider.SubscribeFromQueue<string>(key, func =>
                {
                    var list = func.Invoke(10);


                });
            });

            await eventBusProvider.PublishToQueueAsync(key, new List<string>() { "5" });
            var a = 1;
            var b = 2;
            var c = 1;


        }


    }
}