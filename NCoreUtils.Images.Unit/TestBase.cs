using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images
{
    public abstract class TestBase : IDisposable
    {
        protected readonly ServiceProvider _serviceProvider;

        protected TestBase()
        {
            _serviceProvider = new ServiceCollection()
                .AddImageResizer<DebugImageProvider>()
                .AddTransient(typeof(ILogger<>), typeof(DummyLogger<>))
                .BuildServiceProvider();
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            _serviceProvider.Dispose();
        }
    }
}