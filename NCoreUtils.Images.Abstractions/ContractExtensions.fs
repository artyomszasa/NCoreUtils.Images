namespace NCoreUtils.Images

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.IO

module private F =

  [<Literal>]
  let private BufferSize = 16384

  [<CompiledName("Read")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let read path = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous)

  [<CompiledName("Write")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let write path = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous)

/// Contains F# specific image resizer extensions.
[<AutoOpen>]
module ImageResizerExt =

  type internal ConsumerDestination (consumer : IStreamConsumer) =
    do if isNull (box consumer) then ArgumentNullException (nameof consumer) |> raise
    interface IImageDestination with
      member __.CreateConsumer _ = consumer

  [<Sealed>]
  type internal StreamDestination =
    inherit ConsumerDestination
    new (stream : Stream) =
      do if isNull stream then ArgumentNullException (nameof stream) |> raise
      let consumer =
        { new IStreamConsumer with
            member __.AsyncConsume input = Stream.asyncCopyToWithBufferSize 8192 stream input
            member __.Dispose () = ()
        }
      { inherit ConsumerDestination (consumer) }

  type internal ProducerSource (producer : IStreamProducer) =
    do if isNull (box producer) then ArgumentNullException (nameof producer) |> raise
    interface IImageSource with
      member __.CreateProducer () = producer

  let private producerOfStream stream =
    { new IStreamProducer with
        member __.AsyncProduce output = Stream.asyncCopyTo output stream
        member __.Dispose () = stream.Dispose ()
    }

  [<Sealed>]
  type internal StreamSource =
    inherit ProducerSource
    new (stream : Stream) =
      do if isNull stream then ArgumentNullException (nameof stream) |> raise
      let producer =
        { new IStreamProducer with
            member __.AsyncProduce output = Stream.asyncCopyTo output stream
            member __.Dispose () = stream.Dispose ()
        }
      { inherit ProducerSource (producer) }

  type IImageResizer with

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : IImageSource, destination : IStreamConsumer, options) =
      this.AsyncResize (source, ConsumerDestination destination, options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : IImageSource, destination : Stream, options) =
      this.AsyncResize (source, StreamDestination destination, options)

    member this.AsyncResize (source : IImageSource, destinationPath : string, options) = async {
      use stream = F.write destinationPath
      do! this.AsyncResize (source, stream, options) }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : IStreamProducer, destination : IImageDestination, options) =
      this.AsyncResize (ProducerSource source, destination, options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : IStreamProducer, destination : IStreamConsumer, options) =
      this.AsyncResize (ProducerSource source, ConsumerDestination destination, options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : IStreamProducer, destination : Stream, options) =
      this.AsyncResize (ProducerSource source, StreamDestination destination, options)

    member this.AsyncResize (source : IStreamProducer, destinationPath : string, options) = async {
      use stream = F.write destinationPath
      do! this.AsyncResize (source, stream, options) }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Stream, destination : IImageDestination, options) =
      this.AsyncResize (StreamSource source, destination, options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Stream, destination : IStreamConsumer, options) =
      this.AsyncResize (StreamSource source, ConsumerDestination destination, options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Stream, destination : Stream, options) =
      this.AsyncResize (StreamSource source, StreamDestination destination, options)

    member this.AsyncResize (source : Stream, destinationPath : string, options) =async {
      use stream = F.write destinationPath
      do! this.AsyncResize (source, stream, options) }

    member this.AsyncResize (sourcePath : string, destination : IImageDestination, options) = async {
      use stream = F.read sourcePath
      do! this.AsyncResize (stream, destination, options) }

    member this.AsyncResize (sourcePath : string, destination : IStreamConsumer, options) = async {
      use stream = F.read sourcePath
      do! this.AsyncResize (stream, destination, options) }

    member this.AsyncResize (sourcePath : string, destination : Stream, options) = async {
      use stream = F.read sourcePath
      do! this.AsyncResize (stream, destination, options) }

    member this.AsyncResize (sourcePath : string, destinationPath : string, options) = async {
      use sourceStream = F.read sourcePath
      use destinationStream = F.write destinationPath
      do! this.AsyncResize (sourceStream, destinationStream, options) }

  type IImageAnalyzer with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncGetImageInfo (source : IStreamProducer) =
      this.AsyncGetImageInfo (ProducerSource source)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncGetImageInfo (source : Stream) =
      this.AsyncGetImageInfo (StreamSource source)

    member this.AsyncGetImageInfo (sourcePath : string) = async {
      use stream = F.read sourcePath
      return! this.AsyncGetImageInfo (stream) }


/// Contains cross-language image resizer extensions.
[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IImageSource, destination : IImageDestination, options) =
    match this with
    | :? AsyncImageResizer as inst -> inst.ResizeDirect(source, destination, options, CancellationToken.None).GetAwaiter().GetResult()
    | _                            -> this.AsyncResize(source, destination, options) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IImageSource, destination : IStreamConsumer, options) =
    this.Resize (source, ConsumerDestination destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IImageSource, destination : Stream, options) =
    this.Resize (source, StreamDestination destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, source : IImageSource, destinationPath : string, options) =
    use stream = F.write destinationPath
    this.Resize (source, stream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IStreamProducer, destination : IImageDestination, options) =
    this.Resize (ProducerSource source, destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IStreamProducer, destination : IStreamConsumer, options) =
    this.Resize (ProducerSource source, ConsumerDestination destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : IStreamProducer, destination : Stream, options) =
    this.Resize (ProducerSource source, StreamDestination destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, source : IStreamProducer, destinationPath : string, options) =
    use stream = F.write destinationPath
    this.Resize (source, stream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destination : IImageDestination, options) =
    this.Resize (StreamSource source, destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destination : IStreamConsumer, options) =
    this.Resize (StreamSource source, ConsumerDestination destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destination : Stream, options) =
    this.Resize (StreamSource source, StreamDestination destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, source : Stream, destinationPath : string, options) =
    use stream = F.write destinationPath
    this.Resize (source, stream, options)

  [<Extension>]
  static member Resize (this : IImageResizer, sourcePath : string, destination : IImageDestination, options) =
    use stream = F.read sourcePath
    this.Resize (stream, destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, sourcePath : string, destination : IStreamConsumer, options) =
    use stream = F.read sourcePath
    this.Resize (stream, destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, sourcePath : string, destination : Stream, options) =
    use stream = F.read sourcePath
    this.Resize (stream, destination, options)

  [<Extension>]
  static member Resize (this : IImageResizer, sourcePath : string, destinationPath : string, options) =
    use sourceStream = F.read sourcePath
    use destinationStream = F.write destinationPath
    this.Resize (sourceStream, destinationStream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IImageSource, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.ResizeDirect(source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResize(source, destination, options), cancellationToken = cancellationToken) :> _

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IImageSource, destination : IStreamConsumer, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (source, ConsumerDestination destination, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IImageSource, destination : Stream, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (source, StreamDestination destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IImageSource, destinationPath : string, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (source, destinationPath, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IStreamProducer, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (ProducerSource source, destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IStreamProducer, destination : IStreamConsumer, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (ProducerSource source, ConsumerDestination destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IStreamProducer, destination : Stream, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (ProducerSource source, StreamDestination destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : IStreamProducer, destinationPath : string, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (source, destinationPath, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (StreamSource source, destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destination : IStreamConsumer, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (StreamSource source, ConsumerDestination destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destination : Stream, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (StreamSource source, StreamDestination destination, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destinationPath : string, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (source, destinationPath, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, sourcePath : string, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (sourcePath, destination, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, sourcePath : string, destination : IStreamConsumer, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (sourcePath, destination, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, sourcePath : string, destination : Stream, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (sourcePath, destination, options), cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, sourcePath : string, destinationPath : string, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncResize (sourcePath, destinationPath, options), cancellationToken = cancellationToken) :> Task

[<Extension>]
[<Sealed; AbstractClass>]
type ImageAnalyzerExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageAnalyzer, source : IImageSource) =
    match this with
    | :? AsyncImageAnalyzer as inst -> inst.GetImageInfoDirect(source, CancellationToken.None).GetAwaiter().GetResult ()
    | _                             -> this.AsyncGetImageInfo source |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageAnalyzer, source : IStreamProducer) =
    this.GetImageInfo (ProducerSource source)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageAnalyzer, source : Stream) =
    this.GetImageInfo (StreamSource source)

  [<Extension>]
  static member GetImageInfo (this : IImageAnalyzer, sourcePath : string) =
    use stream = F.read sourcePath
    this.GetImageInfo stream

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfoAsync (this : IImageAnalyzer, source : IImageSource, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageAnalyzer as inst -> inst.GetImageInfoDirect (source, cancellationToken)
    | _                             -> Async.StartAsTask (this.AsyncGetImageInfo source, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfoAsync (this : IImageAnalyzer, source : IStreamProducer, [<Optional>] cancellationToken) =
    this.GetImageInfoAsync (ProducerSource source, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfoAsync (this : IImageAnalyzer, source : Stream, [<Optional>] cancellationToken) =
    this.GetImageInfoAsync (StreamSource source, cancellationToken)

  [<Extension>]
  static member GetImageInfoAsync (this : IImageAnalyzer, sourcePath : string, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncGetImageInfo sourcePath, cancellationToken = cancellationToken)

