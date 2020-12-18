using AopCache.Abstractions;
using System;
using System.Threading.Tasks;

namespace AopCache.Redis
{
    public class RedisEventBusProvider : IAopEventBusProvider
    {
        private readonly ISerializerProvider _serializerProvider;

        public RedisEventBusProvider(ISerializerProvider serializerProvider)
        {
            _serializerProvider = serializerProvider;
        }

        public async Task PublishAsync<T>(string channel, AopMessageModel<T> message)
        {
            message.Channel = channel;
            await RedisHelper.PublishAsync(channel, _serializerProvider.Serialize(message));

            Console.WriteLine($"{DateTime.Now} 发布事件 {channel}");
        }

        public void Subscribe<T>(string channel, Action<AopMessageModel<T>> message)
        {
            RedisHelper.Subscribe((channel, msg =>
                    {
                        Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                        var data = _serializerProvider.Deserialize<AopMessageModel<T>>(msg.Body);
                        message?.Invoke(data);
                    }
                ));
        }
    }
}