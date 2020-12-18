using AopCache.Abstractions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AopCache.Implements
{
    public class SubscriberWorker : BackgroundService
    {
        private readonly IAopEventBusProvider _eventBusProvider;
        private readonly IAopCacheProvider _cacheProvider;

        //private static readonly Dictionary<string, List<MethodInfo>> _channelMethodDictionary =
        //    new Dictionary<string, List<MethodInfo>>();


        private static readonly Dictionary<string, List<(AopCacheAttribute, AopSubscriberTagAttribute)>>
            ChannelAttributeDictionary =
                new Dictionary<string, List<(AopCacheAttribute, AopSubscriberTagAttribute)>>();

        public SubscriberWorker(IAopEventBusProvider eventBusProvider,IAopCacheProvider cacheProvider)
        {
            _eventBusProvider = eventBusProvider;
            _cacheProvider = cacheProvider;
        }
        
        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"{DateTime.Now} ----------------------------------------");

            foreach (var channel in AopSubscriberTagAttribute.ChannelList)
            {
                Console.WriteLine($"{DateTime.Now} AopCache：生产者频道：{channel}");

                var methodList = AopSubscriberTagAttribute.MethodList.Where(d =>
                    d.GetCustomAttributes<AopSubscriberTagAttribute>().ToList().Exists(t => t.Channel == channel)).ToList();

                //_channelMethodDictionary.Add(channel, methodList);

                var attrs = new List<(AopCacheAttribute, AopSubscriberTagAttribute)>();

                foreach (var method in methodList)
                {
                    var cacheAttribute = method.GetCustomAttribute<AopCacheAttribute>();
                    if (cacheAttribute == null)
                        continue;

                    Console.WriteLine($"{DateTime.Now} AopCache：消费者订阅方法：{method.DeclaringType?.FullName}.{method.Name}");

                    var subscriberTag = method.GetCustomAttributes<AopSubscriberTagAttribute>().First(d => d.Channel == channel);

                    attrs.Add((cacheAttribute, subscriberTag));
                }

                ChannelAttributeDictionary.Add(channel, attrs);

                Console.WriteLine($"{DateTime.Now} ----------------------------------------");
            }

            foreach (var keyValuePair in ChannelAttributeDictionary)
            {
                _eventBusProvider.Subscribe<Dictionary<string, object>>(keyValuePair.Key, msg =>
                {
                    var data = msg.Data;

                    foreach (var valueTuple in keyValuePair.Value)
                    {
                        var cacheAttribute = valueTuple.Item1;
                        var subscriberTag = valueTuple.Item2;
                        
                        switch (subscriberTag.ActionType)
                        {
                            case ActionType.DeleteByKey:

                                var key = subscriberTag.GetKey(cacheAttribute.Key, data);
                                key = cacheAttribute.FormatPrefix(key);
                                _cacheProvider.Remove(key);
                                Console.WriteLine($"{DateTime.Now} Channel：{msg.Channel}：清除缓存：{key}");
                                break;
                            case ActionType.DeleteByGroup:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                });
            }
        }
    }
}