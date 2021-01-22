# AopCache
AopCache based on AspectCore

## 一、单独使用 EventBus
### 1、引用包
    <PackageReference Include="AopCache.EventBus.RabbitMQ" Version="1.0.1" />
    <PackageReference Include="AopCache.EventBus.CSRedis" Version="1.0.3" />

### 2、注入
```
//redis
services.AddEventBusUseCsRedis("redis connection string");

//rabbitmq
services.AddEventBusUseRabbitMq(op =>
                {
                    op.ExchangeName = "xxx";
                    op.HostName = "xxx";
                    op.UserName = "xxx";
                    op.Password = "xxx";
                    op.Port = 5672;
                    op.VirtualHost = "/";;
                    op.PrefetchSize = 0;
                    op.PrefetchCount = 1;
                });
```
### 3、获取到 IEventBusProvider，是单例
```
//1、普通模式：生产者发布了一条消息，假如有多个消费者，那么只有一个消费者会收到消息
//普通模式只对rabbitmq有效，redis 只有广播模式
//普通模式 发布
await _eventBusProvider.PublishAsync("xxxxxkey", new EventMessageModel<string>("hello world"), broadcast: false);

//普通模式 订阅
 _eventBusProvider.Subscribe<string>("xxxxxkey", async data =>
            {
                Console.WriteLine(" [1] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, broadcast: false);



//2、广播模式：生产者发布了一条消息，假如有多个消费者，那么每个消费者都会收到相同的消息（需注意幂等性）
//广播模式 发布
await _eventBusProvider.PublishAsync("xxxxxkey", new EventMessageModel<string>("hello world"), broadcast: true);

//广播模式 订阅
 _eventBusProvider.Subscribe<string>("xxxxxkey", async data =>
            {
                Console.WriteLine(" [1] Received {0}", _serializerProvider.Serialize(data));
                await Task.CompletedTask;
            }, broadcast: true);
            
            
            
//3、队列模式（默认是广播）：生产者发布了一条消息，会把数据放入一个单独的数据队列。消费者收到消息会是一条空消息，此时消费者需要主动从队列拉取一定数据的数据，再处理
//队列模式 发布
await _eventBusProvider.PublishQueueAsync("xxxxxkey", new List<string>(){"hello world"});

//队列模式 订阅（此时多个消费者会收到消息，但是拉取的数据是幂等的，不会重复消费）
 _eventBusProvider.SubscribeQueue<string>("xxxxxkey", async func =>
            {
                //获取1000条数据
                var data = await func(1000);
                foreach (var v in data)
                {
                    Console.WriteLine($"------{v}-----------------1");
                }
               
                await Task.CompletedTask;
            });      
            
  //增强的队列模式 订阅（此时多个消费者会收到消息，但是拉取的数据是幂等的，不会重复消费） 
  //内部自动循环直到数据队列被消耗完毕；定义每次处理条数，每次处理完毕后暂定毫秒
  //ExceptionHandlerEnum 当发生异常时，可以选择继续、停止、把数据重新放入原队列或放入专门的错误队列等，详情看注释
  _eventBusProvider.SubscribeQueue<string>(key, 10, 1, ExceptionHandlerEnum.PushToSelfQueueAndContinue,
                async data =>
                {
                    foreach (var v in data)
                    {
                        Console.WriteLine($"value:{v}");
                    }

                    await Task.CompletedTask;
                },
                //当发生异常
                error: async (ex, list) =>
                {
                    Console.WriteLine($"error :{ex.Message}");
                    await Task.CompletedTask;
                },
                //当本次消费结束（即数据队列数据为空）
                completed: async () =>
                {
                    Console.WriteLine($"completed");
                    await Task.CompletedTask;
                });   
 
            
```






## 二、AopCache
AopCache 使用教程 https://www.cnblogs.com/ad313/p/10642554.html
