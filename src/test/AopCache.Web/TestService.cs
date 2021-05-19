using System;
using System.Threading.Tasks;

namespace AopCache.Web
{
    //接口
    public interface ITestService
    {
        [AopCache]
        [AopSubscriber(Channel = "aaa")]
        string Get();

        //默认时间单位是秒，长度为0，即永不过期
        [AopCache(Key = "aaa")]
        [AopSubscriber(Channel = "aaa")]
        string GetByKey();

        //设置3秒过期 这里的“{userId}”，占位符。用参数 userId 的值去替换
        [AopCache(Key = "bbb_{userId}", Length = 3)]
        [AopSubscriber(Channel = "aaa", Map = "userId={type}")]
        string GetByKeyAndParamter(int userId);

        //设置十分钟过期 这里的“{req:Id}”，占位符。用参数 req里面的Id 的值去替换
        [AopCache(Key = "ccc_{req:Id}_{type}", Type = CacheTimeType.Minute, Length = 10)]
        [AopSubscriber(Channel = "aaa", Map = "type={type},req:Id={req:Id}")]
        Task<UserInfo> GetUserInfo(int type, Req req);

        [AopPublisher(Channel = "aaa", MessageSource = MessageSource.InParams)]
        Task<UserInfo> SetUserInfo(int type, Req req);
    }

    //实现接口
    public class TestService : ITestService
    {
        public string Get()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GetByKey()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GetByKeyAndParamter(int userId)
        {
            return Guid.NewGuid().ToString("N") + "---" + userId;
        }

        public async Task<UserInfo> GetUserInfo(int type, Req req)
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

        public async Task<UserInfo> SetUserInfo(int type, Req req)
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
        [AopCache(Key = "TestSingleClassKey")]
        [AopSubscriber(Channel = "aaa2")]
        public virtual string Get()
        {
            return Guid.NewGuid().ToString("N");
        }
        
        [AopPublisher(Channel = "aaa2")]
        public virtual string ClearTestSingleClassCache()
        {
            return Guid.NewGuid().ToString("N");
        }



        [AopCache(Key = "GetByUserId_{userId}")]
        [AopSubscriber(Channel = "aaa2_list")]
        public virtual int GetByUserId(int userId)
        {
            return userId;
        }
        
        [AopPublisher(Channel = "aaa2_list")]
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
