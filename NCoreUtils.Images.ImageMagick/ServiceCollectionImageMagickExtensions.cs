using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NCoreUtils.Images.ImageMagick;

namespace NCoreUtils.Images
{
    public static class ServiceCollectionImageMagickExtensions
    {
        public static IServiceCollection AddImageMagickResizer(
            this IServiceCollection services,
            bool suppressDefaultResizers = false,
            Action<ResizerCollectionBuilder>? configure = default)
            => services
                .AddOptions<ImageMagickImageProviderConfiguration>()
                    .Services
                .AddTransient<IImageMagickImageProviderConfiguration>(
                    serviceProvider => serviceProvider
                        .GetRequiredService<IOptionsMonitor<ImageMagickImageProviderConfiguration>>()
                        .CurrentValue
                )
                .AddImageResizer<ImageProvider>(suppressDefaultResizers, configure);
    }
}