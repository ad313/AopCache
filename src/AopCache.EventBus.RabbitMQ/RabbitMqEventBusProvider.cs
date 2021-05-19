using AopCache.Core.Abstractions;
using AopCache.Core.Common;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.EventBus.RabbitMQ
{
    /// <summary>
    /// 基于 RabbitMQ 的发布订阅实现
    /// </summary>
    public class RabbitMqEventBusProvider : IEventBusProvider
    {
        /// <summary>
        /// ServiceProvider
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        private readonly ISerializerProvider _serializerProvider;
        private readonly RabbitMqClientProvider _rabbitMqClientProvider;
        private readonly RabbitMqClientProvider _rabbitMqClientProviderRead;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<RabbitMqEventBusProvider> _logger;

        /// <summary>
        /// 总开关默认开启
        /// </summary>
        public bool Enable { get; private set; } = true;

        /// <summary>
        /// 频道开关
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _channelEnableDictionary = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// key queue 名称字典
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _queueNameDictionary = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 异步锁
        /// </summary>
        private readonly ConcurrentDictionary<string, AsyncLock> _lockObjectDictionary = new ConcurrentDictionary<string, AsyncLock>();

        /// <summary>
        /// Rpc client 标识
        /// </summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<RpcResult>> _rpcCallbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<RpcResult>>();

        public RabbitMqEventBusProvider(ISerializerProvider serializerProvider,
            IServiceProvider serviceProvider,
            RabbitMqClientProvider rabbitMqClientProvider,
            RabbitMqClientProvider rabbitMqClientProviderRead,
            ILogger<RabbitMqEventBusProvider> logger)
        {
            _serializerProvider = serializerProvider;
            _rabbitMqClientProvider = rabbitMqClientProvider;
            _rabbitMqClientProviderRead = rabbitMqClientProviderRead;
            _config = _rabbitMqClientProvider.Config;
            _logger = logger;
            ServiceProvider = serviceProvider;
        }

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

            //广播模式
            if (broadcast)
            {
                await PublishBroadcastAsync(key, message);
                return;
            }

            var channel = _rabbitMqClientProvider.GetChannel();
            channel.ConfirmSelect();

            //持久化
            var properties = channel.BasicProperties();

            channel.ExchangeDeclare(
                exchange: _config.ExchangeName,
                durable: true,
                type: "topic",
                autoDelete: false);

            message.Key = key;
            var body = _serializerProvider.SerializeBytes(message);

            channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: key,
                basicProperties: properties,
                body: body);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogDebug($"RabbitMQ topic message [{key}] has been published. message:{ _serializerProvider.Serialize(message)}");

            _rabbitMqClientProvider.ReturnChannel(channel);

            await Task.CompletedTask;
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
            if (message == null)
                return;

            await PushToQueueAsync(key, message);
            await PublishAsync(key, new EventMessageModel<T>(), true);
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
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));

            var channel = _rabbitMqClientProvider.GetChannel();
            channel.ConfirmSelect();

            var dic = new Dictionary<string, object>
            {
                {"x-expires", (seconds + 60) * 1000},
                {"x-message-ttl", seconds * 1000}, //队列上消息过期时间，应小于队列过期时间  
                {"x-dead-letter-exchange", _config.DeadLetterExchange}, //过期消息转向路由  
                {"x-dead-letter-routing-key", _config.GetDeadLetterRouteKey(key)} //过期消息转向路由相匹配routingkey  
            };

            var queueKey = _config.GetDeadLetterHostQueueKey(key, seconds);

            channel.QueueDeclare(queue: queueKey,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: dic);

            message.Key = key;
            var body = _serializerProvider.SerializeBytes(message);

            //持久化
            var properties = channel.BasicProperties();

            //向该消息队列发送消息message
            channel.BasicPublish(exchange: "",
                routingKey: queueKey,
                basicProperties: properties,
                body: body);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogDebug($"RabbitMQ topic message [{queueKey}] has been published. message:{ _serializerProvider.Serialize(message)}");

            _rabbitMqClientProvider.ReturnChannel(channel);

            await Task.CompletedTask;
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
            var seconds = (long)(absoluteTime - DateTime.Now).TotalSeconds;
            if (seconds <= 0)
                throw new ArgumentException("absoluteTime must be greater than current time");

            await DelayPublishAsync(key, seconds + 1, message);
        }

        public async Task<RpcResult<T>> RpcClientAsync<T>(string key, object[] message = null, int timeout = 30)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var channel = _rabbitMqClientProvider.GetChannel();
            channel.ConfirmSelect();

            //var replyQueueName = channel.QueueDeclare(_config.GetRpcClientQueueKey(key)).QueueName;
            var replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                _rabbitMqClientProvider.ReturnChannel(channel);

                if (!_rpcCallbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<RpcResult> tcs))
                    return;
                
                tcs.TrySetResult(_serializerProvider.Deserialize<RpcResult>(ea.Body.ToArray()));
            };

            var result = await RpcClientCallAsync(key, channel, consumer, replyQueueName, message, timeout);
            _logger.LogDebug($"RabbitMQ rpc client [{key}] receive { _serializerProvider.Serialize(result)}");

            await Task.Delay(1);

            return new RpcResult<T>()
            {
                Data = typeof(T) == typeof(string)
                    ? (T) ((object) result.Data)
                    : (string.IsNullOrWhiteSpace(result.Data)
                        ? default(T)
                        : _serializerProvider.Deserialize<T>(result.Data)),
                Success = result.Success,
                ErrorMessage = result.ErrorMessage
            };
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
            if (broadcast)
                SubscribeBroadcastInternal(key, handler, false);
            else
                SubscribeInternal(key, handler, false);
        }
        
        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void DelaySubscribe<T>(string key, Action<EventMessageModel<T>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (IModel channel, string queue, string routeKey) resource = GetDelayResource(key);

            var consumer = new EventingBasicConsumer(resource.channel);
            consumer.Received += (ch, ea) =>
            {
                try
                {
                    if (!IsEnable(key))
                    {
                        _logger.LogWarning($"{DateTime.Now} 频道【{resource.routeKey}】 已关闭消费");
                        return;
                    }

                    _logger.LogDebug($"{DateTime.Now}：频道【{resource.routeKey}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                    handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{resource.routeKey}] 消费异常 {e.Message} ");
                }
                finally
                {
                    resource.channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            resource.channel.BasicConsume(queue: resource.queue, autoAck: false, consumer: consumer);
        }

        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void DelaySubscribe<T>(string key, Func<EventMessageModel<T>, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (IModel channel, string queue, string routeKey) resource = GetDelayResource(key);

            var consumer = new EventingBasicConsumer(resource.channel);
            consumer.Received += async (ch, ea) =>
            {
                try
                {
                    if (!IsEnable(key))
                    {
                        _logger.LogWarning($"{DateTime.Now} 频道【{resource.routeKey}】 已关闭消费");
                        return;
                    }

                    _logger.LogDebug($"{DateTime.Now}：频道【{resource.routeKey}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                    await handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{resource.routeKey}] 消费异常 {e.Message} ");
                }
                finally
                {
                    resource.channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            resource.channel.BasicConsume(queue: resource.queue, autoAck: false, consumer: consumer);
        }

        /// <summary>
        /// 订阅事件 RpcServer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void RpcServer<T>(string key, Func<T, Task<RpcResult>> handler)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _logger.LogWarning($"{DateTime.Now} RabbitMQ RpcServer 启动 " +
                               $"[{key}] " +
                               $"......");

            var channel = _rabbitMqClientProvider.Channel;

            channel.QueueDeclare(queue: _config.GetRpcServerQueueKey(key), durable: false, exclusive: false, autoDelete: true, arguments: null);

            //channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: _config.GetRpcServerQueueKey(key), autoAck: false, consumer: consumer);
            consumer.Received += async (model, ea) =>
            {
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;
                
                RpcResult response = null;

                try
                {
                    _logger.LogDebug($"{DateTime.Now}：RpcServer [{key}] 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");
                    
                    response = await handler.Invoke(_serializerProvider.Deserialize<T>(ea.Body.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{DateTime.Now} RabbitMQ RpcServer [{key}] 执行异常 {e.Message} ");
                    
                    response = new RpcResult($"RpcServer [{key}] 执行异常", e);
                }
                finally
                {
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: _serializerProvider.SerializeBytes(response));
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };
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
            if (broadcast)
                SubscribeBroadcastInternal(key, handler, false);
            else
                SubscribeInternal(key, handler, false);
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string key, Action<Func<int, List<T>>> handler)
        {
            SubscribeBroadcastInternal<T>(key, msg =>
            {
                List<T> GetListFunc(int length) => GetQueueItems<T>(key, length);
                handler.Invoke(GetListFunc);
            }, true);
        }

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        public void SubscribeQueue<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler)
        {
            SubscribeBroadcastInternal<T>(key, async msg =>
            {
                Task<List<T>> GetListFunc(int length) => GetQueueItemsAsync<T>(key, length);
                await handler.Invoke(GetListFunc);
            }, true);
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
        public void SubscribeQueue<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler,
            Func<Exception, List<T>, Task> error = null, Func<Task> completed = null)
        {
            if (length <= 0)
                throw new Exception("length must be greater than zero");

            SubscribeBroadcastInternal<T>(key, async msg =>
            {
                while (true)
                {
                    List<T> data = null;
                    Exception ex = null;
                    var isCompleted = false;

                    try
                    {
                        data = await GetQueueItemsAsync<T>(key, length);
                        if (!data.Any())
                        {
                            isCompleted = true;
                            return;
                        }

                        await handler.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        ex = e;

                        if (await HandleException(key, exceptionHandler, data, e))
                            return;
                    }
                    finally
                    {
                        if (ex != null && error != null)
                            await HandleError(key, data, error, ex);

                        if (completed != null && isCompleted)
                            await HandleCompleted(key, completed);
                    }

                    if (isCompleted)
                        break;

                    if (delay > 0)
                        await Task.Delay(delay);
                }
            }, true);

            _lockObjectDictionary.TryAdd(key, new AsyncLock());
        }
        
        /// <summary>
        /// 获取某个频道队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public int GetQueueLength(string key)
        {
            IModel channel = null;
            try
            {
                var queueName = GetChannelQueueKey(key);
                channel = _rabbitMqClientProvider.GetChannel();
                return (int) channel.MessageCount(queueName);
            }
            catch
            {
                return 0;
            }
            finally
            {
                _rabbitMqClientProvider.ReturnChannel(channel);
            }
        }

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetQueueItems<T>(string key, int length)
        {
            lock (GetLockObject(key))
            {
                var queueName = GetChannelQueueKey(key);
                return GetQueueItemsMethod<T>(queueName, length);
            }
        }
        
        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="queueName">queueName</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        private List<T> GetQueueItemsMethod<T>(string queueName, int length)
        {
            var channel = _rabbitMqClientProviderRead.GetChannel();

            var list = new List<T>();

            while (list.Count < length)
            {
                if (channel.IsClosed)
                    channel = _rabbitMqClientProviderRead.GetChannel();

                try
                {
                    var result = channel.BasicGet(queueName, false);
                    if (result == null)
                        break;

                    try
                    {
                        list.Add(_serializerProvider.Deserialize<T>(result.Body.ToArray()));
                        channel.BasicAck(result.DeliveryTag, false);
                    }
                    catch
                    {
                        //PushToQueueAsync("abcvfdrerer", new List<int>() { 1 }).GetAwaiter().GetResult();
                        //Console.WriteLine("-------------------------------------------------------------------------------get new channel");
                        channel.BasicReject(result.DeliveryTag, true);
                        Thread.Sleep(500);
                    }

                    if (result.MessageCount <= 0)
                        break;
                }
                catch (OperationInterruptedException ex)
                {
                    //队列不存在
                    if(ex.ShutdownReason?.ReplyCode == 404)
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    channel = _rabbitMqClientProviderRead.GetChannel();
                    continue;
                }
            }

            _rabbitMqClientProviderRead.ReturnChannel(channel);
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
            using (await GetLockObject(key).LockAsync())
            {
                var queueName = GetChannelQueueKey(key);
                return await Task.FromResult(GetQueueItemsMethod<T>(queueName, length));
            }
        }

        /// <summary>
        /// 获取某个频道错误队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public int GetErrorQueueLength(string key)
        {
            IModel channel = null;
            try
            {
                var queueName = GetChannelErrorQueueKey(key);
                channel = _rabbitMqClientProvider.GetChannel();
                return (int)channel.MessageCount(queueName);
            }
            catch
            {
                return 0;
            }
            finally
            {
                _rabbitMqClientProvider.ReturnChannel(channel);
            }
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public List<T> GetErrorQueueItems<T>(string key, int length)
        {
            lock (GetLockObject(key))
            {
                var queueName = GetChannelErrorQueueKey(key);
                return GetQueueItemsMethod<T>(queueName, length);
            }
        }

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        public async Task<List<T>> GetErrorQueueItemsAsync<T>(string key, int length)
        {
            using (await GetLockObject(key).LockAsync())
            {
                var queueName = GetChannelErrorQueueKey(key);
                return await Task.FromResult(GetErrorQueueItems<T>(queueName, length));
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="key"></param>
        public void UnSubscribe(string key)
        {
            var channel = _rabbitMqClientProvider.Channel;
            var queue = GetTempChannelQueueName(key);
            var exchange = _config.ExchangeName;
            channel.QueueUnbind(queue, exchange, key);
        }

        /// <summary>
        /// 设置发布订阅是否开启
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

        //#region Test

        ///// <summary>
        ///// 订阅事件 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //public void SubscribeTest<T>(string key, Action<EventMessageModel<T>> handler)
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// 订阅事件 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //public async Task SubscribeTest<T>(string key, Func<EventMessageModel<T>, Task> handler)
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// 订阅事件 从队列读取数据 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //public void SubscribeQueueTest<T>(string key, Action<Func<int, List<T>>> handler)
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// 订阅事件 从队列读取数据 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //public async Task SubscribeQueueTest<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler)
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// 订阅事件 从队列读取数据 分批次消费 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="length">每次处理条数</param>
        ///// <param name="delay">每次处理间隔 毫秒</param>
        ///// <param name="exceptionHandler">异常处理方式</param>
        ///// <param name="handler">订阅处理</param>
        ///// <param name="error">发生异常时回调</param>
        ///// <param name="completed">本次消费完成回调 最后执行</param>
        //public async Task SubscribeQueueTest<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler,
        //    Func<Exception, List<T>, Task> error = null, Func<Task> completed = null)
        //{
        //    throw new NotImplementedException();
        //}

        //#endregion

        public void Dispose()
        {
            _rabbitMqClientProvider.Dispose();
            _rabbitMqClientProviderRead.Dispose();
        }

        #region private

        private async Task PublishBroadcastAsync<T>(string key, EventMessageModel<T> message)
        {
            key = FormatBroadcastKey(key);

            var channel = _rabbitMqClientProvider.GetChannel();
            channel.ConfirmSelect();
            channel.ExchangeDeclare(key, "fanout");
            
            message.Key = key;
            var body = _serializerProvider.SerializeBytes(message);

            channel.BasicPublish(exchange: key,
                routingKey: "",
                basicProperties: null,
                body: body);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogDebug($"RabbitMQ broadcast message [{key}] has been published. message:{ _serializerProvider.Serialize(message)}");

            _rabbitMqClientProvider.ReturnChannel(channel);

            await Task.CompletedTask;
        }

        private void SubscribeInternal<T>(string key, Action<EventMessageModel<T>> handler, bool checkEnable = true)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _logger.LogWarning($"{DateTime.Now} RabbitMQ 开始订阅 " +
                               $"[{_config.ExchangeName}] " +
                               $"[{key}] " +
                               $"[PrefetchSize：{_config.PrefetchSize}]" +
                               $"[PrefetchCount：{_config.PrefetchCount}]" +
                               $"......");

            var channel = _rabbitMqClientProvider.Channel;

            channel.ExchangeDeclare(
                exchange: _config.ExchangeName,
                durable: true,
                type: "topic",
                autoDelete: false);

            //队列
            var queueName = GetTempChannelQueueName(key);
            channel.QueueDeclare(queueName, true, false, false, null);

            channel.QueueBind(
                queue: queueName,
                exchange: _config.ExchangeName,
                routingKey: key);

            //限流
            channel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                try
                {
                    if (checkEnable && !IsEnable(key))
                    {
                        _logger.LogWarning($"{DateTime.Now} 频道【{key}】 已关闭消费");
                        return;
                    }

                    _logger.LogDebug($"{DateTime.Now}：频道【{key}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                    handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{key}] 消费异常 {e.Message} ");
                }
                finally
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            channel.BasicConsume(queueName, false, consumer);
        }

        private void SubscribeBroadcastInternal<T>(string key, Action<EventMessageModel<T>> handler, bool checkEnable = true)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            key = FormatBroadcastKey(key);

            _logger.LogWarning($"{DateTime.Now} RabbitMQ 广播模式 开始订阅 " +
                               $"[{key}] " +
                               $"......");

            var channel = _rabbitMqClientProvider.Channel;
            channel.ExchangeDeclare(key, "fanout");

            //队列
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queueName, key, "");

            //限流
            channel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                try
                {
                    if (checkEnable && !IsEnable(key))
                    {
                        _logger.LogWarning($"{DateTime.Now} 频道【{key}】 已关闭消费");
                        return;
                    }

                    _logger.LogDebug($"{DateTime.Now}：频道【{key}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                    handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{key}] 消费异常 {e.Message} ");
                }
                finally
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            channel.BasicConsume(queueName, false, consumer);
        }

        private void SubscribeInternal<T>(string key, Func<EventMessageModel<T>, Task> handler, bool checkEnable = true)
        {
            //Task.Factory.StartNew(() =>
            //{
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException(nameof(key));

                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                _logger.LogWarning($"{DateTime.Now} RabbitMQ 开始订阅 " +
                                   $"[{_config.ExchangeName}] " +
                                   $"[{key}] " +
                                   $"[PrefetchSize：{_config.PrefetchSize}]" +
                                   $"[PrefetchCount：{_config.PrefetchCount}]" +
                                   $"......");

                var channel = _rabbitMqClientProvider.Channel;

                channel.ExchangeDeclare(
                    exchange: _config.ExchangeName,
                    durable: true,
                    type: "topic",
                    autoDelete: false);

                //队列
                var queueName = GetTempChannelQueueName(key);
                channel.QueueDeclare(queueName, true, false, false, null);

                channel.QueueBind(
                    queue: queueName,
                    exchange: _config.ExchangeName,
                    routingKey: key);

                //限流
                channel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (ch, ea) =>
                {
                    try
                    {
                        if (checkEnable && !IsEnable(key))
                        {
                            _logger.LogWarning($"{DateTime.Now} 频道【{key}】 已关闭消费");
                            return;
                        }

                        _logger.LogDebug($"{DateTime.Now}：频道【{key}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                        await handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{key}] 消费异常 {e.Message} ");
                    }
                    finally
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                };

                channel.BasicConsume(queueName, false, consumer);
            //}, TaskCreationOptions.LongRunning);
        }

        private void SubscribeBroadcastInternal<T>(string key, Func<EventMessageModel<T>, Task> handler, bool checkEnable = true)
        {
            Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException(nameof(key));

                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                key = FormatBroadcastKey(key);

                _logger.LogWarning($"{DateTime.Now} RabbitMQ 广播模式 开始订阅 " +
                                   $"[{key}] " +
                                   $"......");

                var channel = _rabbitMqClientProvider.Channel;
                channel.ExchangeDeclare(key, "fanout");

                //队列
                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queueName, key, "");

                //限流
                channel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (ch, ea) =>
                {
                    try
                    {
                        if (checkEnable && !IsEnable(key))
                        {
                            _logger.LogWarning($"{DateTime.Now} 频道【{key}】 已关闭消费");
                            return;
                        }

                        _logger.LogDebug($"{DateTime.Now}：频道【{key}】 收到消息： {Encoding.Default.GetString(ea.Body.ToArray())}");

                        await handler.Invoke(_serializerProvider.Deserialize<EventMessageModel<T>>(ea.Body.ToArray()));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"{DateTime.Now} RabbitMQ [{key}] 消费异常 {e.Message} ");
                    }
                    finally
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                };

                channel.BasicConsume(queueName, false, consumer);
            }, TaskCreationOptions.LongRunning);
        }

        private (IModel, string, string) GetDelayResource(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _logger.LogWarning($"{DateTime.Now} RabbitMQ 开始订阅 死信队列 " +
                               $"[{_config.DeadLetterExchange}] " +
                               $"[{_config.GetDeadLetterWorkQueueKey(key)}] " +
                               $"[PrefetchSize：{_config.PrefetchSize}]" +
                               $"[PrefetchCount：{_config.PrefetchCount}]" +
                               $"......");

            var channel = _rabbitMqClientProvider.Channel;

            channel.ExchangeDeclare(exchange: _config.DeadLetterExchange, type: "direct");
            var queue = _config.GetDeadLetterWorkQueueKey(key);
            var routeKey = _config.GetDeadLetterRouteKey(key);
            channel.QueueDeclare(queue, true, false, false, null);
            channel.QueueBind(queue: queue, exchange: _config.DeadLetterExchange, routingKey: routeKey);

            //限流
            channel.BasicQos(_config.PrefetchSize, _config.PrefetchCount, false);

            return (channel, queue, routeKey);
        }

        private async Task PushToQueueAsync<T>(string key, List<T> data, int length = 1000)
        {
            if (data == null || !data.Any())
                return;

            var queueName = GetChannelQueueKey(key);
            await PushToQueueAsyncMethod(queueName, data, length);
        }

        private async Task PushToErrorQueueAsync<T>(string key, List<T> data, int length = 10000)
        {
            if (data == null || !data.Any())
                return;

            var queueName = GetChannelErrorQueueKey(key);
            await PushToQueueAsyncMethod(queueName, data, length);
        }

        private async Task PushToQueueAsyncMethod<T>(string queueName, List<T> data, int length = 10000)
        {
            if (data == null || !data.Any())
                return;

            var channel = _rabbitMqClientProvider.GetChannel();
            channel.QueueDeclare(queueName, true, false, false, null);

            //持久化
            var properties = channel.BasicProperties();

            if (data.Count > length)
            {
                foreach (var list in Helpers.SplitList(data, length))
                {
                    foreach (var item in list)
                    {
                        channel.BasicPublish("", queueName, properties, _serializerProvider.SerializeBytes(item));
                    }
                    await Task.Delay(10);
                }
            }
            else
            {
                foreach (var item in data)
                {
                    channel.BasicPublish("", queueName, properties, _serializerProvider.SerializeBytes(item));
                }
            }

            _rabbitMqClientProvider.ReturnChannel(channel);
        }

        private Task<RpcResult> RpcClientCallAsync(string key, IModel channel, EventingBasicConsumer consumer, string queueName, object[] message, int timeout)
        {
            timeout = timeout > 0 ? timeout : 120;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            var correlationId = Guid.NewGuid().ToString();

            var tcs = new TaskCompletionSource<RpcResult>();
            _rpcCallbackMapper.TryAdd(correlationId, tcs);

            var props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            channel.BasicPublish(
                exchange: "",
                routingKey: _config.GetRpcServerQueueKey(key),
                basicProperties: props,
                body: _serializerProvider.SerializeBytes(message));

            channel.BasicConsume(
                consumer: consumer,
                queue: queueName,
                autoAck: true);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogDebug($"RabbitMQ rpc client [{key}] has been send. message:{ _serializerProvider.Serialize(message)}");

            tokenSource.Token.Register(() =>
            {
                if (_rpcCallbackMapper.TryRemove(correlationId, out var tmp))
                    tmp.SetResult(new RpcResult(null, new TimeoutException($"超时时间 {timeout} s")));
            });

            return tcs.Task;
        }

        private string GetChannelQueueKey(string key)
        {
            return _config.DataQueuePrefixKey + key;
        }

        private string GetChannelErrorQueueKey(string channel)
        {
            return _config.DataErrorQueuePrefixKey + channel;
        }

        private string GetTempChannelQueueName(string key)
        {
            if (_queueNameDictionary.TryGetValue(key, out string name))
            {
                return name;
            }

            name = _config.SampleQueuePrefixKey + key;

            _queueNameDictionary.TryAdd(key, name);

            return name;
        }

        private bool IsEnable(string key)
        {
            return Enable && (!_channelEnableDictionary.TryGetValue(key, out bool enable) || enable);
        }

        private async Task<bool> HandleException<T>(string channel, ExceptionHandlerEnum exceptionHandler, List<T> data, Exception e)
        {
            try
            {
                var text = $"{DateTime.Now} {channel} 队列消费端异常：{e.Message}";
                _logger.LogError(e, text);

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
                        _logger.LogError($"{DateTime.Now} 不支持的 ExceptionHandlerEnum 类型：{exceptionHandler}");
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{channel} 异常处理异常：{ex.Message}");
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
                _logger.LogError(ex, $"{channel} error func 执行异常：{ex.Message}");
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
                _logger.LogError(ex, $"{channel} Completed func 执行异常：{ex.Message}");
            }
        }

        private int GetTotalPagesFromQueue(string channel, int length)
        {
            var total = GetQueueLength(channel);

            return total <= 0 ? 0 : (total / length + (total % length > 0 ? 1 : 0));
        }

        private AsyncLock GetLockObject(string key)
        {
            if (_lockObjectDictionary.TryGetValue(key, out AsyncLock obj))
                return obj;

            return new AsyncLock();
        }


        private string FormatBroadcastKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            return $"aop_cache_broadcast_{key}";
        }

        #endregion
    }
}