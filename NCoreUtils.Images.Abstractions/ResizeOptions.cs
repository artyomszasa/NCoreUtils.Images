using System;

namespace NCoreUtils.Images
{
    public class ResizeOptions
    {
        public string? ImageType { get; }

        public int? Width { get; }

        public int? Height { get; }

        public string? ResizeMode { get; }

        public int? Quality { get; }

        public bool? Optimize { get; }

        public int? WeightX { get; }

        public int? WeightY { get; }

        public ResizeOptions(
            string? imageType = default,
            int? width = default,
            int? height = default,
            string? resizeMode = default,
            int? quality = default,
            bool? optimize = default,
            int? weightX = default,
            int? weightY = default)
        {
            ImageType = imageType;
            Width = width;
            Height = height;
            ResizeMode = resizeMode;
            Quality = quality;
            Optimize = optimize;
            WeightX = weightX;
            WeightY = weightY;
        }

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
            builder.Append(']');
            return builder.ToString();
        }
    }
}