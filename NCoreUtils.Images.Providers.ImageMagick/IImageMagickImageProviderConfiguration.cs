using System;
using ImageMagick;

namespace NCoreUtils.Images
{
    public interface IImageMagickImageProviderConfiguration
    {
        bool EnableLogging { get; }

        bool EnableResourceTracking { get; }

        /// <summary>
        /// By default images are preferrably created synchronously as it fits most of the cases. In some rear cases
        /// (e.g. large PDF files passed using pipes) images reader (i.e. consumer) may block the producer thread
        /// indefinitely. Setting this option to <c>true</c> forces the consumer to use thread pool resolving the issue.
        /// </summary>
        bool ForceAsync { get; }

        Action<MagickReadSettings> ConfigureReadSettings { get; }
    }
}