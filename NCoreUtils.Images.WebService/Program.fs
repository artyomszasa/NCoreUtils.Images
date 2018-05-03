namespace NCoreUtils.Images.WebService

open Microsoft.AspNetCore.Hosting
open NCoreUtils.AspNetCore

module Program =
  let exitCode = 0

  [<EntryPoint>]
  let main args =
    WebHostBuilder()
      .UseKestrel(args)
      .UseStartup<Startup>()
      .Build()
      .Run()

    exitCode