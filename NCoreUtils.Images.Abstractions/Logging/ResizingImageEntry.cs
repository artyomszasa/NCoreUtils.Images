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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryToStringOnStack([NotNullWhen(true)] out string? result)
        {
            Span<char> buffer = stackalloc char[4 * 1024];
            if (TryEmplace(buffer, out var size))
            {
                result = buffer[..size].ToString();
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
            return buffer[..size].ToString();
        }
    }
}