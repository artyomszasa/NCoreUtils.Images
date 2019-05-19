using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Images.Server;
using Xunit;

namespace NCoreUtils.Images.Unit
{
    public class GrpcTests : IDisposable
    {
        readonly GrpcServer _server;

        int _isDisposed;

        public GrpcTests()
        {
            var serviceCollection = new ServiceCollection();
            new GrpcStartup().ConfigureServices(serviceCollection);
            // serviceCollection.RemoveAll(typeof(IImageResizer));
            // serviceCollection.AddSingleton<IImageResizer, DummyResizer>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _server = new GrpcServer(serviceProvider);
            _server.Start("localhost", Grpc.Core.ServerPort.PickUnused);
        }

        void IDisposable.Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                _server.Stop();
            }
        }

        protected IImageResizer CreateClient()
        {
            return new GrpcImageResizerClient(new GrpcImageResizerClientConfiguration
            {
                Host = _server.ServerPort.Host,
                Port = _server.ServerPort.BoundPort
            });
        }

        [Fact]
        public async Task TransformationTest()
        {
            var client = CreateClient();
            var data = Resources.Get10MbImage();
            var source = new ArrayImageSource(data);
            for (var i = 0; i < 10; ++i)
            {
                var buffer = new MemoryStream();
                var dest = new MemoryStreamImageDestination(buffer);
                await client.ResizeAsync(source, dest, new ResizeOptions(
                    imageType: null,
                    width: 1920,
                    height: null,
                    resizeMode: "exact",
                    quality: null,
                    optimize: null,
                    weightX: null,
                    weightY: null
                ));
                // Assert.True(Enumerable.SequenceEqual(data, buffer.ToArray()));
            }
        }
    }
}