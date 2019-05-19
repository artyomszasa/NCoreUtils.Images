using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images.Server
{
    public class LoggerInterceptor : Interceptor
    {
        readonly ILogger _logger;

        public LoggerInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        async Task Logged(ServerCallContext context, Func<Task> action)
        {
            var stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation("Incoming request to {0}.", context.Method);
                stopwatch.Start();
                await action();
                stopwatch.Stop();
                _logger.LogInformation("Request to {0} processed successfully in {1}ms.", context.Method, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception exn)
            {
                stopwatch.Stop();
                _logger.LogError(exn, "Request to {0} has failed, execution took {1}ms.", context.Method, stopwatch.Elapsed.TotalMilliseconds);
                throw;
            }
        }

        async Task<T> Logged<T>(ServerCallContext context, Func<Task<T>> action)
        {
            var stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation("Incoming request to {0}.", context.Method);
                stopwatch.Start();
                var res = await action();
                stopwatch.Stop();
                _logger.LogInformation("Request to {0} processed successfully in {1}ms.", context.Method, stopwatch.Elapsed.TotalMilliseconds);
                return res;
            }
            catch (Exception exn)
            {
                stopwatch.Stop();
                _logger.LogError(exn, "Request to {0} has failed, execution took {1}ms.", context.Method, stopwatch.Elapsed.TotalMilliseconds);
                throw;
            }
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
            => Logged(context, () => continuation(request, context));

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
            => Logged(context, () => continuation(requestStream, context));

        public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
            => Logged(context, () => continuation(request, responseStream, context));

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
            => Logged(context, () => continuation(requestStream, responseStream, context));
    }
}