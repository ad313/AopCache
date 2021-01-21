using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;
using System.Reflection;
using System.Threading;

namespace AopCache.EventBus.RabbitMQ
{
    public class RabbitMqClientProvider : IDisposable
    {
        public RabbitMqConfig Config { get; }
        private const int DefaultChannelPoolSize = 10;
        private readonly ILogger<RabbitMqClientProvider> _logger;
        private static ConnectionFactory ConnectionFactory { get; set; }
        private static DefaultObjectPool<IModel> _channelPool;

        public IConnection Connection { get; private set; }
        public IModel Channel { get; private set; }

        public RabbitMqClientProvider(RabbitMqConfig config, ILogger<RabbitMqClientProvider> logger)
        {
            Config = config;
            _logger = logger;

            CreateConnection();
            CreateChannelPool();
        }
        
        public IModel GetChannel()
        {
            IModel channel = null;
            while (true)
            {
                channel = _channelPool.Get();
                if (channel.IsOpen)
                    break;

                Thread.Sleep(10);
            }

            return channel;
        }

        public void ReturnChannel(IModel channel)
        {
            _channelPool.Return(channel);
        }

        private void CreateConnection()
        {
            Config.Check();

            ConnectionFactory = new ConnectionFactory
            {
                UserName = Config.UserName,
                Password = Config.Password,
                HostName = Config.HostName,
                Port = Config.Port,
                VirtualHost = Config.VirtualHost,
                AutomaticRecoveryEnabled = true
            };

            var name = $"{Assembly.GetEntryAssembly()?.GetName().Name.ToLower()}_{Guid.NewGuid()}";
            Connection = ConnectionFactory.CreateConnection(name);
            Connection.ConnectionShutdown += Connection_ConnectionShutdown;

            _logger.LogInformation($"{DateTime.Now} RabbitMQ 连接成功：Host：{Config.HostName}，UserName：{Config.UserName} [{name}]");

            Channel = Connection.CreateModel();
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning($"{DateTime.Now} RabbitMQ Connection Shutdown......");
        }

        private void CreateChannelPool()
        {
            var policy = new ChannelPool(Connection);
            _channelPool = new DefaultObjectPool<IModel>(policy, DefaultChannelPoolSize);

            _logger.LogInformation($"{DateTime.Now} channel 连接池初始化成功，连接池大小 {DefaultChannelPoolSize}");
        }

        public void Dispose()
        {
            Channel?.Dispose();
            Connection?.Dispose();
        }
    }

    static class Extensions
    {
        /// <summary>
        /// 持久化
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static IBasicProperties BasicProperties(this IModel channel)
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            return properties;
        }
    }
}