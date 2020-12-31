using AopCache.Abstractions;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 发布事件 发布数据到队列，并发布通知
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task PublishToQueueAsync<T>(string channel, List<T> data)
        {
            throw new NotImplementedException();
        }


        public void Subscribe<T>(string channel, Action<AopMessageModel<T>> handler)
        {
            RedisHelper.Subscribe((channel, msg =>
                    {
                        Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                        var data = _serializerProvider.Deserialize<AopMessageModel<T>>(msg.Body);
                        handler?.Invoke(data);
                    }
                ));
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        public void Subscribe<T>(string channel, Func<AopMessageModel<T>, Task> handler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件 队列中有新数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        public void SubscribeFromQueue<T>(string channel, Action<Func<int, List<T>>> handler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件 队列中有新数据 分批次消费
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">处理完一次的停顿时间 毫秒</param>
        /// <param name="rollbackToQueueWhenException">当处理失败时是否把数据重新加入到队列</param>
        /// <param name="message"></param>
        public void SubscribeFromQueue<T>(string channel, int length, int delay, bool rollbackToQueueWhenException, Action<List<T>> message)
        {
            throw new NotImplementedException();
        }
    }
}