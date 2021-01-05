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
    /// 基于内存的发布订阅实现
    /// </summary>
    public class MemoryEventBusProvider : IEventBusProvider
    {
        private readonly ISerializerProvider _serializerProvider;
        private static readonly ConcurrentDictionary<string, Channel<string>> ChannelProviderDictionary = new ConcurrentDictionary<string, Channel<string>>();
        private static readonly ConcurrentDictionary<string, Channel<byte[]>> QueueDictionary = new ConcurrentDictionary<string, Channel<byte[]>>();
        private static readonly ConcurrentDictionary<string, Channel<byte[]>> ErrorQueueDictionary = new ConcurrentDictionary<string, Channel<byte[]>>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="serializerProvider"></param>
        /// <param name="serviceProvider"></param>
        public MemoryEventBusProvider(ISerializerProvider serializerProvider, IServiceProvider serviceProvider)
        {
            _serializerProvider = serializerProvider;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// ServiceProvider
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="message">数据</param>
        /// <returns></returns>
        public async Task PublishAsync<T>(string channel, EventMessageModel<T> message)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            message.Channel = channel;
            var channelProvider = GetChannel(channel);
            await channelProvider.Writer.WriteAsync(_serializerProvider.Serialize(message));
        }

        /// <summary>
        /// 发布事件 数据放到队列，并发布通知到订阅者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="message">数据集合</param>
        /// <returns></returns>
        public async Task PublishQueueAsync<T>(string channel, List<T> message)
        {
            if (message == null || !message.Any())
                return;

            await PushToQueueAsync(channel, message);
            await PublishAsync(channel, new EventMessageModel<T>());
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void Subscribe<T>(string channel, Action<EventMessageModel<T>> handler)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Run(async () =>
            {
                var channelProvider = GetChannel(channel);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        Console.WriteLine($"{DateTime.Now} {channel} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                        handler.Invoke(data);
                    }
                }
            });
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void Subscribe<T>(string channel, Func<EventMessageModel<T>, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Run(async () =>
            {
                var channelProvider = GetChannel(channel);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        Console.WriteLine($"{DateTime.Now} {channel} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                        await handler.Invoke(data);
                    }
                }
            });
        }

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string channel, Action<Func<int, List<T>>> handler)
        {
            Subscribe<T>(channel, msg =>
            {
                List<T> GetListFunc(int length) => GetQueueItems<T>(channel, length);

                handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            Subscribe<T>(channel, async msg =>
            {
                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(channel, length);

                await handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 分批次消费
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">每次处理间隔 毫秒</param>
        /// <param name="exceptionHandler">异常处理方式</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="error">发生异常时回调</param>
        /// <param name="completed">本次消费完成回调 最后执行</param>
        public void SubscribeQueue<T>(string channel, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            //if (delay <= 0)
            //    throw new Exception("delay must be greater than zero");

            Subscribe<T>(channel, async msg =>
            {
                var pages = GetTotalPagesFromQueue(channel, length);

                while (pages > 0)
                {
                    var data = await GetQueueItemsAsync<T>(channel, length);
                    if (!data.Any())
                        break;

                    var hasError = false;

                    try
                    {
                        await handler.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        hasError = true;

                        if (await HandleException(channel, exceptionHandler, data, e))
                        {
                            pages = 1;
                            return;
                        }
                    }
                    finally
                    {
                        if (hasError && error != null)
                            await HandleError(channel, data, error);

                        if (completed != null && pages == 1)
                            await HandleCompleted(channel, completed);
                    }

                    pages--;

                    if (delay > 0)
                        await Task.Delay(delay);
                }
            });
        }

        /// <summary>
        /// 获取某个频道队列数据量
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <returns></returns>
        public int GetQueueLength(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            var queue = GetQueue(channel);
            return queue.Reader.Count;
        }

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetQueueItems<T>(string channel, int length)
        {
            var queue = GetQueue(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                if (queue.Reader.TryRead(out byte[] item))
                    list.Add(_serializerProvider.Deserialize<T>(item));
                else
                    break;
            }

            return list;
        }

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public async Task<List<T>> GetQueueItemsAsync<T>(string channel, int length)
        {
            return await Task.FromResult(GetQueueItems<T>(channel, length));
        }

        /// <summary>
        /// 获取某个频道错误队列数据量
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <returns></returns>
        public int GetErrorQueueLength(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            var queue = GetErrorQueue(channel);
            return queue.Reader.Count;
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetErrorQueueItems<T>(string channel, int length)
        {
            var queue = GetErrorQueue(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                if (queue.Reader.TryRead(out byte[] item))
                    list.Add(_serializerProvider.Deserialize<T>(item));
                else
                    break;
            }

            return list;
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public async Task<List<T>> GetErrorQueueItemsAsync<T>(string channel, int length)
        {
            var queue = GetErrorQueue(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                var item = await queue.Reader.ReadAsync();
                if (item != null)
                    list.Add(_serializerProvider.Deserialize<T>(item));
                else
                    break;
            }

            return list;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel"></param>
        public void UnSubscribe(string channel)
        {
            throw new NotImplementedException();
        }
        
        #region private

        private async Task<bool> HandleException<T>(string channel, ExceptionHandlerEnum exceptionHandler, List<T> data, Exception e)
        {
            try
            {
                var text = $"{DateTime.Now} {channel} 队列消费端异常：{e.Message}";
                Console.WriteLine($"{text} {e}");

                switch (exceptionHandler)
                {
                    case ExceptionHandlerEnum.Continue:
                        return false;
                    case ExceptionHandlerEnum.Stop:
                        return true;
                    case ExceptionHandlerEnum.PushToSelfQueueAndStop:
                        await PushToQueueAsync(channel, data);
                        return true;
                    case ExceptionHandlerEnum.PushToSelfQueueAndContinue:
                        await PushToQueueAsync(channel, data);
                        return false;
                    case ExceptionHandlerEnum.PushToErrorQueueAndStop:
                        await PushToErrorQueueAsync(channel, data);
                        return true;
                    case ExceptionHandlerEnum.PushToErrorQueueAndContinue:
                        await PushToErrorQueueAsync(channel, data);
                        return false;
                    default:
                        Console.WriteLine($"{DateTime.Now} 不支持的 ExceptionHandlerEnum 类型：{exceptionHandler}");
                        return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{channel} 异常处理异常：{ex}");
                return true;
            }
        }

        private async Task HandleError<T>(string channel, List<T> data, Func<List<T>, Task> error)
        {
            try
            {
                await error.Invoke(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{channel} error func 执行异常：{ex}");
            }
        }

        private async Task HandleCompleted(string channel, Func<Task> completed)
        {
            try
            {
                await completed.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{channel} Completed func 执行异常：{ex}");
            }
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

        private Channel<byte[]> GetErrorQueue(string channel)
        {
            if (!ErrorQueueDictionary.TryGetValue(channel, out Channel<byte[]> queue))
            {
                queue = Channel.CreateUnbounded<byte[]>();
                ErrorQueueDictionary.TryAdd(channel, queue);
            }

            return queue;
        }

        private int GetTotalPagesFromQueue(string channel, int length)
        {
            var total = GetQueueLength(channel);

            return total <= 0 ? 0 : (total / length + (total % length > 0 ? 1 : 0));
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

        private async Task PushToErrorQueueAsync<T>(string channel, List<T> data)
        {
            if (data == null || !data.Any())
                return;

            var queue = GetErrorQueue(channel);

            foreach (var item in data)
            {
                await queue.Writer.WriteAsync(_serializerProvider.SerializeBytes(item));
            }
        }

        #endregion

        #region Test

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeTest<T>(string channel, Action<EventMessageModel<T>> handler)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channelProvider = GetChannel(channel);

            if (channelProvider.Reader.TryRead(out var msg))
            {
                Console.WriteLine($"{DateTime.Now} {channel} 收到数据：{msg}");

                var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                handler.Invoke(data);
            }
        }

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeTest<T>(string channel, Func<EventMessageModel<T>, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channelProvider = GetChannel(channel);

            if (channelProvider.Reader.TryRead(out var msg))
            {
                Console.WriteLine($"{DateTime.Now} {channel} 收到数据：{msg}");

                var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                await handler.Invoke(data);
            }
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueueTest<T>(string channel, Action<Func<int, List<T>>> handler)
        {
            SubscribeTest<T>(channel, msg =>
            {
                List<T> GetListFunc(int length) => GetQueueItems<T>(channel, length);

                handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeQueueTest<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            await SubscribeTest<T>(channel, async msg =>
            {
                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(channel, length);

                await handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 分批次消费 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">每次处理间隔 毫秒</param>
        /// <param name="exceptionHandler">异常处理方式</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="error">发生异常时回调</param>
        /// <param name="completed">本次消费完成回调 最后执行</param>
        public async Task SubscribeQueueTest<T>(string channel, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler,
            Func<List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            if (delay <= 0)
                throw new Exception("delay must be greater than zero");

            await SubscribeTest<T>(channel, async msg =>
            {
                var pages = GetTotalPagesFromQueue(channel, length);

                while (pages > 0)
                {
                    var data = await GetQueueItemsAsync<T>(channel, length);
                    if (!data.Any())
                        break;

                    var hasError = false;

                    try
                    {
                        await handler.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        hasError = true;

                        if (await HandleException(channel, exceptionHandler, data, e))
                        {
                            pages = 1;
                            return;
                        }
                    }
                    finally
                    {
                        if (hasError && error != null)
                            await HandleError(channel, data, error);

                        if (completed != null && pages == 1)
                            await HandleCompleted(channel, completed);
                    }

                    pages--;

                    await Task.Delay(delay);
                }
            });
        }

        #endregion
    }
}