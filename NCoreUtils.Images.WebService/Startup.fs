namespace NCoreUtils.Images.WebService

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Logging
open NCoreUtils.Images
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.Threading
open NCoreUtils.Images.Optimization

type Startup () =

  static let mutable counter = 0L

  static let forceGC (httpContext : HttpContext) (asyncNext : Async<unit>) = async {
    do! asyncNext
    let _count = Interlocked.Increment (&counter)
    // if 0L = count % 32L then
    let logger = httpContext.RequestServices.GetRequiredService<ILogger<Startup>> ()
    logger.LogInformation (sprintf "Forced garbage collection started (GC memory = %d bytes)" (System.GC.GetTotalMemory false))
    System.Runtime.GCSettings.LargeObjectHeapCompactionMode <- System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
    System.GC.Collect ()
    System.GC.WaitForPendingFinalizers ()
    System.GC.Collect ()
    System.GC.WaitForPendingFinalizers ()
    logger.LogInformation (sprintf "Forced garbage collection ended (GC memory = %d bytes)" (System.GC.GetTotalMemory false)) }

  member __.ConfigureServices (services: IServiceCollection) =
    let configuration = Config.buildConfig ()
    let imageResizerOptions = configuration.GetSection("Images").Get<ImageResizerOptions>()
    let serviceConfiguration = configuration.GetSection("Images").Get<ServiceConfiguration>()
    if box imageResizerOptions |> isNull |> not
      then
        services.AddSingleton<IImageResizerOptions> imageResizerOptions |> ignore
        services.AddSingleton<ImageResizerOptions> imageResizerOptions |> ignore
      else
        services.AddSingleton<IImageResizerOptions> ImageResizerOptions.Default |> ignore
        services.AddSingleton<ImageResizerOptions>  ImageResizerOptions.Default |> ignore
    if box serviceConfiguration |> isNull |> not
      then
        services.AddSingleton<ServiceConfiguration> serviceConfiguration |> ignore
      else
        services.AddSingleton<ServiceConfiguration> (ServiceConfiguration (MaxConcurrentOps = 20)) |> ignore
    services
      .AddSingleton<IConfiguration>(configuration)
      .AddLogging(fun b -> b.ClearProviders().SetMinimumLevel(LogLevel.Information) |> ignore)
      .AddPrePopulatedLoggingContext()
      .AddSingleton(JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ()))
      .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
      .AddSingletonImageOptimization<JpegoptimOptimization>()
      .AddImageMagickResizer()
      |> ignore

  member __.Configure (app: IApplicationBuilder, env: IHostingEnvironment, loggerFactory : ILoggerFactory, configuration : IConfiguration, httpContextAccessor : IHttpContextAccessor, serviceConfiguration : ServiceConfiguration) =
    match env.IsDevelopment () with
    | true -> loggerFactory.AddConsole (LogLevel.Trace) |> ignore
    | _    -> loggerFactory.AddGoogleSink (httpContextAccessor, configuration.GetSection "Google") |> ignore
    let semaphore = new SemaphoreSlim (serviceConfiguration.MaxConcurrentOps, serviceConfiguration.MaxConcurrentOps)

    ImageMagick.MagickNET.SetLogEvents ImageMagick.LogEvents.None
    // ImageMagick.MagickNET.Log.AddHandler (fun _ e -> printfn "%s" e.Message)

    app
      .UsePrePopulateLoggingContext()
      .Use(GCMiddleware.run)
      .Use(Middleware.run semaphore) |> ignore