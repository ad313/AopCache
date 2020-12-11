//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace AopCache.Test
//{
//    /// <summary>
//    /// ip访问限制操作类
//    /// </summary>
//    public class OneLock
//    {
//        //存储生命周期内的数据
//        //private static readonly ConcurrentDictionary<TargetType, ConcurrentQueue<Tuple<string, DateTime>>> GuestQueue = new ConcurrentDictionary<TargetType, ConcurrentQueue<Tuple<string, DateTime>>>();

//        private static readonly LazyConcurrentDictionary<TargetType, LazyConcurrentDictionary<string, ConcurrentQueue<DateTime>>> GuestQueue = new LazyConcurrentDictionary<TargetType, LazyConcurrentDictionary<string, ConcurrentQueue<DateTime>>>();

//        //限制列表 存储已触发规则的元素
//        private static readonly LazyConcurrentDictionary<TargetType, LazyConcurrentDictionary<string, DateTime>> GuestLimit = new LazyConcurrentDictionary<TargetType, LazyConcurrentDictionary<string, DateTime>>();

//        //配置文件组名
//        private const string ConfigGroupNameKey = "OneLock";
//        //时间锁秒数
//        private const string TimeLengthKey = "TimeLength";
//        //期间每个访问者能访问的次数
//        private const string GuestLengthKey = "GuestLength";
//        //触发规则后等待时间
//        private const string WaitSecondsKey = "WaitSeconds";
//        //队列中数据的生命周期偏移值 
//        private const string PixSecondsKey = "PixSeconds";

//        public static List<OneLockOption> OneLockOptions { get; set; } = new List<OneLockOption>()
//        {
//            new OneLockOption()
//            {
//                Name = "CreateOrder",
//                TimeLength = 30,
//                GuestLength = 10,
//                WaitSeconds = 3,
//                PixSeconds = 2
//            }
//        };

//        /// <summary>
//        /// 入口
//        /// </summary>
//        /// <param name="guest">来访者唯一标示</param>
//        /// <param name="tarType">类型</param>
//        /// <returns></returns>
//        public static bool Target(string guest, TargetType tarType)
//        {
//            //检测当前来访者是否在限制列表中
//            if (GuestLimit.ContainsKey(tarType) && GuestLimit[tarType].ContainsKey(guest))
//            {
//                if (GuestLimit[tarType][guest] > DateTime.Now)
//                    return false;
//                GuestLimit[tarType].TryRemove(guest, out DateTime currDateTime);
//            }
//            return DoCheck(guest, tarType);
//        }

//        /// <summary>
//        /// 执行检查
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="tarType"></param>
//        /// <returns></returns>
//        private static bool DoCheck(string key, TargetType tarType)
//        {
//            var option = GetConfigValue(tarType, TimeLengthKey);
//            var now = DateTime.Now;
//            //不包括此类型或者 在规则之内
//            if (!GuestQueue.ContainsKey(tarType) || !GuestQueue[tarType].ContainsKey(key) ||
//                GuestQueue[tarType][key].ToArray().Count(d => d >= now.AddSeconds(-option.TimeLength)) < option.GuestLength)
//            {
//                //加入队列
//                GuestQueue.AddOrUpdate(tarType, k =>
//                {
//                    var dicTemp = new LazyConcurrentDictionary<string, ConcurrentQueue<DateTime>>();
//                    dicTemp.TryAdd(key, new ConcurrentQueue<DateTime>(new[] { now }));
//                    return dicTemp;
//                }, (k, oldValue) =>
//                {
//                    oldValue.AddOrUpdate(key,d=> new ConcurrentQueue<DateTime>(new[] { now }), (k1, oldDic) =>
//                    {
//                        oldDic.Enqueue(now);
//                        return oldDic;
//                    });
//                    return oldValue;
//                });
//                return true;
//            }


//            GuestLimit.AddOrUpdate(tarType, dicKey =>
//            {
//                var dic = new LazyConcurrentDictionary<string, DateTime>();
//                dic.TryAdd(key, now.AddSeconds(option.WaitSeconds));
//                return dic;
//            }, (dicKey, oldValue) =>
//            {
//                oldValue.AddOrUpdate(key, k => now.AddSeconds(option.WaitSeconds), (k, oValue) => oValue.AddSeconds(option.WaitSeconds));
//                return oldValue;
//            });

//            //清除过期的资源
//            ClearTimeOutData();
//            return false;
//        }

//        /// <summary>
//        /// 清除特定的项
//        /// </summary>
//        /// <param name="targetType">类型</param>
//        /// <param name="key">key</param>
//        public static void ClearGuestLock(TargetType targetType, string key)
//        {
//            if (GuestLimit.ContainsKey(targetType))
//            {
//                GuestLimit[targetType].TryRemove(key, out DateTime currDateTime);
//            }

//            if (GuestQueue.ContainsKey(targetType))
//            {
//                GuestQueue[targetType].TryRemove(key, out ConcurrentQueue<DateTime> currConcurrentQueue);
//            }
//        }

//        /// <summary>
//        /// 获取配置
//        /// </summary>
//        /// <param name="targetType">当前类型</param>
//        /// <param name="key">字典key</param>
//        /// <returns></returns>
//        private static OneLockOption GetConfigValue(TargetType targetType, string key)
//        {
//            return OneLockOptions.FirstOrDefault(d => d.Name == "CreateOrder");
//        }

//        /// <summary>
//        /// 清除过期的资源
//        /// </summary>
//        private static void ClearTimeOutData()
//        {
//            var option = GetConfigValue(TargetType.CreateOrder , TimeLengthKey);
//            var currTask = Task.Run(() =>
//            {
//                //检测限制列表中的数据是否过期，过期则移除
//                foreach (var typeItem in GuestLimit)
//                {
//                    typeItem.Value.Value.Instance.Where(d => d.Value.Value <= DateTime.Now).Select(d => d.Key).ToList().ForEach(k =>
//                    {
//                        GuestLimit[typeItem.Key].TryRemove(k, out DateTime currDateTime);
//                    });
//                }

//                //清理队列数据
//                foreach (var kv in GuestQueue)
//                {
//                    foreach (var kvItem in kv.Value.Value)
//                    {
//                        while (true)
//                        {
//                            //每次检测队列数据，当最旧的一条时间在生命周期内（加上偏移时间），则跳出，否则，移除过期数据
//                            if (!kvItem.Value.Value.TryPeek(out DateTime currDateTime))
//                            {
//                                break;
//                            }
//                            if (currDateTime >= DateTime.Now.AddSeconds(-option.TimeLength - option.PixSeconds))
//                            {
//                                break;
//                            }
//                            //真正的移除
//                            GuestQueue[kv.Key][kvItem.Key].TryDequeue(out currDateTime);
//                        }
//                    }
//                }
//            });
//            //currTask.Wait();
//        }

//        /// <summary>
//        /// 获取当前队列
//        /// </summary>
//        /// <returns></returns>
//        public static LazyConcurrentDictionary<TargetType, LazyConcurrentDictionary<string, ConcurrentQueue<DateTime>>> GetCurrentQueue()
//        {
//            return GuestQueue;
//        }
//    }

//    /// <summary>
//    /// 触发类型 订单生成、购买记录、订单详情
//    /// </summary>
//    public enum TargetType
//    {
//        [Description("CreateOrder")]
//        CreateOrder = 0,
//        [Description("PurchList")]
//        PurchList = 1,
//        [Description("OrderDetails")]
//        OrderDetails = 2,
//    }


//    public class OneLockOption
//    {
//        /// <summary>
//        /// 锁名称
//        /// </summary>
//        public string Name { get; set; }
//        /// <summary>
//        /// 时间锁秒数
//        /// </summary>
//        public int TimeLength { get; set; }
//        /// <summary>
//        /// 期间每个访问者能访问的次数
//        /// </summary>
//        public int GuestLength { get; set; }
//        /// <summary>
//        /// 触发规则后等待时间
//        /// </summary>
//        public int WaitSeconds { get; set; }
//        /// <summary>
//        /// 队列中数据的生命周期偏移值 
//        /// </summary>
//        public int PixSeconds { get; set; }
//    }



//    public class LazyConcurrentDictionary<TKey, TValue>
//    {
//        public readonly ConcurrentDictionary<TKey, Lazy<TValue>> Instance;

//        public LazyConcurrentDictionary()
//        {
//            this.Instance = new ConcurrentDictionary<TKey, Lazy<TValue>>();
//        }

//        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
//        {
//            var lazyResult = this.Instance.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));

//            return lazyResult.Value;
//        }


//        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory,
//            Func<TKey, TValue, TValue> updateValueFactory)
//        {
//            var lazyResult = Instance.AddOrUpdate(key, 
//                k => new Lazy<TValue>(() => addValueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication),
//                (k,v) => new Lazy<TValue>(() => updateValueFactory(k,v.Value), LazyThreadSafetyMode.ExecutionAndPublication));

//            return lazyResult.Value;
//        }

//        public bool ContainsKey(TKey key)
//        {
//            return Instance.ContainsKey(key);
//        }

//        public TValue this[TKey key]
//        {
//            get => Instance[key].Value;
//            set => Instance[key]= new Lazy<TValue>(()=>value, LazyThreadSafetyMode.ExecutionAndPublication);
//        }

//        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
//        {
//            var result = Instance.TryRemove(key, out Lazy<TValue> lazyValue);
//            value = lazyValue.Value;
//            return result;
//        }

//        public bool TryAdd(TKey key, TValue value)
//        {
//            var result = Instance.TryAdd(key, new Lazy<TValue>(()=>value, LazyThreadSafetyMode.ExecutionAndPublication));
            
//            return result;
//        }

//        public IEnumerator<KeyValuePair<TKey, Lazy<TValue>>> GetEnumerator()
//        {
//            return Instance.GetEnumerator();
//        }
//    }
//}