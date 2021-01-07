using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AopCache.Core.Abstractions
{
    /// <summary>
    /// EventBus
    /// </summary>
    public interface IEventBusProvider
    {
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
        Task PublishAsync<T>(string channel, EventMessageModel<T> message);

        /// <summary>
        /// 发布事件 数据放到队列，并发布通知到订阅者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="message">数据集合</param>
        /// <returns></returns>
        Task PublishQueueAsync<T>(string channel, List<T> message);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void Subscribe<T>(string channel, Action<EventMessageModel<T>> handler);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void Subscribe<T>(string channel, Func<EventMessageModel<T>, Task> handler);

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeQueue<T>(string channel, Action<Func<int, List<T>>> handler);

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeQueue<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler);

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
        void SubscribeQueue<T>(string channel, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<List<T>, Task> error = null, Func<Task> completed = null);

        /// <summary>
        /// 获取某个频道队列数据量
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <returns></returns>
        int GetQueueLength(string channel);

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        List<T> GetQueueItems<T>(string channel, int length);

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        Task<List<T>> GetQueueItemsAsync<T>(string channel, int length);

        /// <summary>
        /// 获取某个频道错误队列数据量
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <returns></returns>
        int GetErrorQueueLength(string channel);

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        List<T> GetErrorQueueItems<T>(string channel, int length);

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="channel">频道名称</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        Task<List<T>> GetErrorQueueItemsAsync<T>(string channel, int length);

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel"></param>
        void UnSubscribe(string channel);

        #region Test
        
        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeTest<T>(string channel, Action<EventMessageModel<T>> handler);
        
        /// <summary>
        /// 订阅事件 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        Task SubscribeTest<T>(string channel, Func<EventMessageModel<T>, Task> handler);

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeQueueTest<T>(string channel, Action<Func<int, List<T>>> handler);

        /// <summary>
        /// 订阅事件 从队列读取数据 用于单元测试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">频道名称</param>
        /// <param name="handler">订阅处理</param>
        Task SubscribeQueueTest<T>(string channel, Func<Func<int, Task<List<T>>>, Task> handler);

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
        Task SubscribeQueueTest<T>(string channel, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<List<T>, Task> error = null, Func<Task> completed = null);

        #endregion
    }

    /// <summary>
    /// 事件消息模型
    /// </summary>
    public class EventMessageModel<T>
    {
        /// <summary>
        /// 频道
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// TrackId
        /// </summary>
        public string TrackId { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public EventMessageModel()
        {
            TrackId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public EventMessageModel(T data, string trackId = null)
        {
            Data = data;
            TrackId = trackId ?? Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// 处理错误枚举
    /// </summary>
    public enum ExceptionHandlerEnum
    {
        /// <summary>
        /// 忽略错误，继续执行
        /// </summary>
        Continue,
        /// <summary>
        /// 停止执行
        /// </summary>
        Stop,
        /// <summary>
        /// 重新加入本身队列 并且停止执行
        /// </summary>
        PushToSelfQueueAndStop,
        /// <summary>
        /// 重新加入本身队列 并且忽略错误，继续执行
        /// </summary>
        PushToSelfQueueAndContinue,
        /// <summary>
        /// 加入错误队列 并且停止执行
        /// </summary>
        PushToErrorQueueAndStop,
        /// <summary>
        /// 加入错误队列 并且忽略错误，继续执行
        /// </summary>
        PushToErrorQueueAndContinue,
    }
}