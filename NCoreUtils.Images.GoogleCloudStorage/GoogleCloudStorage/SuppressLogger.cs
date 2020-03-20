using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    internal class SuppressLogger<T> : ILogger<T>
    {
        private sealed class DummyDisposable : IDisposable
        {
            public static DummyDisposable Instance { get; } = new DummyDisposable();

            public void Dispose() { }
        }

        public static SuppressLogger<T> Instance { get; } = new SuppressLogger<T>();

        public IDisposable BeginScope<TState>(TState state)
            => DummyDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}