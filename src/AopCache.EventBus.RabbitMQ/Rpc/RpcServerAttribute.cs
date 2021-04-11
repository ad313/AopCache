using System;

namespace AopCache.EventBus.RabbitMQ.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcServerAttribute : Attribute
    {
        /// <summary>
        /// RpcServer 唯一标识
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// RpcServer 分组
        /// </summary>
        public string Group { get; private set; }

        public RpcServerAttribute(string key, string group = null)
        {
            Key = key;
            Group = group;

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
        }

        public string GetFormatKey() => string.IsNullOrWhiteSpace(Group) ? Key : $"{Group}_{Key}";
    }
}
