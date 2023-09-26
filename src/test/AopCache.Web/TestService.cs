using System;
using System.Threading.Tasks;
using AopCache.Core.Common;

namespace AopCache.Web
{
    //接口
    public interface ITestService
    {
        [AopCache(AopTag = true)]
        string Get();

        //默认时间单位是秒，长度为0，即永不过期
        [AopCache(AopTag = true, Key = "aaa")]
        string GetByKey();

        //设置3秒过期 这里的“{userId}”，占位符。用参数 userId 的值去替换
        [AopCache(AopTag = true, Key = "bbb_{userId}", Length = 3)]
        string GetByKeyAndParamter(int userId);

        //设置十分钟过期 这里的“{req:Id}”，占位符。用参数 req里面的Id 的值去替换
        [AopCache(AopTag = true, Key = "ccc_{req:Id}_{type}", Type = CacheTimeType.Minute, Length = 10)]
        Task<UserInfo> GetUserInfo(int type, Req req);

        Task<UserInfo> SetUserInfo(int type, Req req);
    }

    //实现接口
    public class TestService : ITestService
    {
        public virtual string Get()
        {
            return Guid.NewGuid().ToString("N");
        }

        public virtual string GetByKey()
        {
            return Guid.NewGuid().ToString("N");
        }

        public virtual string GetByKeyAndParamter(int userId)
        {
            return Guid.NewGuid().ToString("N") + "---" + userId;
        }

        public virtual async Task<UserInfo> GetUserInfo(int type, Req req)
        {
            return new UserInfo()
            {
                Id = new Random().Next(1, 100),
                Name = Guid.NewGuid().ToString("N"),
                UserInfo2 = new UserInfo2()
                {
                    Id = new Random().Next(1, 100),
                    Name = Guid.NewGuid().ToString("N")
                }
            };
        }

        public virtual async Task<UserInfo> SetUserInfo(int type, Req req)
        {
            return new UserInfo()
            {
                Id = new Random().Next(1, 100),
                Name = Guid.NewGuid().ToString("N"),
                UserInfo2 = new UserInfo2()
                {
                    Id = new Random().Next(1, 100),
                    Name = Guid.NewGuid().ToString("N")
                }
            };
        }
    }



    
    public class TestSingleClass
    {
        [AopCache(AopTag = true, Key = "TestSingleClassKey")]
        public virtual string Get()
        {
            return Guid.NewGuid().ToString("N");
        }
        
        public virtual string ClearTestSingleClassCache()
        {
            return Guid.NewGuid().ToString("N");
        }



        [AopCache(AopTag = true, Key = "GetByUserId_{userId}")]
        public virtual int GetByUserId(int userId)
        {
            return userId;
        }
        
        public virtual string ClearByIdList(string ids)
        {
            return Guid.NewGuid().ToString("N");
        }
    }






    public class Req
    {
        public int Id { get; set; }

        public int Status { get; set; }
    }


    public class UserInfo
    {
        public int Id { get; set; }


        public string Name { get; set; }

        public UserInfo2 UserInfo2 { get; set; }
    }

    public class UserInfo2
    {
        public int Id { get; set; }


        public string Name { get; set; }
    }

    public class Testaaa
    {

    }
}
