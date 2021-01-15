using AopCache.Core.Abstractions;
using AopCache.Core.Common;
using CSRedis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AopCache.EventBus.CSRedis
{
    /// <summary>
    /// 基于CsRedis的发布订阅实现
    /// </summary>
    public class RedisEventBusProvider : IEventBusProvider
    {
        /// <summary>
        /// ServiceProvider
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        private readonly ISerializerProvider _serializerProvider;

        private readonly ConcurrentDictionary<string, CSRedisClient.SubscribeObject> _subscribeDictionary = new ConcurrentDictionary<string, CSRedisClient.SubscribeObject>();

        /// <summary>
        /// 异步锁
        /// </summary>
        private readonly ConcurrentDictionary<string, AsyncLock> _lockObjectDictionary = new ConcurrentDictionary<string, AsyncLock>();
        
        /// <summary>
        /// 队列key前缀
        /// </summary>
        private static readonly string PrefixKey = "EventBusProvider:";

        /// <summary>
        /// 总开关默认开启
        /// </summary>
        public bool Enable { get; private set; } = true;

        /// <summary>
        /// 频道开关
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _channelEnableDictionary = new ConcurrentDictionary<string, bool>();

        public RedisEventBusProvider(ISerializerProvider serializerProvider, IServiceProvider serviceProvider)
        {
            _serializerProvider = serializerProvider;
            ServiceProvider = serviceProvider;
        }

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
            await RedisHelper.PublishAsync(channel, _serializerProvider.Serialize(message));
        }

        /// <summary>
        /// 发布事件 数据放到队列，并发布通知到订阅者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="message">数据集合</param>
        /// <returns></returns>
        public async Task PublishQueueAsync<T>(string channel, params T[] message)
        {
            if (message == null)
                return;

            await PushToQueueAsync(channel, message.ToList());
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
            SubscribeInternal(channel, handler, false, false);
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void Subscribe<T>(string channel, Func<EventMessageModel<T>, Task> handler)
        {
            SubscribeInternal(channel, handler, false, false);
        }
        
        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string channel, Action<Func<int, List<T>>> handler)
        {
            SubscribeInternal<T>(channel, msg =>
            {
                List<T> GetListFunc(int length) => GetQueueItems<T>(channel, length);

                handler.Invoke(GetListFunc);
            }, true);
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            SubscribeInternal<T>(channel, async msg =>
            {
                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(channel, length);

                await handler.Invoke(GetListFunc);
            }, true);
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
        public void SubscribeQueue<T>(string channel, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler,
            Func<List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");
            
            SubscribeInternal<T>(channel, async msg =>
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
            }, true);
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

            var key = GetChannelQueueKey(channel);
            return (int)RedisHelper.LLen(key);
        }

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetQueueItems<T>(string channel, int length)
        {
            var key = GetChannelQueueKey(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                var item = RedisHelper.RPop<T>(key);
                if (item != null)
                    list.Add(item);
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
            var key = GetChannelQueueKey(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                var item = await RedisHelper.RPopAsync<T>(key);
                if (item != null)
                    list.Add(item);
                else
                    break;
            }

            return list;
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

            var key = GetChannelErrorQueueKey(channel);
            return (int)RedisHelper.LLen(key);
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetErrorQueueItems<T>(string channel, int length)
        {
            var key = GetChannelErrorQueueKey(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                var item = RedisHelper.RPop<T>(key);
                if (item != null)
                    list.Add(item);
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
            var key = GetChannelErrorQueueKey(channel);
            var list = new List<T>();

            while (list.Count < length)
            {
                var item = await RedisHelper.RPopAsync<T>(key);
                if (item != null)
                    list.Add(item);
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
            if (_subscribeDictionary.TryGetValue(channel, out CSRedisClient.SubscribeObject ob))
            {
                ob.Unsubscribe();
                ob.Dispose();
            }
        }

        /// <summary>
        /// 设置订阅是否消费
        /// </summary>
        /// <param name="enable">true 开启开关，false 关闭开关</param>
        /// <param name="channel">为空时表示总开关</param>
        public void SetEnable(bool enable, string channel = null)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                Enable = enable;
                return;
            }

            _channelEnableDictionary.AddOrUpdate(channel, d => enable, (key, value) => enable);
        }

        #region private

        private void SubscribeInternal<T>(string channel, Action<EventMessageModel<T>> handler, bool useLock = false, bool checkEnable = true)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var sub = RedisHelper.Subscribe((channel, msg =>
            {
                if (checkEnable && !IsEnable(channel))
                {
                    Console.WriteLine($"{DateTime.Now} 频道【{channel}】 已关闭消费");
                    return;
                }

                if (useLock)
                {
                    lock (GetLockObject(channel))
                    {
                        //Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg.Body);
                        handler.Invoke(data);
                    }
                }
                else
                {
                    //Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                    var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg.Body);
                    handler.Invoke(data);
                }
            }
            ));

            _subscribeDictionary.TryAdd(channel, sub);
            _lockObjectDictionary.TryAdd(channel, new AsyncLock());
        }

        private void SubscribeInternal<T>(string channel, Func<EventMessageModel<T>, Task> handler, bool useLock = false, bool checkEnable = true)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var sub = RedisHelper.Subscribe((channel, async msg =>
            {
                if (checkEnable && !IsEnable(channel))
                {
                    Console.WriteLine($"{DateTime.Now} 频道【{channel}】 已关闭消费");
                    return;
                }

                if (useLock)
                {
                    using (await GetLockObject(channel).LockAsync())
                    {
                        //Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                        var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg.Body);
                        await handler.Invoke(data);
                    }
                }
                else
                {
                    //Console.WriteLine($"{DateTime.Now} 收到数据：{msg.Body}");
                    var data = _serializerProvider.Deserialize<EventMessageModel<T>>(msg.Body);
                    await handler.Invoke(data);
                }
            }
            ));

            _subscribeDictionary.TryAdd(channel, sub);
            _lockObjectDictionary.TryAdd(channel, new AsyncLock());
        }

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

        private int GetTotalPagesFromQueue(string channel, int length)
        {
            var total = GetQueueLength(channel);

            return total <= 0 ? 0 : (total / length + (total % length > 0 ? 1 : 0));
        }

        private async Task PushToQueueAsync<T>(string channel, List<T> data, int length = 500)
        {
            if (data == null || !data.Any())
                return;

            var key = GetChannelQueueKey(channel);

            if (data.Count > length)
            {
                foreach (var list in Helpers.SplitList(data, length))
                {
                    await RedisHelper.LPushAsync(key, list.ToArray());
                    await Task.Delay(10);
                }
            }
            else
            {
                await RedisHelper.LPushAsync(key, data.ToArray());
            }
        }

        private string GetChannelQueueKey(string channel)
        {
            return PrefixKey + channel;
        }

        private async Task PushToErrorQueueAsync<T>(string channel, List<T> data)
        {
            if (data == null || !data.Any())
                return;

            var key = GetChannelErrorQueueKey(channel);
            await RedisHelper.LPushAsync(key, data.ToArray());
        }

        private string GetChannelErrorQueueKey(string channel)
        {
            return PrefixKey + "Error:" + channel;
        }

        private AsyncLock GetLockObject(string channel)
        {
            if (_lockObjectDictionary.TryGetValue(channel, out AsyncLock obj))
                return obj;

            return new AsyncLock();
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
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeTest<T>(string channel, Action<EventMessageModel<T>> handler)
        {
            SubscribeInternal(channel, handler);
        }

        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeTest<T>(string channel, Func<EventMessageModel<T>, Task> handler)
        {
            SubscribeInternal(channel, handler);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueueTest<T>(string channel, Action<Func<int, List<T>>> handler)
        {

        }

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        public async Task SubscribeQueueTest<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            await Task.CompletedTask;
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
            await Task.CompletedTask;
        }


        #endregion
    }
}