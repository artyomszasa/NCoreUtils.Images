using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Logging
{
    public struct InitializingDestinationEntry : IEmplaceable<InitializingDestinationEntry>
    {
        public static Func<InitializingDestinationEntry, Exception?, string> Formatter { get; } =
            (entry, _) => entry.ToString();

        public string ContentType { get; }

        public InitializingDestinationEntry(string contentType)
        {
            ContentType = contentType;
        }

        public int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var used))
            {
                return used;
            }
            throw new ArgumentException("Insufficient buffer size.", nameof(span));
        }

        public bool TryEmplace(Span<char> span, out int used)
        {
            var builder = new SpanBuilder(span);
            if (builder.TryAppend("Initializing image destination with content type ")
                && builder.TryAppend(ContentType)
                && builder.TryAppend('.'))
            {
                used = builder.Length;
                return true;
            }
            used = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryToStringOnStack([NotNullWhen(true)] out string? result)
        {
            Span<char> buffer = stackalloc char[4 * 1024];
            if (TryEmplace(buffer, out var size))
            {
                result = buffer.Slice(0, size).ToString();
                return true;
            }
            result = default;
            return false;
        }

        public override string ToString()
        {
            if (TryToStringOnStack(out var result))
            {
                return result;
            }
            using var memoryBuffer = MemoryPool<char>.Shared.Rent(32 * 1024);
            var buffer = memoryBuffer.Memory.Span;
            var size = Emplace(buffer);
            return buffer.Slice(0, size).ToString();
        }
    }
}