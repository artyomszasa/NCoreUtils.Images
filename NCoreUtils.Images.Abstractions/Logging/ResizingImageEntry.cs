using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Logging
{
    public struct ResizingImageEntry : IEmplaceable<ResizingImageEntry>
    {
        public static Func<ResizingImageEntry, Exception?, string> Formatter { get; } =
            (entry, _) => entry.ToString();

        public string ImageType { get; }

        public bool IsExplicit { get; }

        public int Quality { get; }

        public bool Optimize { get; }

        public ResizingImageEntry(string imageType, bool isExplicit, int quality, bool optimize)
        {
            ImageType = imageType;
            IsExplicit = isExplicit;
            Quality = quality;
            Optimize = optimize;
        }

        bool IEmplaceable<ResizingImageEntry>.TryGetEmplaceBufferSize(out int minimumBufferSize)
        {
            minimumBufferSize = 52 + 12 + 13 + 21 + 5 + ImageType.Length;
            if (!IsExplicit)
            {
                minimumBufferSize += 11;
            }
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
            used = default;
            if (!builder.TryAppend("Resizing image with computed options [ImageType = ")) { return false; }
            if (!builder.TryAppend(ImageType)) { return false; }
            if (!IsExplicit)
            {
                if (!builder.TryAppend(" (implicit)")) { return false; }
            }
            if (builder.TryAppend(", Quality = ")
                && builder.TryAppend(Quality)
                && builder.TryAppend(", Optimize = ")
                && builder.TryAppend(Optimize)
                && builder.TryAppend("]."))
            {
                used = builder.Length;
                return true;
            }
            return false;
        }

        public override string ToString()
            => Emplacer.ToStringUsingArrayPool(this);
    }
}