using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images
{
    public sealed class DummyLogger<T> : ILogger<T>
    {
        private sealed class DummyDisposable : IDisposable
        {
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state)
            => new DummyDisposable();

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        { }
    }
}