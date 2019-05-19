using System;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Images.Server
{
    static class ServiceProviderExtensions
    {
        public static T Activate<T>(this IServiceProvider serviceProvider)
            => ActivatorUtilities.CreateInstance<T>(serviceProvider);
    }
}