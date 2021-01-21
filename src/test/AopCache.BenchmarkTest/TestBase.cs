using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreUtil.BenchTest
{
    public class TestBase
    {
        public TestBase()
        {
            IServiceCollection services = new ServiceCollection();

           
        }

        public IServiceProvider Provider { get; }
    }
}