using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AopCache.Runtime
{
    /// <summary>
    /// 依赖引导器
    /// </summary>
    public class DependencyRegistrator
    {
        private TypeFinder TypeFinder { get; set; }

        private IServiceCollection Services { get; set; }

        private List<Assembly> Assemblies { get; set; }

        private ILogger<DependencyRegistrator> Logger { get; }
        
        public DependencyRegistrator(IServiceCollection services, TypeFinder typeFinder)
        {
            Logger = new LoggerFactory().CreateLogger<DependencyRegistrator>();
            TypeFinder = typeFinder ?? new TypeFinder();
            Services = services;
            Assemblies = TypeFinder.GetAssemblies().ToList();
        }

        /// <summary>
        /// 获取类型集合
        /// </summary>
        private Type[] GetTypes<T>()
        {
            return TypeFinder.Find<T>(Assemblies).ToArray();
        }


        ///// <summary>
        ///// 启动引导
        ///// </summary>
        //public IServiceProvider Run()
        //{
        //    return ServiceFactory.ServiceProvider.Register(Services, RegisterServices, _configs);
        //}

        public void RegisterServices()
        {
          
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Logger.LogInformation("开始自动注册服务");
          
            RegisterTransientDependency();
            //ResolveDependencyRegistrar();
            watch.Stop();
            Logger.LogInformation($"结束自动注册服务,耗时\t{watch.ElapsedMilliseconds}\t毫秒");
            //return Services;
        }
       

        private void RegisterTransientDependency()
        {
            var methods = TypeFinder.FindAllInterface(Assemblies).SelectMany(d => d.GetMethods()).ToList();

            var publishers = methods.SelectMany(d => d.GetCustomAttributes<AopPublisherAttribute>()).ToList();
            var check = publishers.Select(d => d.Channel).GroupBy(d => d).ToDictionary(d => d.Key, d => d.Count()).Where(d => d.Value > 1).ToList();
            if (check.Any())
            {
                throw new Exception($"[AopCache AopPublisherAttribute] [Channel 重复：{string.Join("、", check.Select(d => d.Key))}]");
            }

            var subscribers = methods.SelectMany(d => d.GetCustomAttributes<AopSubscriberTagAttribute>()).ToList();
            if (subscribers.Any())
            {
                AopSubscriberTagAttribute.ChannelList = subscribers.Select(d => d.Channel).Distinct().ToList();
                //foreach (var channel in AopSubscriberTagAttribute.ChannelList)
                //{
                //    Console.WriteLine($"{DateTime.Now} AopCache：发布事件频道：{channel}");
                //}

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


                //foreach (var method in AopSubscriberTagAttribute.MethodList)
                //{
                //    var cacheAttribute = method.GetCustomAttribute<AopCacheAttribute>();
                //    Console.WriteLine($"{DateTime.Now} AopCache：触发方法：{method.Name}：{cacheAttribute.Key}");

                //    var subscriberTag = method.GetCustomAttributes<AopSubscriberTagAttribute>().ToList();
                //    foreach (var tagAttribute in subscriberTag)
                //    {
                //        Console.WriteLine($"{DateTime.Now} AopCache：订阅频道：{tagAttribute.Channel}");
                //    }

                //    Console.WriteLine($"{DateTime.Now} ----------------------------------------");
                //}
            }
        }



        ///// <summary>
        ///// 解析依赖注册器
        ///// </summary>
        //private void ResolveDependencyRegistrar()
        //{
        //    var types = GetTypes<IDependencyRegistrar>();
        //    types.Select(type => Reflection.CreateInstance<IDependencyRegistrar>(type)).ToList().ForEach(t => t.Register(Services));
        //}

    }
}
