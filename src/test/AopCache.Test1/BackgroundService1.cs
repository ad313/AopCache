using System;
using System.Threading;
using System.Threading.Tasks;
using AopCache.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Timer = System.Timers.Timer;

namespace AopCache.Test1
{
    public class BackgroundService1 : BackgroundService
    {
        private readonly IEventBusProvider _eventBusProvider;

        public BackgroundService1(IEventBusProvider eventBusProvider)
        {
            _eventBusProvider = eventBusProvider;
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            var timer = new Timer { Interval = 3000 };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var result = _eventBusProvider.RpcClientAsync<DateTime>("aaaaabbbbb").GetAwaiter().GetResult();
            Console.WriteLine($"-------{result.Success}: {result.Data}");
        }
    }
}