namespace NCoreUtils.Images

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.Serialization
open System.Threading
open System.Threading.Tasks
open NCoreUtils

[<Serializable>]
type ImageResizerException =
  inherit Exception
  new () = { inherit Exception () }
  new (message : string) = { inherit Exception (message) }
  new (message : string, innerException : exn) = { inherit Exception (message, innerException) }
  new (info : SerializationInfo, context) = { inherit Exception (info, context) }

[<Interface>]
type ILog =
  abstract LogDebug   : exn:exn * message:string -> unit
  abstract LogWarning : exn:exn * message:string -> unit
  abstract LogError   : exn:exn * message:string -> unit

[<Interface>]
type ILog<'a> = inherit ILog

[<Interface>]
type IImageDestination =
  abstract AsyncWrite : contentInfo:ContentInfo * generator:(Stream -> Async<unit>) -> Async<unit>

[<Interface>]
type IImageResizer =
  abstract AsyncResize : source:Stream * destination:IImageDestination * options:ResizeOptions -> Async<unit>
  abstract AsyncGetImageInfo : source:Stream -> Async<ImageInfo>

[<Interface>]
type IImageOptimization =
  abstract Optimize : data:byte[] -> Async<byte[]>

// C# interop

[<AbstractClass>]
type AsyncImageDestination () =
  abstract WriteAsync : contentInfo:ContentInfo * generator:Func<Stream, CancellationToken, Task> * cancellationToken:CancellationToken -> Task
  member inline internal this.WriteDirect (contentInfo, generator, cancellationToken) = this.WriteAsync (contentInfo, generator, cancellationToken)
  interface IImageDestination with
    member this.AsyncWrite (contentInfo, generator) =
      let gen = Func<Stream, CancellationToken, Task> (fun stream cancellationToken -> Async.StartAsTask (generator stream, cancellationToken = cancellationToken) :> Task)
      Async.Adapt (fun cancellationToken -> this.WriteAsync (contentInfo, gen, cancellationToken))

[<AbstractClass>]
type AsyncImageResizer () =
  abstract ResizeAsync : source:Stream * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task
  abstract GetImageInfoAsync : source:Stream * cancellationToken:CancellationToken -> Task<ImageInfo>
  member inline internal this.ResizeDirect (source, destination, options, cancellationToken) = this.ResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.GetImageInfoDirect (source, cancellationToken) = this.GetImageInfoAsync (source, cancellationToken)
  interface IImageResizer with
    member this.AsyncResize (source, destination, options) = Async.Adapt (fun cancellationToken -> this.ResizeAsync (source, destination, options, cancellationToken))
    member this.AsyncGetImageInfo source = Async.Adapt (fun cancellationToken -> this.GetImageInfoAsync (source, cancellationToken))

[<Extension>]
[<Sealed; AbstractClass>]
type ImageDestinationExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Write (this : IImageDestination, contentInfo, generator : Action<Stream>) =
    this.AsyncWrite (contentInfo, fun stream -> async { generator.Invoke stream }) |> Async.RunSynchronously

  [<Extension>]
  static member WriteAsync (this : IImageDestination, contentInfo, generator : Func<Stream, CancellationToken, Task>, cancellationToken) =
    match this with
    | :? AsyncImageDestination as inst -> inst.WriteDirect (contentInfo, generator, cancellationToken)
    | _                           ->
      let gen stream = Async.Adapt (fun cancellationToken -> generator.Invoke (stream, cancellationToken))
      Async.StartAsTask (this.AsyncWrite (contentInfo, gen), cancellationToken = cancellationToken) :> _

[<AutoOpen>]
module ImageResizerExt =

  [<Sealed>]
  type internal StreamDestination (stream : Stream) =
    do if isNull stream then ArgumentNullException "stream" |> raise
    interface IImageDestination with
      member __.AsyncWrite (_, generator) = generator stream

  type IImageResizer with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source, destinationStream : Stream, options) = this.AsyncResize (source, StreamDestination destinationStream, options)

[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source, destination : IImageDestination, options) =
    this.AsyncResize (source, destination, options) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageResizer, source) =
    this.AsyncGetImageInfo source |> Async.RunSynchronously

  [<Extension>]
  static member ResizeAsync (this : IImageResizer, source, destination : IImageDestination, options, cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.ResizeDirect (source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResize (source, destination, options), cancellationToken = cancellationToken) :> _

  [<Extension>]
  static member GetImageInfoAsync (this : IImageResizer, source, cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.GetImageInfoDirect (source, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncGetImageInfo source, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source, destinationStream : Stream, options) =
    this.Resize (source, StreamDestination destinationStream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source, destinationStream : Stream, options, cancellationToken) =
    this.ResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)
