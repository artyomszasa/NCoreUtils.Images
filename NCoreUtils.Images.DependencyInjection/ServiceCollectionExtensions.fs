namespace NCoreUtils.Images

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.Runtime.CompilerServices

[<Sealed>]
type internal LoggerAdapter<'T> (logger : ILogger<'T>) =
  interface ILog<'T> with
    member __.LogDebug   (exn, message) = logger.LogDebug (exn, message)
    member __.LogWarning (exn, message) = logger.LogWarning (exn, message)
    member __.LogError   (exn, message) = logger.LogError (exn, message)

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionNCoreUtilsImagesExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddImageResizer<'TResizer when 'TResizer : not struct and 'TResizer :> IImageResizer>(services : IServiceCollection) =
    services
      .AddTransient(typedefof<ILog<_>>, typedefof<LoggerAdapter<_>>)
      .AddTransient<IImageResizer, 'TResizer>()