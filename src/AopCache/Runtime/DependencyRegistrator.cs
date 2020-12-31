using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AopCache.Runtime
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
        public static IServiceCollection ServiceCollection { get;set; }

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
            //虚函数
            var virtualClass = Assemblies.SelectMany(d => d.GetTypes().Where(t => t.IsClass && t.GetInterfaces().Length == 0)).ToList();
            var virtualMethods = virtualClass.SelectMany(d => d.GetMethods()).Where(d =>
                d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(AopPublisherAttribute)) ||
                d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(AopSubscriberTagAttribute))).ToList();

            var methods = TypeFinder.FindAllInterface(Assemblies).SelectMany(d => d.GetMethods()).ToList();
            methods.AddRange(virtualMethods);

            var publishers = methods.SelectMany(d => d.GetCustomAttributes<AopPublisherAttribute>()).ToList();
            var check = publishers.Select(d => d.Channel).GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).Where(d => d.Value > 1).ToList();
            if (check.Any())
            {
                throw new Exception($"[AopCache AopPublisherAttribute] [Channel 重复：{string.Join("、", check.Select(d => d.Key))}]");
            }

            //开启发布
            if (publishers.Any())
            {
                AopPublisherAttribute.Enable = true;
            }

            var existsList = methods.Where(d =>
                d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(AopPublisherAttribute)) &&
                d.CustomAttributes.Any(t => t.AttributeType.Name == nameof(AopCacheAttribute))).ToList();
            
            if (existsList.Any())
            {
                throw new Exception($"[AopCache AopPublisherAttribute] [不能与 AopCacheAttribute 一起使用 ：{string.Join("、", existsList.Select(d => $"{d.DeclaringType?.FullName}.{d.Name}"))}]");
            }
            
            var subscribers = methods.SelectMany(d => d.GetCustomAttributes<AopSubscriberTagAttribute>()).ToList();
            if (subscribers.Any())
            {
                AopSubscriberTagAttribute.ChannelList = subscribers.Select(d => d.Channel).Distinct().ToList();
                
                AopSubscriberTagAttribute.MethodList = methods.Where(d =>
                    d.CustomAttributes != null &&
                    d.CustomAttributes.Any(c => c.AttributeType.Name == nameof(AopSubscriberTagAttribute))).ToList();

                foreach (var subscriber in subscribers)
                {
                    if (string.IsNullOrWhiteSpace(subscriber.Map))
                        continue;

                    //特殊处理冒号，兼容冒号
                    subscriber.Map = subscriber.Map.Replace(":", ".");

                    var key = subscriber.GetMapDictionaryKey();

                    if (AopSubscriberTagAttribute.MapDictionary.ContainsKey(key))
                        continue;

                    var mapDic = subscriber.Map.Split(',')
                        .Where(d => d.IndexOf('=') > -1
                                    && !string.IsNullOrWhiteSpace(d.Split('=')[0])
                                    && !string.IsNullOrWhiteSpace(d.Split('=')[1]))
                        .ToDictionary(d => d.Split('=')[0].Trim(), d => d.Split('=')[1].Trim().TrimStart('{').TrimEnd('}'));
                    AopSubscriberTagAttribute.MapDictionary.TryAdd(key, mapDic);
                }
            }
        }
    }
}
