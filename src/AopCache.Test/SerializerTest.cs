using AopCache.Abstractions;
using AopCache.Test.Base;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AopCache.Test
{
    public class SerializerTest : TestBase
    {
        private ISerializerProvider serializerProvider = null;

        [SetUp]
        public void Setup()
        {
            serializerProvider = GetService<ISerializerProvider>();
        }

        [Test]
        public void SerializerHandlerTest1()
        {
            var json = JsonSerializer.Serialize(new User()
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                Birthday = DateTime.Now,
                Money = 100m,
                Name = "ad314"
            });

            var model = new SerializerModel()
            {
                Text = "ad313",
                UserInfo = new User()
                {
                    Id = 1,
                    UserId = Guid.NewGuid(),
                    Birthday = DateTime.Now,
                    Money = 100m,
                    Name = "ad314"
                },
                //Json = JsonDocument.Parse(json).RootElement
            };

            var bytes = SerializerHandler.ToBytes(model, model.GetType());
            Assert.IsTrue(bytes != null && bytes.Length > 0);

            var newModel = (SerializerModel)SerializerHandler.BytesToObject(bytes, model.GetType());
            Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");
        }

        [Test]
        public void SerializerHandlerTest2()
        {
            var model = new SerializerModel()
            {
                Text = "ad313",
                UserInfo = new User()
                {
                    Id = 1,
                    UserId = Guid.NewGuid(),
                    Birthday = DateTime.Now,
                    Money = 100m,
                    Name = "ad314"
                }
            };

            var list = new List<SerializerModel>();
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);
            list.Add(model);

            var bytes = SerializerHandler.ToBytes(list, list.GetType());

            var bytes2 = serializerProvider.SerializeBytes(list, list.GetType());

            
        }


        [Test]
        public void SerializerProviderTest()
        {
            var user = new User()
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                Birthday = DateTime.Now,
                Money = 100m,
                Name = "ad314"
            };

            //json
            var json = serializerProvider.Serialize(user);
            var newUser = serializerProvider.Deserialize<User>(json);
            Assert.IsTrue(newUser.Id == user.Id && newUser.UserId == user.UserId);

            //bytes
            var model = new SerializerModel()
            {
                Text = "ad313",
                UserInfo = new User()
                {
                    Id = 1,
                    UserId = Guid.NewGuid(),
                    Birthday = DateTime.Now,
                    Money = 100m,
                    Name = "ad314"
                },
                //Json = JsonDocument.Parse(json).RootElement
            };

            var bytes = serializerProvider.SerializeBytes(model);
            Assert.IsTrue(bytes != null && bytes.Length > 0);

            var newModel = serializerProvider.Deserialize<SerializerModel>(bytes);
            Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");

            //clone
            newModel = serializerProvider.Clone(model);
            Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");

        }

        //[Test]
        //public void SerializerProviderTest2()
        //{
        //    var user = new User2(Guid.NewGuid())
        //    {
        //        Birthday = DateTime.Now,
        //        Money = 100m,
        //        Name = "ad314"
        //    };

        //    var bytes = SerializerHandler.ToBytes(user, typeof(User2));

        //    var model = (User2) SerializerHandler.BytesToObject(bytes, typeof(User2));

        //}
    }


    public class SerializerModel
    {
        public string Text { get; set; }

        public User UserInfo { get; set; }

        //public JsonElement Json { get; set; }
    }

    public class User
    {
        public int Id { get; set; }


        public Guid UserId { get; set; }


        public DateTime Birthday { get; set; }


        public decimal Money { get; set; }


        public string Name { get; set; }
    }

    public class User2
    {

        public User2()
        {

        }

        public User2(Guid id)
        {
            UserId = id;
        }

        public Guid UserId { get;private set; }


        public DateTime Birthday { get; set; }


        public decimal Money { get; set; }


        public string Name { get; set; }
    }
}