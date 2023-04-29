using System;
using Microsoft.Extensions.DependencyInjection;
using TemporalActivityGen;

namespace TestApplication;
internal class Program
{
    private static void Main(string[] args)
    {
        var delegates = ActivityProxyHelper.DiscoverAndRegisterActivityProxies(new ServiceCollection().BuildServiceProvider(), typeof(IOrderProcessingActivities).Assembly);

        foreach (var @delegate in delegates)
        {
            Console.WriteLine(@delegate.Method.Name);
        }
    }
}
