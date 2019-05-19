using System;
using Grpc.Core;

namespace NCoreUtils.Images.Server
{
    static class ServerCallContextExtensions
    {
        sealed class ServiceProviderKeyClass { }

        readonly static object _key = new ServiceProviderKeyClass();

        public static void InjectRequestServices(this ServerCallContext context, IServiceProvider serviceProvider)
        {
            context.UserState[_key] = serviceProvider;
        }

        public static IServiceProvider GetRequestServices(this ServerCallContext context)
            => (IServiceProvider)context.UserState[_key];
    }
}