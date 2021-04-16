using AopCache.EventBus.RabbitMQ.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AopCache.EventBus.RabbitMQ.Runtime
{
    /// <summary>
    /// 依赖引导器
    /// </summary>
    public class DependencyRegistrator
    {
        private TypeFinder TypeFinder { get; set; } = new TypeFinder();

        private List<Assembly> Assemblies { get; set; }

        /// <summary>
        /// ServiceCollection
        /// </summary>
        public static IServiceCollection ServiceCollection { get; set; }

        /// <summary>
        /// RpcServer 列表
        /// </summary>
        public static List<MethodInfo> RpcServerMethodList = new List<MethodInfo>();

        /// <summary>
        /// Subscriber 列表
        /// </summary>
        public static List<MethodInfo> SubscriberMethodList = new List<MethodInfo>();

        /// <summary>
        /// 初始化
        /// </summary>
        public DependencyRegistrator()
        {
            Assemblies = TypeFinder.GetAssemblies().ToList();
        }

        /// <summary>
        /// 设置ServiceCollection
        /// </summary>
        /// <param name="serviceCollection"></param>
        public void SetServiceCollection(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        /// <summary>
        /// 注册依赖服务
        /// </summary>
        public void RegisterServices()
        {
            RegisterTransientDependency();
        }

        private void RegisterTransientDependency()
        {
            var types = Assemblies.SelectMany(d => d.GetTypes().Where(t => t.IsClass)).ToList();

            RegisterRpcServer(types);
            RegisterSubscriber(types);
        }

        private void RegisterRpcServer(List<Type> types)
        {
            types ??= Assemblies.SelectMany(d => d.GetTypes().Where(t => t.IsClass)).ToList();
            var methodInfos = types.SelectMany(d => d.GetMethods()).Where(d => d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(RpcServerAttribute))).ToList();

            //去重复
            var dicMethod = new Dictionary<int, MethodInfo>();
            methodInfos.ForEach(m =>
            {
                dicMethod.TryAdd(m.MetadataToken, m);
            });

            RpcServerMethodList = dicMethod.Select(d => d.Value).ToList();

            var first = RpcServerMethodList.Select(d => d.GetCustomAttribute<RpcServerAttribute>().GetFormatKey()).GroupBy(d => d)
                .ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value)
                .FirstOrDefault();

            if (first.Value > 1)
                throw new ArgumentException($"RpcServr Key 重复：{first.Key}");
        }

        private void RegisterSubscriber(List<Type> types)
        {
            types ??= Assemblies.SelectMany(d => d.GetTypes().Where(t => t.IsClass)).ToList();
            var methodInfos = types.SelectMany(d => d.GetMethods()).Where(d => d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(SubscriberAttribute))).ToList();

            //去重复
            var dicMethod = new Dictionary<int, MethodInfo>();
            methodInfos.ForEach(m =>
            {
                dicMethod.TryAdd(m.MetadataToken, m);
            });

            SubscriberMethodList = dicMethod.Select(d => d.Value).ToList();

            var first = SubscriberMethodList.Select(d => d.GetCustomAttribute<SubscriberAttribute>().GetFormatKey()).GroupBy(d => d)
                .ToDictionary(d => d.Key, d => d.Count()).OrderByDescending(d => d.Value)
                .FirstOrDefault();

            if (first.Value > 1)
                throw new ArgumentException($"Subscriber Key 重复：{first.Key}");
        }
    }
}
