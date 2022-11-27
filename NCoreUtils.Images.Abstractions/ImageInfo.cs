using System;
using System.Collections.Generic;

namespace NCoreUtils.Images;

/// <summary>
/// Represents information returned by <see cref="IImageAnalyzer" />.
/// </summary>
public class ImageInfo
{
    public int Width { get; }

    public int Height { get; }

    public int XResolution { get; }

    public int YResolution { get; }

    public IReadOnlyDictionary<string, string> Iptc { get; }

    public IReadOnlyDictionary<string, string> Exif { get; }

    public ImageInfo(int width, int height, int xResolution, int yResolution, IReadOnlyDictionary<string, string> iptc, IReadOnlyDictionary<string, string> exif)
    {
        Width = width;
        Height = height;
        XResolution = xResolution;
        YResolution = yResolution;
        Iptc = iptc ?? throw new ArgumentNullException(nameof(iptc));
        Exif = exif ?? throw new ArgumentNullException(nameof(exif));
    }
}