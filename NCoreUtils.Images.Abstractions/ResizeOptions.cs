using System;
using System.Collections.Generic;
using NCoreUtils.Images.Internal;
using NCoreUtils.Memory;

namespace NCoreUtils.Images;

public class ResizeOptions : ISpanExactEmplaceable
{
    public static IEmplacer<ResizeOptions> Emplacer { get; } = new SpanEmplaceableEmplacer<ResizeOptions>();

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
        Filters = filters ?? Array.Empty<IFilter>();
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
        : this(imageType, width, height, resizeMode, quality, optimize, weightX, weightY, (IReadOnlyList<IFilter>)(filters.Length == 0 ? Array.Empty<IFilter>() : filters))
    { }

    internal int GetEmplaceBufferSize()
    {
        var size = 2;
        if (!string.IsNullOrEmpty(ImageType))
        {
            size += 2 + 9 + 3 + ImageType.Length;
        }
        if (Width.HasValue)
        {
            size += 2 + 5 + 3 + 21;
        }
        if (Height.HasValue)
        {
            size += 2 + 6 + 3 + 21;
        }
        if (!string.IsNullOrEmpty(ResizeMode))
        {
            size += 2 + 10 + 3 + ResizeMode.Length;
        }
        if (Quality.HasValue)
        {
            size += 2 + 7 + 3 + 21;
        }
        if (Optimize.HasValue)
        {
            size += 2 + 8 + 3 + 5;
        }
        if (WeightX.HasValue)
        {
            size += 2 + 7 + 3 + 21;
        }
        if (WeightY.HasValue)
        {
            size += 2 + 7 + 3 + 21;
        }
        foreach (var filter in Filters)
        {
            size += 2 + 6 + 3 + filter.GetEmplaceBufferSize();
        }
        return size;
    }

    int ISpanExactEmplaceable.GetEmplaceBufferSize()
        => GetEmplaceBufferSize();

    bool ISpanEmplaceable.TryGetEmplaceBufferSize(out int minimumBufferSize)
    {
        minimumBufferSize = GetEmplaceBufferSize();
        return true;
    }


#if NET6_0_OR_GREATER
    string IFormattable.ToString(string? format, System.IFormatProvider? formatProvider)
        => ToString();
#else
    public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
        => TryEmplace(destination, out charsWritten);

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

    public override string ToString()
        => NCoreUtils.Emplacer.ToStringUsingArrayPool(this);
}