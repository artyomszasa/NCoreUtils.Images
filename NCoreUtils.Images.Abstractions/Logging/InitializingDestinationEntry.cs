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
            => ContentType = contentType ?? string.Empty;

        bool IEmplaceable<InitializingDestinationEntry>.TryGetEmplaceBufferSize(out int minimumBufferSize)
        {
            minimumBufferSize = 50 + ContentType.Length;
            return true;
        }

#if NET6_0_OR_GREATER
        string IFormattable.ToString(string? format, System.IFormatProvider? formatProvider)
            => ToString();
#else
        public int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var used))
            {
                return used;
            }
            throw new InsufficientBufferSizeException(span);
        }
#endif

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

        public override string ToString()
            => Emplacer.ToStringUsingArrayPool(this);
    }
}