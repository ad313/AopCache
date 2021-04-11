using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using AopCache.Core.Common;

namespace AopCache.Core.Implements
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
        /// 总开关默认开启
        /// </summary>
        public bool Enable { get; private set; } = true;

        /// <summary>
        /// 频道开关
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _channelEnableDictionary = new ConcurrentDictionary<string, bool>();

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
        /// <param name="key">Key</param>
        /// <param name="message">数据</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        /// <returns></returns>
        public async Task PublishAsync<T>(string key, EventMessageModel<T> message, bool broadcast = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            message.Key = key;
            var channelProvider = GetChannel(key);
            await channelProvider.Writer.WriteAsync(_serializerProvider.Serialize(message));
        }

        /// <summary>
        /// 发布事件 数据放到队列，并发布通知到订阅者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="message">数据集合</param>
        /// <returns></returns>
        public async Task PublishQueueAsync<T>(string key, List<T> message)
        {
            if (message == null || !message.Any())
                return;

            await PushToQueueAsync(key, message);
            await PublishAsync(key, new EventMessageModel<T>());
        }

        /// <summary>
        /// 发布事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="seconds">延迟秒数</param>
        /// <param name="message">数据</param>
        /// <returns></returns>
        public async Task DelayPublishAsync<T>(string key, long seconds, EventMessageModel<T> message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 发布事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="absoluteTime">指定执行时间</param>
        /// <param name="message">数据</param>
        /// <returns></returns>
        public async Task DelayPublishAsync<T>(string key, DateTime absoluteTime, EventMessageModel<T> message)
        {
            throw new NotImplementedException();
        }
        
        public Task<RpcResult<T>> RpcClientAsync<T>(string key, object[] message = null, int timeout = 30)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        public void Subscribe<T>(string key, Action<EventMessageModel<T>> handler, bool broadcast = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Factory.StartNew(async () =>
            {
                var channelProvider = GetChannel(key);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        //Console.WriteLine($"{DateTime.Now} {key} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                        handler.Invoke(data);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void DelaySubscribe<T>(string key, Action<EventMessageModel<T>> handler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void DelaySubscribe<T>(string key, Func<EventMessageModel<T>, Task> handler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件 RpcServer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void RpcServer<T>(string key, Func<T, Task<RpcResult>> handler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        public void Subscribe<T>(string key, Func<EventMessageModel<T>, Task> handler, bool broadcast = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Task.Factory.StartNew(async () =>
            {
                var channelProvider = GetChannel(key);

                while (await channelProvider.Reader.WaitToReadAsync())
                {
                    if (channelProvider.Reader.TryRead(out var msg))
                    {
                        //Console.WriteLine($"{DateTime.Now} {key} 收到数据：{msg}");

                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                        await handler.Invoke(data);
                    }

                    await Task.Delay(1);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string key, Action<Func<int, List<T>>> handler)
        {
            Subscribe<T>(key, msg =>
            {
                if (!IsEnable(key))
                {
                    Console.WriteLine($"{DateTime.Now} 频道【{key}】 已关闭消费");
                    return;
                }

                List<T> GetListFunc(int length) => GetQueueItems<T>(key, length);

                handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            Subscribe<T>(key, async msg =>
            {
                if (!IsEnable(key))
                {
                    Console.WriteLine($"{DateTime.Now} 频道【{key}】 已关闭消费");
                    return;
                }

                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(key, length);

                await handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 分批次消费
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">每次处理间隔 毫秒</param>
        /// <param name="exceptionHandler">异常处理方式</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="error">发生异常时回调</param>
        /// <param name="completed">本次消费完成回调 最后执行</param>
        public void SubscribeQueue<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<Exception, List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            Subscribe<T>(key, async msg =>
            {
                if (!IsEnable(key))
                {
                    Console.WriteLine($"{DateTime.Now} 频道【{key}】 已关闭消费");
                    return;
                }

                var pages = GetTotalPagesFromQueue(key, length);

                var needGc = pages * length > 1000;

                while (pages > 0)
                {
                    var data = await GetQueueItemsAsync<T>(key, length);
                    if (!data.Any())
                        break;

                    Exception ex = null;

                    try
                    {
                        await handler.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        ex = e;

                        if (await HandleException(key, exceptionHandler, data, e))
                        {
                            pages = 1;
                            return;
                        }
                    }
                    finally
                    {
                        if (ex != null && error != null)
                            await HandleError(key, data, error, ex);

                        if (completed != null && pages == 1)
                            await HandleCompleted(key, completed);
                    }

                    pages--;

                    if (pages == 0 && needGc)
                    {
                        GC.Collect();
                        Console.WriteLine("---------------gc-----------------");
                    }

                    if (delay > 0)
                        await Task.Delay(delay);
                }
            });
        }

        /// <summary>
        /// 获取某个频道队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public int GetQueueLength(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var queue = GetQueue(key);
            return queue.Reader.Count;
        }

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetQueueItems<T>(string key, int length)
        {
            var queue = GetQueue(key);
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
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public async Task<List<T>> GetQueueItemsAsync<T>(string key, int length)
        {
            return await Task.FromResult(GetQueueItems<T>(key, length));
        }

        /// <summary>
        /// 获取某个频道错误队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public int GetErrorQueueLength(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var queue = GetErrorQueue(key);
            return queue.Reader.Count;
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetErrorQueueItems<T>(string key, int length)
        {
            var queue = GetErrorQueue(key);
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
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public async Task<List<T>> GetErrorQueueItemsAsync<T>(string key, int length)
        {
            var queue = GetErrorQueue(key);
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
        /// <param name="key"></param>
        public void UnSubscribe(string key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置订阅是否消费
        /// </summary>
        /// <param name="enable">true 开启开关，false 关闭开关</param>
        /// <param name="key">为空时表示总开关</param>
        public void SetEnable(bool enable, string key = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Enable = enable;
                return;
            }

            _channelEnableDictionary.AddOrUpdate(key, d => enable, (k, value) => enable);
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

        private async Task HandleError<T>(string channel, List<T> data, Func<Exception, List<T>, Task> error, Exception e)
        {
            try
            {
                await error.Invoke(e, data);
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

        private async Task PushToQueueAsync<T>(string channel, List<T> data, int length = 10000)
        {
            if (data == null || !data.Any())
                return;

            var queue = GetQueue(channel);

            var type = typeof(T);

            if (data.Count > length)
            {
                foreach (var list in Helpers.SplitList(data, length))
                {
                    foreach (var item in list)
                    {
                        await queue.Writer.WriteAsync(_serializerProvider.SerializeBytes(item, type));
                    }

                    await Task.Delay(10);
                }
            }
            else
            {
                foreach (var item in data)
                {
                    await queue.Writer.WriteAsync(_serializerProvider.SerializeBytes(item, type));
                }
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

        private bool IsEnable(string channel)
        {
            return Enable && (!_channelEnableDictionary.TryGetValue(channel, out bool enable) || enable);
        }

        #endregion

        #region Test

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeTest<T>(string key, Action<EventMessageModel<T>> handler)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channelProvider = GetChannel(key);

            if (channelProvider.Reader.TryRead(out var msg))
            {
                Console.WriteLine($"{DateTime.Now} {key} 收到数据：{msg}");

                var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                handler.Invoke(data);
            }
        }

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeTest<T>(string key, Func<EventMessageModel<T>, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channelProvider = GetChannel(key);

            if (channelProvider.Reader.TryRead(out var msg))
            {
                Console.WriteLine($"{DateTime.Now} {key} 收到数据：{msg}");

                var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg);
                await handler.Invoke(data);
            }
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueueTest<T>(string key, Action<Func<int, List<T>>> handler)
        {
            SubscribeTest<T>(key, msg =>
            {
                List<T> GetListFunc(int length) => GetQueueItems<T>(key, length);

                handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeQueueTest<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            await SubscribeTest<T>(key, async msg =>
            {
                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(key, length);

                await handler.Invoke(GetListFunc);
            });
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 分批次消费 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">每次处理间隔 毫秒</param>
        /// <param name="exceptionHandler">异常处理方式</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="error">发生异常时回调</param>
        /// <param name="completed">本次消费完成回调 最后执行</param>
        public async Task SubscribeQueueTest<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler,
            Func<Exception, List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            if (delay <= 0)
                throw new Exception("delay must be greater than zero");

            await SubscribeTest<T>(key, async msg =>
            {
                var pages = GetTotalPagesFromQueue(key, length);

                while (pages > 0)
                {
                    var data = await GetQueueItemsAsync<T>(key, length);
                    if (!data.Any())
                        break;

                    Exception ex = null;

                    try
                    {
                        await handler.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        ex = e;

                        if (await HandleException(key, exceptionHandler, data, e))
                        {
                            pages = 1;
                            return;
                        }
                    }
                    finally
                    {
                        if (ex != null && error != null)
                            await HandleError(key, data, error, ex);

                        if (completed != null && pages == 1)
                            await HandleCompleted(key, completed);
                    }

                    pages--;

                    await Task.Delay(delay);
                }
            });
        }

        public void Dispose()
        {
            ChannelProviderDictionary.Clear();
            ErrorQueueDictionary.Clear();
            QueueDictionary.Clear();
        }

        #endregion
    }
}