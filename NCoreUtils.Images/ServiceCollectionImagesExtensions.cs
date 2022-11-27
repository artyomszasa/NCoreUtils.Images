using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images;

public static class ServiceCollectionImagesExtensions
{
    public static IServiceCollection AddImageResizer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime,
        bool suppressDefaultResizers = false,
        Action<ResizerCollectionBuilder>? configure = default)
        where TProvider : class, IImageProvider
    {
        var builder = new ResizerCollectionBuilder();
        if (!suppressDefaultResizers)
        {
            builder
                .Add(ResizeModes.None, new NoneResizerFactory())
                .Add(ResizeModes.Exact, ExactResizerFactory.Instance)
                .Add(ResizeModes.Inbox, InboxResizerFactory.Instance);
        }
        configure?.Invoke(builder);
        services
            .AddSingleton(builder.Build())
            .AddSingleton<IImageProvider, TProvider>();
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                services
                    .AddTransient<ImageResizer>()
                    .AddTransient<IImageResizer, ImageResizer>()
                    .AddTransient<IImageAnalyzer, ImageResizer>();
                break;
            case ServiceLifetime.Scoped:
                services
                    .AddScoped<ImageResizer>()
                    .AddScoped<IImageResizer>(serviceProvider => serviceProvider.GetRequiredService<ImageResizer>())
                    .AddScoped<IImageAnalyzer>(serviceProvider => serviceProvider.GetRequiredService<ImageResizer>());
                break;
            case ServiceLifetime.Singleton:
                services
                    .AddSingleton<ImageResizer>()
                    .AddSingleton<IImageResizer>(serviceProvider => serviceProvider.GetRequiredService<ImageResizer>())
                    .AddSingleton<IImageAnalyzer>(serviceProvider => serviceProvider.GetRequiredService<ImageResizer>());
                break;
        }
        return services;
    }

    public static IServiceCollection AddImageResizer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services,
        bool suppressDefaultResizers = false,
        Action<ResizerCollectionBuilder>? configure = default)
        where TProvider : class, IImageProvider
        => services.AddImageResizer<TProvider>(ServiceLifetime.Singleton, suppressDefaultResizers, configure);
}