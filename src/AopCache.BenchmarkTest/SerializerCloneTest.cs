using AopCache.Abstractions;
using AopCache.BenchmarkTest.Models;
using AopCache.Implements;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;
using AopCache.Redis;

namespace AopCache.BenchmarkTest
{
    public class SerializerCloneTest
    {
        private SampleLittleModel Model1 { get; set; }

        //private SampleLittleModel Model2 { get; set; }

        //private SampleLittleModel Model3 { get; set; }

        //private SampleLittleModel Model4 { get; set; }

        public ISerializerProvider SerializerProvider { get; set; }


        public SerializerCloneTest()
        {
            Model1 = new SampleLittleModel()
            {
                Id = new Random().Next(1, 1000),
                Age = new Random().Next(1, 1000),
                State = 1,
                Name = "test name hhha",
                AddTime = DateTime.Now
            };

            //Model2 = new SampleLittleModel()
            //{
            //    Id = new Random().Next(1, 1000),
            //    Age = new Random().Next(1, 1000),
            //    State = 1,
            //    Name = "test name hhha",
            //    AddTime = DateTime.Now
            //};

            //Model3 = new SampleLittleModel()
            //{
            //    Id = new Random().Next(1, 1000),
            //    Age = new Random().Next(1, 1000),
            //    State = 1,
            //    Name = "test name hhha",
            //    AddTime = DateTime.Now
            //};

            //Model4 = new SampleLittleModel()
            //{
            //    Id = new Random().Next(1, 1000),
            //    Age = new Random().Next(1, 1000),
            //    State = 1,
            //    Name = "test name hhha",
            //    AddTime = DateTime.Now
            //};

            SerializerProvider = new SerializerProvider();
        }

        [Benchmark]
        public void TestJson()
        {
            var result = SerializerProvider.Clone(Model1);
        }

        [Benchmark]
        public void TestNewtonsoftString()
        {
            var result = JsonConvert.DeserializeObject<SampleLittleModel>(JsonConvert.SerializeObject(Model1));
        }
        
        [Benchmark]
        public void TestMessagePack()
        {
            var result = SerializerHandler.BytesClone(Model1);
        }
    }
}
