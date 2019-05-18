using System;
using Grpc.Core.Interceptors;
using Grpc.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images.Grpc.Server
{
    class Program
    {
        sealed class GrpcLoggerAdapter : global::Grpc.Core.Logging.ILogger
        {
            readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

            readonly Microsoft.Extensions.Logging.ILogger _logger;

            public GrpcLoggerAdapter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Logging.ILogger logger = null)
            {
                _loggerFactory = loggerFactory;
                _logger = logger ?? loggerFactory.CreateLogger("Grpc");
            }

            public void Debug(string message) => _logger.LogDebug(message);

            public void Debug(string format, params object[] formatArgs) => _logger.LogDebug(format, formatArgs);

            public void Error(string message) => _logger.LogError(message);

            public void Error(string format, params object[] formatArgs) => _logger.LogError(format, formatArgs);

            public void Error(Exception exception, string message) => _logger.LogError(exception, message);

            public global::Grpc.Core.Logging.ILogger ForType<T>()
                => new GrpcLoggerAdapter(_loggerFactory, _loggerFactory.CreateLogger<T>());

            public void Info(string message) => _logger.LogInformation(message);

            public void Info(string format, params object[] formatArgs) => _logger.LogInformation(format, formatArgs);

            public void Warning(string message) => _logger.LogWarning(message);

            public void Warning(string format, params object[] formatArgs) => _logger.LogWarning(format, formatArgs);

            public void Warning(Exception exception, string message) => _logger.LogWarning(exception, message);
        }


        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            new Startup().ConfigureServices(serviceCollection);
            var services = serviceCollection.BuildServiceProvider(true);
            var interceptor = new DependencyInjectorInterceptor(services);

            global::Grpc.Core.GrpcEnvironment.SetLogger(new GrpcLoggerAdapter(
                services.GetRequiredService<ILoggerFactory>()
            ));

            var server = new global::Grpc.Core.Server
            {
                Services = { ImageService.BindService(new ImageServiceImpl()).Intercept(interceptor) },
                Ports = { { "0.0.0.0", 5000, global::Grpc.Core.ServerCredentials.Insecure } }
            };
            server.Start();
            Console.Read();
            server.ShutdownAsync().Wait();
        }
    }
}
