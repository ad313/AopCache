using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace AopCache.EventBus.RabbitMQ
{
    internal class ChannelPool : IPooledObjectPolicy<IModel>
    {
        private readonly IConnection _connection;

        public ChannelPool(IConnection connection)
        {
            _connection = connection;
        }
        
        public IModel Create()
        {
            return _connection.CreateModel();
        }
        
        public bool Return(IModel obj)
        {
            return true;
        }
    }
}