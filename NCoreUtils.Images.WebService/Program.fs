namespace NCoreUtils.Images.WebService

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open NCoreUtils
open System
open System.Net

module Program =
  let exitCode = 0

  [<CompiledName("ParseEndpoint")>]
  let private parseEndpoint input =
    match input with
    | null | "" -> IPEndPoint (IPAddress.Loopback, 5000) // DEFAULT HOST+PORT
    | _ ->
      match input.LastIndexOf ':' with
      | -1        -> IPEndPoint (IPAddress.Parse input, 5000)
      | portIndex ->
        IPEndPoint (
          IPAddress.Parse (input.Substring (0, portIndex)),
          Int32.Parse (input.Substring (portIndex + 1))
        )

  [<CompiledName("ConfigureKestrel")>]
  let private configureKestrel (options : KestrelServerOptions) =
    options.Limits.MaxResponseBufferSize <- Nullable 0L
    options.Limits.MaxRequestBodySize <- Nullable ()
    options.AllowSynchronousIO <- true
    match Environment.GetEnvironmentVariable "PORT" |> ValueOption.ofObj >>= tryInt32Value with
    | ValueSome port ->
      options.Listen (IPEndPoint (IPAddress.Any, port))
    | _ ->
      let endpoint = parseEndpoint <| Environment.GetEnvironmentVariable "ASPNETCORE_LISTEN_AT"
      options.Listen endpoint

  let CreateWebHostBuilder (_args : string[]) =
    WebHostBuilder()
      .UseContentRoot(IO.Directory.GetCurrentDirectory ())
      .UseKestrel(configureKestrel)
      .UseStartup<Startup>()

  [<EntryPoint>]
  let main args =
    CreateWebHostBuilder(args)
      .Build()
      .Run()

    exitCode