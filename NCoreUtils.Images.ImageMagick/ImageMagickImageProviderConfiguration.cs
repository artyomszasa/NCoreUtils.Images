using System;
using ImageMagick;

namespace NCoreUtils.Images;

public class ImageMagickImageProviderConfiguration : IImageMagickImageProviderConfiguration
{
    public bool EnableLogging { get; set; } = true;

    public bool EnableResourceTracking { get; set; } = true;

    /// <inheritdoc />
    public bool ForceAsync { get; set; } = false;

    public Action<MagickReadSettings> ConfigureReadSettings { get; } = settings =>
    {
        settings.Density = new Density(600, 600);
        settings.Verbose = false;
    };
}