using AopCache.Abstractions;
using AopCache.Test.Base;
using NUnit.Framework;
using System;
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

        //[Test]
        //public void Test1()
        //{
        //    var json = JsonSerializer.Serialize(new User()
        //    {
        //        Id = 1,
        //        UserId = Guid.NewGuid(),
        //        Birthday = DateTime.Now,
        //        Money = 100m,
        //        Name = "ad314"
        //    });

        //    var model = new SerializerModel()
        //    {
        //        Text = "ad313",
        //        UserInfo = new User()
        //        {
        //            Id = 1,
        //            UserId = Guid.NewGuid(),
        //            Birthday = DateTime.Now,
        //            Money = 100m,
        //            Name = "ad314"
        //        },
        //        Json = JsonDocument.Parse(json).RootElement
        //    };

        //    var bytes = SerializerHandler.ToBytes(model, model.GetType());
        //    Assert.IsTrue(bytes != null && bytes.Length > 0);

        //    var newModel = (SerializerModel)SerializerHandler.BytesToObject(bytes, model.GetType());
        //    Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");
        //}


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
                Json = JsonDocument.Parse(json).RootElement
            };

            var bytes = serializerProvider.SerializeBytes(model);
            Assert.IsTrue(bytes != null && bytes.Length > 0);

            var newModel = serializerProvider.Deserialize<SerializerModel>(bytes);
            Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");

            //clone
            newModel = serializerProvider.Clone(model);
            Assert.IsTrue(newModel?.UserInfo != null && newModel.UserInfo.Name == "ad314");

        }
    }


    public class SerializerModel
    {
        public string Text { get; set; }

        public User UserInfo { get; set; }

        public JsonElement Json { get; set; }
    }

    public class User
    {
        public int Id { get; set; }


        public Guid UserId { get; set; }


        public DateTime Birthday { get; set; }


        public decimal Money { get; set; }


        public string Name { get; set; }
    }
}