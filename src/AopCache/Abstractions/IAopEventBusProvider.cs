using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AopCache.Abstractions
{
    /// <summary>
    /// Aop EventBus
    /// </summary>
    public interface IAopEventBusProvider
    {
        Task PublishAsync<T>(string channel, AopMessageModel<T> message);

        void Subscribe<T>(string channel, Action<AopMessageModel<T>> message);
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