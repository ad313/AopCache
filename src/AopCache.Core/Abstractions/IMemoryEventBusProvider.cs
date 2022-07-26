using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AopCache.Core.Abstractions
{
    /// <summary>
    /// EventBus
    /// </summary>
    public interface IMemoryEventBusProvider : IDisposable
    {
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
        Task PublishAsync<T>(string key, EventMessageModel<T> message, bool broadcast = false);

        /// <summary>
        /// 发布事件 数据放到队列，并发布通知到订阅者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="message">数据集合</param>
        /// <returns></returns>
        Task PublishQueueAsync<T>(string key, List<T> message);
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        void Subscribe<T>(string key, Action<EventMessageModel<T>> handler, bool broadcast = false);
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        void Subscribe<T>(string key, Func<EventMessageModel<T>, Task> handler, bool broadcast = false);

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeQueue<T>(string key, Action<Func<int, List<T>>> handler);

        /// <summary>
        /// 订阅事件 从队列读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        void SubscribeQueue<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler);

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
        void SubscribeQueue<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<Exception, List<T>, Task> error = null, Func<Task> completed = null);

        /// <summary>
        /// 获取某个频道队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        int GetQueueLength(string key);

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        List<T> GetQueueItems<T>(string key, int length);

        /// <summary>
        /// 获取某个频道队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        Task<List<T>> GetQueueItemsAsync<T>(string key, int length);

        /// <summary>
        /// 获取某个频道错误队列数据量
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        int GetErrorQueueLength(string key);

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        List<T> GetErrorQueueItems<T>(string key, int length);

        /// <summary>
        /// 获取某个频道错误队列数据
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="length">获取指定的数据条数</param>
        /// <returns></returns>
        Task<List<T>> GetErrorQueueItemsAsync<T>(string key, int length);

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="key"></param>
        void UnSubscribe(string key);

        /// <summary>
        /// 设置发布订阅是否开启
        /// </summary>
        /// <param name="enable">true 开启开关，false 关闭开关</param>
        /// <param name="key">为空时表示总开关</param>
        void SetEnable(bool enable, string key = null);
    }
}