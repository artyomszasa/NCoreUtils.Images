namespace NCoreUtils.Images.Server

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NCoreUtils
open NCoreUtils.Images

module ValueOption = FSharp.Core.ValueOption

[<Extension>]
[<Sealed; AbstractClass>]
type ServerUtilsExtensions =

  [<Extension>]
  static member TryGetImageSourceAsync (this : ServerUtils, provider : IMetadataProvider, [<Optional>] cancellationToken) =
    let computation = this.AsyncTryExtractSource provider >>| ValueOption.toObj
    Async.StartAsTask (computation, cancellationToken = cancellationToken)

  [<Extension>]
  static member TryGetImageDestinationAsync (this : ServerUtils, provider : IMetadataProvider, [<Optional>] cancellationToken) =
    let computation = this.AsyncTryExtractDestination provider >>| ValueOption.toObj
    Async.StartAsTask (computation, cancellationToken = cancellationToken)

  [<Extension>]
  static member ResizeAsync (this : ServerUtils, producer, consumer, options, applyImageType : Action<string>, [<Optional>] cancellationToken) =
    let computation = this.AsyncResize (producer, consumer, options, applyImageType.Invoke)
    Async.StartAsTask (computation, cancellationToken = cancellationToken)