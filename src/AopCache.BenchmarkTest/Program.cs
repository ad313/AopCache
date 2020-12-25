using System;
using BenchmarkDotNet.Running;

namespace AopCache.BenchmarkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var summary3 = BenchmarkRunner.Run<SerializerCloneTest>();

            Console.ReadLine();
        }
    }
}
