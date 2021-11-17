using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Images.Internal;
using NCoreUtils.Memory;

namespace NCoreUtils.Images
{
    public class ResizeOptions : IEmplaceable<ResizeOptions>
    {
        private static readonly IReadOnlyList<IFilter> _noFilters = Array.Empty<IFilter>();

        /// <summary>
        /// Desired output image type. Defaults to the input image type when not set.
        /// </summary>
        public string? ImageType { get; }

        /// <summary>
        /// Desired output image width. Defaults to the input image width when not set.
        /// </summary>
        public int? Width { get; }

        /// <summary>
        /// Desired output image height. Defaults to the input image height when not set.
        /// </summary>
        public int? Height { get; }

        /// <summary>
        /// Defines resizing mode. Defaults to <c>none</c>.
        /// </summary>
        public string? ResizeMode { get; }

        /// <summary>
        /// Desired quality of the output image. Server dependent default value is used when not set.
        /// </summary>
        public int? Quality { get; }

        /// <summary>
        /// Whether to perform any optimization on the output image. Server dependent default value is used when not
        /// set.
        /// </summary>
        public bool? Optimize { get; }

        /// <summary>
        /// Optional X coordinate of the weight point of the image.
        /// </summary>
        public int? WeightX { get; }

        /// <summary>
        /// Optional Y coordinate of the weight point of the image.
        /// </summary>
        public int? WeightY { get; }

        /// <summary>
        /// Filters to apply on the output image (if any).
        /// </summary>
        public IReadOnlyList<IFilter> Filters { get; }

        public ResizeOptions(
            string? imageType = default,
            int? width = default,
            int? height = default,
            string? resizeMode = default,
            int? quality = default,
            bool? optimize = default,
            int? weightX = default,
            int? weightY = default,
            IReadOnlyList<IFilter>? filters = default)
        {
            ImageType = imageType;
            Width = width;
            Height = height;
            ResizeMode = resizeMode;
            Quality = quality;
            Optimize = optimize;
            WeightX = weightX;
            WeightY = weightY;
            Filters = filters ?? _noFilters;
        }

        public ResizeOptions(
            string? imageType = default,
            int? width = default,
            int? height = default,
            string? resizeMode = default,
            int? quality = default,
            bool? optimize = default,
            int? weightX = default,
            int? weightY = default,
            params IFilter[] filters)
            : this(imageType, width, height, resizeMode, quality, optimize, weightX, weightY, filters.Length == 0 ? _noFilters : filters)
        { }

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
            var first = true;
            if (builder.TryAppend('[')
                && builder.TryAppendOption(ref first, nameof(ImageType), ImageType)
                && builder.TryAppendOption(ref first, nameof(Width), Width)
                && builder.TryAppendOption(ref first, nameof(Height), Height)
                && builder.TryAppendOption(ref first, nameof(ResizeMode), ResizeMode)
                && builder.TryAppendOption(ref first, nameof(Quality), Quality)
                && builder.TryAppendOption(ref first, nameof(Optimize), Optimize)
                && builder.TryAppendOption(ref first, nameof(WeightX), WeightX)
                && builder.TryAppendOption(ref first, nameof(WeightY), WeightY)
                && builder.TryAppendFilters(ref first, Filters)
                && builder.TryAppend(']'))
            {
                used = builder.Length;
                return true;
            }
            used = default;
            return false;
        }
    }
}