using System;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Images.ImageMagick;

namespace NCoreUtils.Images
{
    public static class ServiceCollectionImageMagickExtensions
    {
        public static IServiceCollection AddImageMagickResizer(
            this IServiceCollection services,
            bool suppressDefaultResizers = false,
            Action<ResizerCollectionBuilder>? configure = default)
            => services.AddImageResizer<ImageProvider>(suppressDefaultResizers, configure);
    }
}