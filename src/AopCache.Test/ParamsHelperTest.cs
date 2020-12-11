using AopCache.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace AopCache.Test
{
    public class ParamsHelperTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
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

            var source = "aaa_{a}";
            var str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "a", 1 } });
            Assert.IsTrue(str.Equals("aaa_1"));

            source = "aaa_{a}";
            str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "a", null } });
            Assert.IsTrue(str.Equals("aaa_{a}"));

            source = "aaa_{model.Text}";
            str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "model", model } });
            Assert.IsTrue(str.Equals("aaa_ad313"));

            str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "model", null } });
            Assert.IsTrue(str.Equals("aaa_{model.Text}"));

            source = "aaa_{a}_{model.Text}";
            str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "a", 1 }, { "model", null } });
            Assert.IsTrue(str.Equals("aaa_1_{model.Text}"));

            source = "aaa_{a}_{model.Text}";
            str = ParamsHelper.FillValue(source, new Dictionary<string, object>() { { "a", 1 }, { "model", model } });
            Assert.IsTrue(str.Equals("aaa_1_ad313"));
        }
    }
}