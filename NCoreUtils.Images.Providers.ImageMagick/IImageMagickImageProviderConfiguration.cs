using System;
using ImageMagick;

namespace NCoreUtils.Images
{
    public interface IImageMagickImageProviderConfiguration
    {
        bool EnableLogging { get; }

        bool EnableResourceTracking { get; }

        Action<MagickReadSettings> ConfigureReadSettings { get; }
    }
}