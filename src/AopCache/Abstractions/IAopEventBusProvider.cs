using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AopCache.Abstractions
{
    /// <summary>
    /// Aop EventBus
    /// </summary>
    public interface IAopEventBusProvider
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishAsync<T>(string channel, AopMessageModel<T> message);

        /// <summary>
        /// 发布事件 发布数据到队列，并发布通知
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task PublishToQueueAsync<T>(string channel, List<T> data);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        void Subscribe<T>(string channel, Action<AopMessageModel<T>> handler);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        void Subscribe<T>(string channel, Func<AopMessageModel<T>, Task> handler);

        /// <summary>
        /// 订阅事件 队列中有新数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        void SubscribeFromQueue<T>(string channel, Action<Func<int, List<T>>> message);

        /// <summary>
        /// 订阅事件 队列中有新数据 分批次消费
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="length">每次处理条数</param>
        /// <param name="delay">处理完一次的停顿时间 毫秒</param>
        /// <param name="rollbackToQueueWhenException">当处理失败时是否把数据重新加入到队列</param>
        /// <param name="message"></param>
        void SubscribeFromQueue<T>(string channel, int length, int delay, bool rollbackToQueueWhenException, Action<List<T>> message);
    }

    /// <summary>
    /// 事件消息模型
    /// </summary>
    public class AopMessageModel<T>
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
        public Guid TrackId { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public AopMessageModel()
        {
            TrackId = Guid.NewGuid();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public AopMessageModel(T data, Guid? trackId = null)
        {
            Data = data;
            TrackId = trackId ?? Guid.NewGuid();
        }
    }
}