using AopCache.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.Implements
{
    /// <summary>
    /// 订阅服务
    /// </summary>
    public class SubscriberWorker : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;
        private readonly IAopCacheProvider _cacheProvider;

        private static readonly Dictionary<string, List<(AopCacheAttribute, AopSubscriberAttribute)>>
            ChannelSubscribersDictionary = new Dictionary<string, List<(AopCacheAttribute, AopSubscriberAttribute)>>();

        /// <summary>
        /// 初始化 订阅服务
        /// </summary>
        /// <param name="eventBusProvider"></param>
        /// <param name="cacheProvider"></param>
        public SubscriberWorker(IEventBusProvider eventBusProvider, IAopCacheProvider cacheProvider)
        {
            _eventBusProvider = eventBusProvider;
            _cacheProvider = cacheProvider;
        }

        /// <summary>
        /// 处理订阅
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"{DateTime.Now} ----------------------------------------");

            foreach (var channel in AopSubscriberAttribute.ChannelList)
            {
                Console.WriteLine($"{DateTime.Now} AopCache：生产者频道：{channel}");

                var methodList = AopSubscriberAttribute.MethodList.Where(d =>
                    d.GetCustomAttributes<AopSubscriberAttribute>().ToList().Exists(t => t.Channel == channel)).ToList();

                var subscribers = new List<(AopCacheAttribute, AopSubscriberAttribute)>();

                foreach (var method in methodList)
                {
                    var cacheAttribute = method.GetCustomAttribute<AopCacheAttribute>();
                    if (cacheAttribute == null)
                        continue;

                    Console.WriteLine($"{DateTime.Now} AopCache：消费者订阅方法：{method.DeclaringType?.FullName}.{method.Name}");

                    var subscriberTag = method.GetCustomAttributes<AopSubscriberAttribute>().First(d => d.Channel == channel);

                    subscribers.Add((cacheAttribute, subscriberTag));
                }

                ChannelSubscribersDictionary.Add(channel, subscribers);

                Console.WriteLine($"{DateTime.Now} ----------------------------------------");
            }

            //开始订阅
            foreach (var keyValuePair in ChannelSubscribersDictionary)
            {
                _eventBusProvider.Subscribe<Dictionary<string, object>>(keyValuePair.Key, msg =>
                {
                    foreach ((AopCacheAttribute cache, AopSubscriberAttribute sub) item in keyValuePair.Value)
                    {
                        var cacheAttribute = item.cache;
                        var subscriberTag = item.sub;

                        switch (subscriberTag.ActionType)
                        {
                            case ActionType.DeleteByKey:

                                var key = subscriberTag.GetKey(cacheAttribute.Key, msg.Data);
                                key = cacheAttribute.FormatPrefix(key);
                                _cacheProvider.Remove(key);
                                Console.WriteLine($"{DateTime.Now} Key：{msg.Key}：清除缓存：{key}");
                                break;
                            //case ActionType.DeleteByGroup:
                            //    break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }, true);
            }

            await Task.CompletedTask;
        }
    }
}