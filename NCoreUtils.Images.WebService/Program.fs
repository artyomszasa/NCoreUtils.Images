namespace NCoreUtils.Images.WebService

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open System

module Program =
  let exitCode = 0

  let private configureKestrel (options : KestrelServerOptions) =
    options.Limits.MaxResponseBufferSize <- Nullable 8192L
    ()

  [<EntryPoint>]
  let main args =
    WebHostBuilder()
      .UseKestrel(configureKestrel)
      .UseStartup<Startup>()
      .Build()
      .Run()

    exitCode