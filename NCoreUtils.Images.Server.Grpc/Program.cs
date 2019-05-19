using System;
using System.Linq;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Proto;

namespace NCoreUtils.Images.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            new GrpcStartup().ConfigureServices(serviceCollection);
            var server = new GrpcServer(serviceCollection.BuildServiceProvider(true));
            server.Start();
            Console.Read();
            server.Stop();
        }
    }
}
