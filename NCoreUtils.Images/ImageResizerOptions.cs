using System.Collections.Generic;

namespace NCoreUtils.Images;

public class ImageResizerOptions : IImageResizerOptions
{
    public static IImageResizerOptions Default { get; } = new ImageResizerOptions();

    public long? MemoryLimit { get; set; }

    public Dictionary<string, int> Quality { get; } = new();

    public Dictionary<string, bool> Optimize { get; } = new();

    int IImageResizerOptions.Quality(string imageType)
        => Quality.TryGetValue(imageType, out var i) || Quality.TryGetValue("Default", out i)
            ? i
            : 85;

    bool IImageResizerOptions.Optimize(string imageType)
        => (Optimize.TryGetValue(imageType, out var b) || Optimize.TryGetValue("Default", out b)) && b;
}