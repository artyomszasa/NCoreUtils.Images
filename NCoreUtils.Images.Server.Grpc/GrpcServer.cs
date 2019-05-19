using System;
using System.Linq;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Proto;

namespace NCoreUtils.Images.Server
{
    public class GrpcServer
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

        readonly IServiceProvider _serviceProvider;

        public Grpc.Core.Server Server { get; private set; }

        public Grpc.Core.ServerPort ServerPort => Server.Ports.FirstOrDefault();

        public GrpcServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            GrpcEnvironment.SetLogger(new GrpcLoggerAdapter(
                serviceProvider.GetRequiredService<ILoggerFactory>()
            ));
        }

        public void Start(string host = "localhost", int port = 5000)
        {
            var diInterceptor = new DependencyInjectorInterceptor(_serviceProvider);
            var loggerInterceptor = new LoggerInterceptor(_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<GrpcServer>());
            this.Server = new Grpc.Core.Server
            {
                Services = { ImageService.BindService(new ImageServiceImpl()).Intercept(diInterceptor, loggerInterceptor) },
                Ports = { { host, port, ServerCredentials.Insecure } }
            };
            this.Server.Start();
            GrpcEnvironment.Logger.Info($"Started gRPC server at {this.Server.Ports.Single().Host}:{this.Server.Ports.Single().BoundPort}.");
        }

        public void Stop()
        {
            this.Server.ShutdownAsync().Wait();
        }
    }
}