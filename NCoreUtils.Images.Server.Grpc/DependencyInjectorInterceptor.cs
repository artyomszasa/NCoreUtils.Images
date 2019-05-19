using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Images.Server
{
    public class DependencyInjectorInterceptor : Interceptor
    {

        readonly IServiceProvider _serviceProvider;

        public DependencyInjectorInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        async Task<TResponse> WithServiceProvider<TResponse>(ServerCallContext context, Func<Task<TResponse>> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                context.InjectRequestServices(scope.ServiceProvider);
                return await action();
            }
        }

        async Task WithServiceProvider(ServerCallContext context, Func<Task> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                context.InjectRequestServices(scope.ServiceProvider);
                await action();
            }
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
            => WithServiceProvider(context, () => continuation(request, context));

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
            => WithServiceProvider(context, () => continuation(requestStream, context));

        public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
            => WithServiceProvider(context, () => continuation(request, responseStream, context));

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
            => WithServiceProvider(context, () => continuation(requestStream, responseStream, context));

    }
}