using AopCache.Abstractions;
using AopCache.Test.Base;
using NUnit.Framework;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AopCache.Test
{
    public class CacheProviderTest : TestBase
    {
        private IAopCacheProvider cacheProvider = null;
        private ISerializerProvider serializerProvider = null;

        [OneTimeSetUp]
        public void Setup()
        {
            cacheProvider = GetService<IAopCacheProvider>();
            serializerProvider = GetService<ISerializerProvider>();
        }

        [Test]
        public async Task Test1()
        {
            var user = new User()
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                Birthday = DateTime.Now,
                Money = 100m,
                Name = "ad314"
            };

            var key = Guid.NewGuid().ToString();

            var result = await cacheProvider.Set(key, user, typeof(User), DateTime.MaxValue);
            Assert.IsTrue(result);

            var model = (User)(await cacheProvider.Get(key, typeof(User)));
            Assert.NotNull(model);
            Assert.IsTrue(model.UserId == user.UserId);

            cacheProvider.Remove(key);
        }

        [Test]
        public async Task Test2()
        {
            var user = 2;

            var key = Guid.NewGuid().ToString();

            var result = await cacheProvider.Set(key, user, typeof(int), DateTime.MaxValue);
            Assert.IsTrue(result);

            var model = (int)(await cacheProvider.Get(key, typeof(int)));
            Assert.NotNull(model);
            Assert.IsTrue(model == user);

            cacheProvider.Remove(key);
        }

        [Test]
        public async Task Test3()
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

            //bytes
            var data = new SerializerModel()
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


            var key = Guid.NewGuid().ToString();

            var result = await cacheProvider.Set(key, data, typeof(SerializerModel), DateTime.MaxValue);
            Assert.IsTrue(result);

            var model = (SerializerModel)(await cacheProvider.Get(key, typeof(SerializerModel)));
            Assert.NotNull(model);
            Assert.IsTrue(model.Text == data.Text);

            cacheProvider.Remove(key);
        }
    }
}