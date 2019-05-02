namespace NCoreUtils.Images.WebService

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open System

module Program =
  let exitCode = 0

  let private configureKestrel (options : KestrelServerOptions) =
    options.Limits.MaxResponseBufferSize <- Nullable 0L
    options.Limits.MaxRequestBodySize <- Nullable ()
    // options.Limits.MaxRequestBufferSize  <- Nullable 32768L
    ()

  [<EntryPoint>]
  let main args =
    WebHostBuilder()
      .UseKestrel(configureKestrel)
      .UseStartup<Startup>()
      .Build()
      .Run()

    exitCode