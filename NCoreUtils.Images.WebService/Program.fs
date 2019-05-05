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

  let CreateWebHostBuilder (_args : string[]) =
    WebHostBuilder()
      .UseKestrel(configureKestrel)
      .UseStartup<Startup>()

  [<EntryPoint>]
  let main args =
    CreateWebHostBuilder(args)
      .Build()
      .Run()

    exitCode