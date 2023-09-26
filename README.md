# AopCache

# 一、使用 AopCache
## 1、有两个版本，分别引入包
### 1.1、基于 AspectCore
    //最小化使用 使用 MemoryCache
    <PackageReference Include="AopCache" Version="2.0.1" />
    
    //使用 redis
    <PackageReference Include="AopCache.Redis" Version="2.0.1" />
    
### 1.2、基于 SourceGenerator 的 Mic.Aop：https://github.com/ad313/mic/tree/develop/src/Mic.Aop
    //最小化使用 使用 MemoryCache
    <PackageReference Include="AopCache.SourceGenerator" Version="2.0.1" />
    
    //使用 redis
    <PackageReference Include="AopCache.SourceGenerator.Redis" Version="2.0.1" />

### 2、注入
```
    //AopCache
    services.AddAopCache(option =>
    {
        //1、使用内存缓存
        option.UseMemoryCacheProvider();
   
        //2、使用redis
        option.UseRedisCacheProvider(Configuration.GetValue<string>("RedisConnectionString"));   
    });
```

### 3、接口定义

```
public interface ITestService
    {
        //默认时间单位是秒，长度为0，即永不过期
        [AopCache(AopTag = true, Key = "aaa")]
        string GetByKey();

        //设置3秒过期 这里的“{userId}”，占位符。用参数 userId 的值去替换
        [AopCache(AopTag = true, Key = "bbb_{userId}", Length = 3)]
        string GetByKeyAndParamter(int userId);

        //设置十分钟过期 这里的“{req.Id}”，占位符。用参数 req里面的Id 的值去替换
        [AopCache(AopTag = true, Key = "ccc_{req.Id}_{type}", Type = CacheTimeType.Minute, Length = 10)]
        Task<UserInfo> GetUserInfo(int type, Req req);
    }

```

### 4、注意事项
如果使用 SourceGenerator，那么 具体的 class 方法 或 接口的方法实现，必须是 virtual 或 override 可重写的，并且标签加上 AopTag 属性。SourceGenerator 会生成对应的代理类继承原来的类，
因此需要自己处理注入，比如原来是 class1，注入的时候用代理类去替换，class1_g。最好是扫描程序集注入的时候判断是否是继承


## 三、AopCache
【旧】AopCache 使用教程 https://www.cnblogs.com/ad313/p/10642554.html
