namespace NCoreUtils.Images

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils

// EXTENSIONS

[<Extension>]
[<Sealed; AbstractClass>]
type ImageDestinationExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Write (this : IImageDestination, contentInfo : ContentInfo, generator : Action<Stream>) =
    this.AsyncWrite (contentInfo, fun stream -> async { generator.Invoke stream }) |> Async.RunSynchronously

  // [<Extension>]
  // static member WriteAsync (this : IImageDestination, contentInfo, generator : Func<Stream, CancellationToken, Task>, cancellationToken) =
  //   match this with
  //   | :? AsyncImageDestination as inst -> inst.WriteDirect (contentInfo, generator, cancellationToken)
  //   | _                           ->
  //     let gen stream = Async.Adapt (fun cancellationToken -> generator.Invoke (stream, cancellationToken))
  //     Async.StartAsTask (this.AsyncWrite (contentInfo, gen), cancellationToken = cancellationToken) :> _

[<AutoOpen>]
module ImageResizerExt =

  [<Sealed>]
  type internal StreamDestination (stream : Stream) =
    do if isNull stream then ArgumentNullException "stream" |> raise
    interface IImageDestination with
      member __.AsyncWrite (_, generator) = generator stream
      member __.AsyncWriteDelayed generator = generator ignore stream


  type IImageResizer with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Stream, destinationStream : Stream, options) = this.AsyncResize (source, StreamDestination destinationStream, options)

[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destination : IImageDestination, options) =
    this.AsyncResize (source, destination, options) |> Async.RunSynchronously

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member Resize (this : IImageResizer, source : string, destination : IImageDestination, options) =
  //   this.AsyncResize (source, destination, options) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destinationStream : Stream, options) =
    this.Resize (source, StreamDestination destinationStream, options)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member Resize (this : IImageResizer, source : string, destinationStream : Stream, options) =
  //   this.Resize (source, StreamDestination destinationStream, options)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResize (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Out>] error : byref<_>) =
  //   match this.AsyncResResize (source, destination, options) |> Async.RunSynchronously with
  //   | Ok () ->
  //     error <- Unchecked.defaultof<_>
  //     true
  //   | Error err ->
  //     error <- err
  //     false

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResize (this : IImageResizer, source : string, destination : IImageDestination, options, [<Out>] error : byref<_>) =
  //   match this.AsyncResResize (source, destination, options) |> Async.RunSynchronously with
  //   | Ok () ->
  //     error <- Unchecked.defaultof<_>
  //     true
  //   | Error err ->
  //     error <- err
  //     false

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResize (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Out>] error : byref<_>) =
  //   this.TryResize (source, StreamDestination destinationStream, options, &error)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResize (this : IImageResizer, source : string, destinationStream : Stream, options, [<Out>] error : byref<_>) =
  //   this.TryResize (source, StreamDestination destinationStream, options, &error)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageResizer, source) =
    this.AsyncGetImageInfo source |> Async.RunSynchronously

  // [<Extension>]
  // static member ResizeAsync (this : IImageResizer, source : string, destination : IImageDestination, options, [<Optional>] cancellationToken) =
  //   match this with
  //   | :? AsyncImageResizer as inst -> inst.ResizeDirect (source, destination, options, cancellationToken)
  //   | _                            -> Async.StartAsTask (this.AsyncResize (source, destination, options), cancellationToken = cancellationToken) :> _

  // [<Extension>]
  // static member ResizeAsync (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Optional>] cancellationToken) =
  //   match this with
  //   | :? AsyncImageResizer as inst -> inst.ResizeDirect (source, destination, options, cancellationToken)
  //   | _                            -> Async.StartAsTask (this.AsyncResize (source, destination, options), cancellationToken = cancellationToken) :> _

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member ResizeAsync (this : IImageResizer, source : string, destinationStream : Stream, options, [<Optional>] cancellationToken) =
  //   this.ResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member ResizeAsync (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Optional>] cancellationToken) =
  //   this.ResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResizeAsync (this : IImageResizer, source : string, destination : IImageDestination, options, [<Optional>] cancellationToken) =
  //   match this with
  //   | :? AsyncImageResizer as inst -> inst.TryResizeDirect (source, destination, options, cancellationToken)
  //   | _                            -> Async.StartAsTask (this.AsyncResResize (source, destination, options) >>| AsyncResult.FromResult, cancellationToken = cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResizeAsync (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Optional>] cancellationToken) =
  //   match this with
  //   | :? AsyncImageResizer as inst -> inst.TryResizeDirect (source, destination, options, cancellationToken)
  //   | _                            -> Async.StartAsTask (this.AsyncResResize (source, destination, options) >>| AsyncResult.FromResult, cancellationToken = cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResizeAsync (this : IImageResizer, source : string, destinationStream : Stream, options, [<Optional>] cancellationToken) =
  //   this.TryResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryResizeAsync (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Optional>] cancellationToken) =
  //   this.TryResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  // [<Extension>]
  // static member GetImageInfoAsync (this : IImageResizer, source, [<Optional>] cancellationToken) =
  //   match this with
  //   | :? AsyncImageResizer as inst -> inst.GetImageInfoDirect (source, cancellationToken)
  //   | _                            -> Async.StartAsTask (this.AsyncGetImageInfo source, cancellationToken = cancellationToken)

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member Optimize (this : IImageOptimization, data : byte[]) =
  //   this.AsyncOptimize data |> Async.RunSynchronously

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryOptimize (this : IImageOptimization, data : byte[], [<Out>] result : byref<byte[]>, [<Out>] error : byref<_>) =
  //   match this.AsyncResOptimize data |> Async.RunSynchronously with
  //   | Ok res ->
  //     result <- res
  //     error  <- Unchecked.defaultof<_>
  //     true
  //   | Error err ->
  //     result <- Unchecked.defaultof<_>
  //     error  <- err
  //     false

  // [<Extension>]
  // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  // static member TryOptimize (this : IImageOptimization, data : byte[], [<Out>] result : byref<byte[]>) =
  //   let mutable ignored = Unchecked.defaultof<_>
  //   this.TryOptimize (data, &result, &ignored)

  // [<Extension>]
  // static member OptimizeAsync (this : IImageOptimization, data : byte[], cancellationToken : CancellationToken) =
  //   match this with
  //   | :? AsyncImageOptimization as inst -> inst.OptimizeDirect (data, cancellationToken)
  //   | _                                 -> Async.StartAsTask (this.AsyncOptimize data, cancellationToken = cancellationToken)

  // [<Extension>]
  // static member TryOptimizeAsync (this : IImageOptimization, data : byte[], cancellationToken : CancellationToken) =
  //   match this with
  //   | :? AsyncImageOptimization as inst -> inst.TryOptimizeDirect (data, cancellationToken)
  //   | _                                 -> Async.StartAsTask (this.AsyncResOptimize data >>| AsyncResult<_, _>.FromResult, cancellationToken = cancellationToken)