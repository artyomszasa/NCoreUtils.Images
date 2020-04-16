using System;
using System.Collections.Generic;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images
{
    public class ResizeOptions
    {
        static readonly IReadOnlyList<IFilter> _noFilters = new IFilter[0];

        public string? ImageType { get; }

        public int? Width { get; }

        public int? Height { get; }

        public string? ResizeMode { get; }

        public int? Quality { get; }

        public bool? Optimize { get; }

        public int? WeightX { get; }

        public int? WeightY { get; }

        /// <summary>
        /// Gets output filters to apply (if any).
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
            : this(imageType, width, height, resizeMode, quality, optimize, weightX, weightY, filters.Length == 0 ? _noFilters : (IReadOnlyList<IFilter>)filters)
        { }

        public override string ToString()
        {
            Span<char> buffer = stackalloc char[8192];
            var builder = new SpanBuilder(buffer);
            var first = true;
            builder.Append('[');
            builder.AppendOption(ref first, nameof(ImageType), ImageType);
            builder.AppendOption(ref first, nameof(Width), Width);
            builder.AppendOption(ref first, nameof(Height), Height);
            builder.AppendOption(ref first, nameof(ResizeMode), ResizeMode);
            builder.AppendOption(ref first, nameof(Quality), Quality);
            builder.AppendOption(ref first, nameof(Optimize), Optimize);
            builder.AppendOption(ref first, nameof(WeightX), WeightX);
            builder.AppendOption(ref first, nameof(WeightY), WeightY);
            if (Filters.Count > 0)
            {
                for (var i = 0; i < Filters.Count; ++i)
                {
                    builder.AppendOption(ref first, "Filter", Filters[i]);
                }
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}