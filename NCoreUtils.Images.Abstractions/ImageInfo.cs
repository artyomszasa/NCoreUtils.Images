using System;
using System.Collections.Generic;

namespace NCoreUtils.Images;

/// <summary>
/// Represents information returned by <see cref="IImageAnalyzer" />.
/// </summary>
public class ImageInfo(
    int width,
    int height,
    int xResolution,
    int yResolution,
    IReadOnlyDictionary<string, string> iptc,
    IReadOnlyDictionary<string, string> exif)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    public int XResolution { get; } = xResolution;

    public int YResolution { get; } = yResolution;

    public IReadOnlyDictionary<string, string> Iptc { get; } = iptc ?? throw new ArgumentNullException(nameof(iptc));

    public IReadOnlyDictionary<string, string> Exif { get; } = exif ?? throw new ArgumentNullException(nameof(exif));
}