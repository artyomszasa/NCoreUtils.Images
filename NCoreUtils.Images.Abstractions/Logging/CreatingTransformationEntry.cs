using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Logging
{
    public struct CreatingTransformationEntry : IEmplaceable<CreatingTransformationEntry>
    {
        public static Func<CreatingTransformationEntry, Exception?, string> Formatter { get; } =
            (entry, _) => entry.ToString();

        public ResizeOptions Options { get; }

        public CreatingTransformationEntry(ResizeOptions options)
            => Options = options ?? throw new ArgumentNullException(nameof(options));

        bool IEmplaceable<CreatingTransformationEntry>.TryGetEmplaceBufferSize(out int minimumBufferSize)
        {
            minimumBufferSize = 38 + Options.GetEmplaceBufferSize();
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
            throw new ArgumentException("Insufficient buffer size.", nameof(span));
        }
#endif

        public bool TryEmplace(Span<char> span, out int used)
        {
            var builder = new SpanBuilder(span);
            if (builder.TryAppend("Creating transformation with options ")
                && builder.TryAppend(Options)
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