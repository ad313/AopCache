using AopCache.Core.Abstractions;
using AopCache.EventBus.RabbitMQ;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AopCache.EventBus.RabbitMQ.Attributes;

namespace AopCache.EventTestWeb
{
    public class RpcServiceTestHost : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly RabbitMqConfig _config;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitmqRpcService _rabbitmqRpcService;

        public RpcServiceTestHost(IEventBusProvider eventBusProvider, RabbitMqConfig config, ISerializerProvider serializerProvider, IServiceProvider serviceProvider, IRabbitmqRpcService rabbitmqRpcService)
        {
            _eventBusProvider = eventBusProvider;
            _config = config;
            _serializerProvider = serializerProvider;
            _serviceProvider = serviceProvider;
            _rabbitmqRpcService = rabbitmqRpcService;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var result = await _eventBusProvider.RpcClientAsync<int>("aaaaa", new object[] { "avalue1", 1, new class1 { Id = 1, Money = 11, Name = "sfsf" } });

            //Console.WriteLine($"result1 {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}");

            ////await Task.Delay(2000);

            //var result2 = await _eventBusProvider.RpcClientAsync<int>("test_aaaaa2", new object[] { "avalue2", 2, new class1 { Id = 2, Money = 11, Name = "sfsf" } });
            //Console.WriteLine($"result2 {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}");


            await _eventBusProvider.PublishAsync("tst-aaa", new EventMessageModel<class1>(new class1(){Money = 1111m}));

        }


    }





    


    public interface IRabbitmqRpcService
    {
        //int Test1(string a, int b, class2 c);
    }

    public class BaseClass: IRabbitmqRpcService
    {
        private readonly ISerializerProvider _provider;

        public BaseClass(ISerializerProvider provider)
        {
            _provider = provider;
        }

        //public int Test1(string a, int b, class2 c)
        //{
        //    return 2;
        //}
    }

    public class TestRpcService : BaseClass
    {
        private readonly ISerializerProvider _provider;

        public TestRpcService(ISerializerProvider provider) : base(provider)
        {
            _provider = provider;
        }

        [RpcServer("aaaaa")]
        public async Task<int> Test1(string a, int b, class2 c)
        {
            await Task.Delay(5000);
            return 1;
        }


        [RpcServer("aaaaa2", "test")]
        public new int Test1(string a, int b, class2 c, int d)
        {
            return 2;
        }


        [RpcServer("bbbbb")]
        public class1 Test2()
        {
            return new class1 { Id = 1, Money = 11, Name = "sfsf" };
        }

        [Subscriber("tst-aaa")]
        public async Task<class1> Test3(class1 aaa)
        {
            await Task.Delay(5000);
            Console.WriteLine($"valu:{aaa}");
            return new class1 { Id = 1, Money = 11, Name = "sfsf" };
        }
    }

    public partial class SerializerHandler
    {
       

        public static byte[] ToBytes(object value, Type type)
        {
            return MessagePackSerializer.Serialize(value, ContractlessStandardResolver.Options);
        }

        public static object BytesToObject(byte[] value, Type type)
        {
            if (value == null)
                return null;

            return MessagePackSerializer.Deserialize(type, value, ContractlessStandardResolver.Options);
        }

        

        public static T BytesClone<T>(T value)
        {
            var bytes = ToBytes(value, null);
            return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        }
    }

    public class class1
    {
        public int Id { get; set; }


        public string Name { get; set; }


        public decimal Money { get; set; }
    }

    public class class2
    {
        public int Id { get; set; }
        

        public decimal Money { get; set; }


        public int Age { get; set; }
    }
}
