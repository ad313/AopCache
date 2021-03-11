//using Microsoft.Extensions.ObjectPool;
//using System;
//using System.Collections.Concurrent;
//using System.Threading;
//using System.Threading.Tasks;
//using Timer = System.Timers.Timer;

//namespace AopCache.Core.Common
//{
//    public static class TimerPoolFactory
//    {
//        private static DefaultObjectPool<Timer> _timerPool;

//        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<TimerWapper>> CallbackWapper = new ConcurrentDictionary<Guid, TaskCompletionSource<TimerWapper>>();

//        static TimerPoolFactory()
//        {
//            CreateTimerPool();
//        }

//        private static void CreateTimerPool()
//        {
//            var policy = new TimerPool();
//            _timerPool = new DefaultObjectPool<Timer>(policy, 10);
//        }

//        public static Task<TimerWapper> GetTimerAsync(int interval, Func<Task> action, CancellationToken cancellationToken = default)
//        {
//            var correlationId = Guid.NewGuid();

//            var tcs = new TaskCompletionSource<TimerWapper>();
//            CallbackWapper.TryAdd(correlationId, tcs);

//            var timer = _timerPool.Get();

//            void Finish()
//            {
//                if (CallbackWapper.TryRemove(correlationId, out var tmp)) tmp.TrySetResult(new TimerWapper(() => { _timerPool.Return(timer); }));
//            }

//            timer.Interval = interval;
//            timer.Elapsed += async (sender, args) =>
//            {
//                try
//                {
//                    await action.Invoke();
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"{DateTime.Now} TimerPool 执行任务出错：{ex.Message} {ex}");
//                }
//                finally
//                {
//                    Finish();
//                }
//            };
//            timer.Start();
            
//            cancellationToken.Register(Finish);

//            return tcs.Task;
//        }

//        public static Task<TimerWapper> GetTimer(int interval, Action action, CancellationToken cancellationToken = default)
//        {
//            var correlationId = Guid.NewGuid();

//            var tcs = new TaskCompletionSource<TimerWapper>();
//            CallbackWapper.TryAdd(correlationId, tcs);

//            var timer = _timerPool.Get();

//            void Finish()
//            {
//                if (CallbackWapper.TryRemove(correlationId, out var tmp)) tmp.TrySetResult(new TimerWapper(() => { _timerPool.Return(timer); }));
//            }

//            timer.Interval = interval;
//            timer.Elapsed += (sender, args) =>
//            {
//                try
//                {
//                    action.Invoke();
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"{DateTime.Now} TimerPool 执行任务出错：{ex.Message} {ex}");
//                }
//                finally
//                {
//                    Finish();
//                }
//            };
//            timer.Start();

//            Console.WriteLine($"begin--{DateTime.Now} ");

//            cancellationToken.Register(Finish);

//            return tcs.Task;
//        }
//    }

//    public class TimerPool : IPooledObjectPolicy<Timer>
//    {
//        public TimerPool() { }

//        public Timer Create()
//        {
//            return new Timer() { AutoReset = false, Enabled = true };
//        }

//        public bool Return(Timer timer)
//        {
//            timer.Stop();
//            return true;
//        }
//    }


//    public class TimerWapper : IDisposable
//    {
//        private readonly Action _dispose;

//        public TimerWapper(Action dispose)
//        {
//            _dispose = dispose;
//        }

//        public void Dispose()
//        {
//            _dispose?.Invoke();
//        }
//    }
//}
