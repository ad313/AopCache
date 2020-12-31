using AopCache.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AopCache.Implements
{
    /// <summary>
    /// 基于进程的发布订阅实现
    /// </summary>
    public class MemoryEventBusProvider : IAopEventBusProvider
    {
        private readonly ISerializerProvider _serializerProvider;
        private static readonly ConcurrentDictionary<string, Channel<string>> ChannelProviderDictionary = new ConcurrentDictionary<string, Channel<string>>();
        private static readonly ConcurrentDictionary<string, Channel<byte[]>> QueueDictionary = new ConcurrentDictionary<string, Channel<byte[]>>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="serializerProvider"></param>
        public MemoryEventBusProvider(ISerializerProvider serializerProvider)
        {
            _serializerProvider = serializerProvider;
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishAsync<T>(string channel, AopMessageModel<T> message)
        {
            message.Channel = channel;
            var channelProvider = GetChannel(channel);
            await channelProvider.Writer.WriteAsync(_serializerProvider.Serialize(message));
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
            if (data == null || !data.Any())
                return;

            await PushToQueueAsync(channel, data);
            await PublishAsync(channel, new AopMessageModel<T>());
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        public void Subscribe<T>(string channel, Action<AopMessageModel<T>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Run(async () =>
            {
                var channelProvider = GetChannel(channel);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        Console.WriteLine($"{DateTime.Now} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<AopMessageModel<T>>(msg);
                        handler.Invoke(data);
                    }
                }
            });
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        public void Subscribe<T>(string channel, Func<AopMessageModel<T>, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Run(async () =>
            {
                var channelProvider = GetChannel(channel);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        Console.WriteLine($"{DateTime.Now} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<AopMessageModel<T>>(msg);
                        await handler.Invoke(data);
                    }
                }
            });
        }

        /// <summary>
        /// 订阅事件 队列中有新数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public void SubscribeFromQueue<T>(string channel, Action<Func<int, List<T>>> message)
        {
            Subscribe<T>(channel, msg =>
            {
                var queue = GetQueue(channel);

                Func<int, List<T>> getListFunc = length =>
                {
                    var list = new List<T>();

                    while (list.Count < length)
                    {
                        if (queue.Reader.TryRead(out byte[] item))
                            list.Add(_serializerProvider.Deserialize<T>(item));
                        else
                            break;
                    }

                    return list;
                };

                message?.Invoke(getListFunc);
            });
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
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            if (delay <= 0)
                throw new Exception("delay must be greater than zero");

            Subscribe<T>(channel, async msg =>
            {
                var queue = GetQueue(channel);

                List<T> GetListFunc(int maxLength)
                {
                    var list = new List<T>();

                    while (list.Count < maxLength)
                    {
                        if (queue.Reader.TryRead(out byte[] item))
                            list.Add(_serializerProvider.Deserialize<T>(item));
                        else
                            break;
                    }

                    return list;
                }

                while (true)
                {
                    var data = GetListFunc(length);
                    if (!data.Any())
                        break;

                    try
                    {
                        message?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        if (rollbackToQueueWhenException)
                        {
                            await PushToQueueAsync(channel, data);
                        }

                        Console.WriteLine($"订阅队列消费端异常：{e.Message} {e}");
                    }

                    await Task.Delay(delay);
                }
            });
        }


        private Channel<string> GetChannel(string channel)
        {
            if (!ChannelProviderDictionary.TryGetValue(channel, out Channel<string> channelProvider))
            {
                channelProvider = Channel.CreateUnbounded<string>();
                ChannelProviderDictionary.TryAdd(channel, channelProvider);
            }

            return channelProvider;
        }

        private Channel<byte[]> GetQueue(string channel)
        {
            if (!QueueDictionary.TryGetValue(channel, out Channel<byte[]> queue))
            {
                queue = Channel.CreateUnbounded<byte[]>();
                QueueDictionary.TryAdd(channel, queue);
            }

            return queue;
        }

        private async Task PushToQueueAsync<T>(string channel, List<T> data)
        {
            if (data == null || !data.Any())
                return;

            var queue = GetQueue(channel);

            foreach (var item in data)
            {
                await queue.Writer.WriteAsync(_serializerProvider.SerializeBytes(item));
            }
        }
    }
}