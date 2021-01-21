using System;

namespace AopCache.EventBus.RabbitMQ
{
    public class RabbitMqConfig
    {
        /// <summary>
        /// 交换机
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// HostName
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 账户
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 虚拟主机
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// prefetchSize
        /// </summary>
        public uint PrefetchSize { get; set; } = 0;

        /// <summary>
        /// prefetchCount
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// 数据队列key前缀
        /// </summary>
        public string DataQueuePrefixKey { get; private set; } = "aop.cache.queue.data.";

        /// <summary>
        /// 数据错误队列key前缀
        /// </summary>
        public string DataErrorQueuePrefixKey { get; private set; } = "aop.cache.queue.data.error.";

        /// <summary>
        /// 普通队列key前缀
        /// </summary>
        public string SampleQueuePrefixKey { get; private set; } = "aop.cache.queue.sample.";

        public void Check()
        {
            if (string.IsNullOrWhiteSpace(ExchangeName))
                throw new ArgumentNullException(nameof(ExchangeName));

            if (string.IsNullOrWhiteSpace(HostName))
                throw new ArgumentNullException(nameof(HostName));

            if (string.IsNullOrWhiteSpace(UserName))
                throw new ArgumentNullException(nameof(UserName));

            if (string.IsNullOrWhiteSpace(Password))
                throw new ArgumentNullException(nameof(Password));

            if (Port <= 0)
                throw new ArgumentException(nameof(Port));
        }
    }
}