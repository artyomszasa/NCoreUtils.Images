using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Providers;

namespace NCoreUtils.Images.Server
{
    public class GrpcStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(b => b.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddImageResizer<ImageMagickImageProvider>()
                .AddScoped<ServerUtils>();
        }
    }
}