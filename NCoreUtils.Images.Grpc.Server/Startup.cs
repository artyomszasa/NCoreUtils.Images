using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Providers;

namespace NCoreUtils.Images.Grpc
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(b => b.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddImageResizer<ImageMagickImageProvider>();
        }
    }
}