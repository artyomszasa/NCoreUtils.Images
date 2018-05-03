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

type Startup () =

  member __.ConfigureServices (services: IServiceCollection) =
    let configuration = Config.buildConfig ()
    let imageResizerOptions = configuration.GetSection("Images").Get<ImageResizerOptions>()
    if box imageResizerOptions |> isNull |> not then
      services.AddSingleton<IImageResizerOptions> imageResizerOptions |> ignore
    services
      .AddSingleton<IConfiguration>(configuration)
      .AddLogging(fun b -> b.ClearProviders().SetMinimumLevel(LogLevel.Trace) |> ignore)
      .AddSingleton(JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ()))
      .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
      .AddImageMagickResizer()
      |> ignore

  member __.Configure (app: IApplicationBuilder, env: IHostingEnvironment, loggerFactory : ILoggerFactory, configuration : IConfiguration, httpContextAccessor : IHttpContextAccessor) =
    match env.IsDevelopment () with
    | true -> loggerFactory.AddConsole () |> ignore
    | _    -> loggerFactory.AddGoogleSink (httpContextAccessor, configuration.GetSection "Google") |> ignore
    app.Use Middleware.run |> ignore