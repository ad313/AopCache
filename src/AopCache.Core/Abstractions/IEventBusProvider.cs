using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AopCache.Core.Abstractions
{
    /// <summary>
    /// EventBus
    /// </summary>
    public interface IEventBusProvider : IDisposable
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
        /// 发布事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="seconds">延迟秒数</param>
        /// <param name="message">数据</param>
        /// <returns></returns>
        Task DelayPublishAsync<T>(string key, long seconds, EventMessageModel<T> message);

        /// <summary>
        /// 发布事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="absoluteTime">指定执行时间</param>
        /// <param name="message">数据</param>
        /// <returns></returns>
        Task DelayPublishAsync<T>(string key, DateTime absoluteTime, EventMessageModel<T> message);

        /// <summary>
        /// 发布事件 RpcClient
        /// </summary>
        /// <typeparam name="T">发送数据</typeparam>
        /// <param name="key">Key 唯一值</param>
        /// <param name="message">数据</param>
        /// <param name="timeout">超时时间 秒</param>
        /// <returns></returns>
        Task<RpcResult> RpcClientAsync<T>(string key, T message, int timeout = 30);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        /// <param name="broadcast">是否广播模式（注：对内存队列和redis无效）</param>
        void Subscribe<T>(string key, Action<EventMessageModel<T>> handler, bool broadcast = false);

        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        void DelaySubscribe<T>(string key, Action<EventMessageModel<T>> handler);

        /// <summary>
        /// 订阅事件 延迟队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key</param>
        /// <param name="handler">订阅处理</param>
        void DelaySubscribe<T>(string key, Func<EventMessageModel<T>, Task> handler);

        /// <summary>
        /// RpcServer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key 唯一</param>
        /// <param name="handler">订阅处理</param>
        void RpcServer<T>(string key, Func<T, Task<RpcResult>> handler);

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

        //#region Test

        ///// <summary>
        ///// 订阅事件 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //void SubscribeTest<T>(string key, Action<EventMessageModel<T>> handler);

        ///// <summary>
        ///// 订阅事件 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //Task SubscribeTest<T>(string key, Func<EventMessageModel<T>, Task> handler);

        ///// <summary>
        ///// 订阅事件 从队列读取数据 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //void SubscribeQueueTest<T>(string key, Action<Func<int, List<T>>> handler);

        ///// <summary>
        ///// 订阅事件 从队列读取数据 用于单元测试
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">Key</param>
        ///// <param name="handler">订阅处理</param>
        //Task SubscribeQueueTest<T>(string key, Func<Func<int, Task<List<T>>>, Task> handler);

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
        //Task SubscribeQueueTest<T>(string key, int length, int delay, ExceptionHandlerEnum exceptionHandler, Func<List<T>, Task> handler, Func<Exception, List<T>, Task> error = null, Func<Task> completed = null);

        //#endregion
    }

    /// <summary>
    /// 事件消息模型
    /// </summary>
    public class EventMessageModel<T>
    {
        /// <summary>
        /// 频道
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// TraceId
        /// </summary>
        public string TraceId { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public EventMessageModel()
        {
            TraceId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public EventMessageModel(T data, string trackId = null)
        {
            Data = data;
            TraceId = trackId ?? Guid.NewGuid().ToString();
        }
    }
    
    public class RpcResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        public RpcResult() { }

        public RpcResult(string data)
        {
            Data = data;
            Success = true;
        }

        public RpcResult(string data, Exception ex)
        {
            if (ex != null)
                ErrorMessage = ex.Message + (ex.InnerException == null ? "" : "|" + ex.InnerException.Message);

            Data = data;
            Success = false;
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