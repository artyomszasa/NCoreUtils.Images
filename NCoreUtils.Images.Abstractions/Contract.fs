namespace NCoreUtils.Images

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Serialization
open System.Threading
open System.Threading.Tasks
open NCoreUtils


module private ImageResizerExceptionKeys =
  [<Literal>]
  let KeyImageResizeError = "ImageResizerError"
  [<Literal>]
  let KeyImageResizeErrorType = "ImageResizerErrorType"

[<Serializable>]
type ImageResizerError  =
  val mutable private message : string
  val mutable private description : string
  internal new (message, description) = { message = message; description = description }
  private new () = ImageResizerError (null, null)
  member this.Message = this.message
  member this.Description = this.description
  member private this.Message with set message = this.message <- message
  member private this.Description with set description = this.description <- description
  abstract RaiseException : unit -> 'a
  default this.RaiseException () = ImageResizerException this |> raise

and
  [<Serializable>]
  InvalidArgumentError =
    inherit ImageResizerError
    internal new (message, description) = { inherit ImageResizerError (message, description) }
    private new () = InvalidArgumentError (null, null)
    override this.RaiseException () = ImageResizerException this |> raise

and
  [<Serializable>]
  InvalidResizeModeError =
    inherit InvalidArgumentError
    val mutable private resizeMode : string
    internal new (message, description, resizeMode) =
      { inherit InvalidArgumentError (message, description)
        resizeMode = resizeMode }
    private new () = InvalidResizeModeError (null, null, null)
    member this.ResizeMode = this.resizeMode
    member private this.ResizeMode with set resizeMode = this.resizeMode <- resizeMode
    override this.RaiseException () = InvalidResizeModeException this |> raise

and
  [<Serializable>]
  InvalidImageTypeError =
    inherit InvalidArgumentError
    val mutable private imageType : string
    internal new (message, description, imageType) =
      { inherit InvalidArgumentError (message, description)
        imageType = imageType }
    private new () = InvalidImageTypeError (null, null, null)
    member this.ImageType = this.imageType
    member private this.ImageType with set imageType = this.imageType <- imageType
    override this.RaiseException () = InvalidImageTypeException this |> raise

and
  [<Serializable>]
  ImageResizerException =
    inherit Exception
    val private error : ImageResizerError
    new (error : ImageResizerError) =
      { inherit Exception (error.Message)
        error = error }
    new (error : ImageResizerError, innerException : exn) =
      { inherit Exception (error.Message, innerException)
        error = error }
    new (info : SerializationInfo, context) =
      let errorType = info.GetString ImageResizerExceptionKeys.KeyImageResizeErrorType |> Type.GetType
      { inherit Exception (info, context)
        error = info.GetValue (ImageResizerExceptionKeys.KeyImageResizeError, errorType) :?> ImageResizerError }
    member this.Error = this.error
    override this.Message = this.error.Message
    member this.Descripton = this.error.Description
    override this.GetObjectData (info, context) =
      base.GetObjectData (info, context)
      let errorType = this.error.GetType ()
      info.AddValue (ImageResizerExceptionKeys.KeyImageResizeErrorType, errorType.AssemblyQualifiedName)
      info.AddValue (ImageResizerExceptionKeys.KeyImageResizeError, this.error, errorType)


and
  [<Serializable>]
  InvalidResizeModeException =
    inherit ImageResizerException
    new (error : InvalidResizeModeError) = { inherit ImageResizerException (error) }
    new (error : InvalidResizeModeError, innerException : exn) = { inherit ImageResizerException (error, innerException) }
    new (info : SerializationInfo, context) = { inherit ImageResizerException (info, context) }
    member this.ResizeMode = (this.Error :?> InvalidResizeModeError).ResizeMode

and
  [<Serializable>]
  InvalidImageTypeException =
    inherit ImageResizerException
    new (error : InvalidImageTypeError) = { inherit ImageResizerException (error) }
    new (error : InvalidImageTypeError, innerException : exn) = { inherit ImageResizerException (error, innerException) }
    new (info : SerializationInfo, context) = { inherit ImageResizerException (info, context) }
    member this.ImageType = (this.Error :?> InvalidImageTypeError).ImageType

type ImageOptimizationError =
  inherit ImageResizerError
  val mutable private optimizer : string
  new (message, description, optimizer) =
    { inherit ImageResizerError (message, description)
      optimizer = optimizer }
  private new () = ImageOptimizationError (null, null, null)
  member this.Optimizer = this.optimizer
  member private this.Optimizer with set optimizer = this.optimizer <- optimizer
  override this.RaiseException () = ImageOptimizationException this |> raise

and
  [<Serializable>]
  ImageOptimizationException =
    inherit ImageResizerException
    new (error : ImageOptimizationError) = { inherit ImageResizerException (error) }
    new (error : ImageOptimizationError, innerException : exn) = { inherit ImageResizerException (error, innerException) }
    new (info : SerializationInfo, context) = { inherit ImageResizerException (info, context) }
    member this.Optimizer = (this.Error :?> ImageOptimizationError).Optimizer

[<CompiledName("ImageResizerErrors")>]
module ImageResizerError =

  let generic error errorDescription = new ImageResizerError (error, errorDescription)

  [<CompiledName("InvalidMode")>]
  let invalidMode resizeMode =
    InvalidResizeModeError (
      message     = sprintf "Invalid or unsupported resize mode = %s." resizeMode,
      description = "Specified mode is either invalid or not supported by the acutal instance of the image resizer.",
      resizeMode  = resizeMode)

  [<CompiledName("InvalidImageType")>]
  let invalidImageType imageType =
    InvalidImageTypeError (
      message     = sprintf "Invalid or unsupported image type = %s." imageType,
      description = "Specified image type is either invalid or not supported by the acutal instance of the image resizer.",
      imageType   = imageType)

  [<CompiledName("InvalidImage")>]
  let invalidImage =
    ImageResizerError (
      message     = "Invalid image data specified",
      description = "Specified image type is either invalid or not supported by the acutal instance of the image resizer.")

  [<CompiledName("OptimizationFailed")>]
  let optimizationFailed optimizer description =
    ImageOptimizationError (
      message     = "Optimization failed",
      description = description,
      optimizer   = optimizer)

  [<CompiledName("FormatOptimizationFailed")>]
  let optimizationFailedf optimizer fmt = Printf.kprintf (optimizationFailed optimizer) fmt

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
  abstract AsyncResize : source:string * destination:IImageDestination * options:ResizeOptions -> Async<unit>
  abstract AsyncResize : source:Stream * destination:IImageDestination * options:ResizeOptions -> Async<unit>
  abstract AsyncResResize : source:string * destination:IImageDestination * options:ResizeOptions -> Async<Result<unit, ImageResizerError>>
  abstract AsyncResResize : source:Stream * destination:IImageDestination * options:ResizeOptions -> Async<Result<unit, ImageResizerError>>
  abstract AsyncGetImageInfo : source:Stream -> Async<ImageInfo>

[<Interface>]
type IImageOptimization =
  abstract Supports         : imageType:string -> bool
  abstract AsyncOptimize    : data:byte[] -> Async<byte[]>
  abstract AsyncResOptimize : data:byte[] -> Async<Result<byte[], ImageOptimizationError>>

// C# interop

// FIXME: move to NCoreUtils.FSharp

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
  abstract ResizeAsync : source:string * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task
  abstract ResizeAsync : source:Stream * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task
  abstract TryResizeAsync : source:string * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task<AsyncResult<ImageResizerError>>
  abstract TryResizeAsync : source:Stream * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task<AsyncResult<ImageResizerError>>
  abstract GetImageInfoAsync : source:Stream * cancellationToken:CancellationToken -> Task<ImageInfo>
  member inline internal this.ResizeDirect (source : string, destination, options, cancellationToken) = this.ResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.ResizeDirect (source : Stream, destination, options, cancellationToken) = this.ResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.TryResizeDirect (source : string, destination, options, cancellationToken) = this.TryResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.TryResizeDirect (source : Stream, destination, options, cancellationToken) = this.TryResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.GetImageInfoDirect (source, cancellationToken) = this.GetImageInfoAsync (source, cancellationToken)
  interface IImageResizer with
    member this.AsyncResize (source : string, destination, options) = Async.Adapt (fun cancellationToken -> this.ResizeAsync (source, destination, options, cancellationToken))
    member this.AsyncResize (source : Stream, destination, options) = Async.Adapt (fun cancellationToken -> this.ResizeAsync (source, destination, options, cancellationToken))
    member this.AsyncResResize (source : string, destination, options) =
      Async.Adapt (fun cancellationToken -> this.TryResizeAsync (source, destination, options, cancellationToken))
      >>| AsyncResult.ToResult
    member this.AsyncResResize (source : Stream, destination, options) =
      Async.Adapt (fun cancellationToken -> this.TryResizeAsync (source, destination, options, cancellationToken))
      >>| AsyncResult.ToResult
    member this.AsyncGetImageInfo source = Async.Adapt (fun cancellationToken -> this.GetImageInfoAsync (source, cancellationToken))

[<AbstractClass>]
type AsyncImageOptimization () =
  abstract Supports : imageType:string -> bool
  abstract OptimizeAsync : data:byte[] * cancellationToken:CancellationToken -> Task<byte[]>
  abstract TryOptimizeAsync : data:byte[] * cancellationToken:CancellationToken -> Task<AsyncResult<byte[], ImageOptimizationError>>
  member inline internal this.OptimizeDirect (data, cancellationToken) = this.OptimizeAsync (data, cancellationToken)
  member inline internal this.TryOptimizeDirect (data, cancellationToken) = this.TryOptimizeAsync (data, cancellationToken)
  interface IImageOptimization with
    member this.Supports imageType = this.Supports imageType
    member this.AsyncOptimize data = Async.Adapt (fun cancellationToken -> this.OptimizeAsync (data, cancellationToken))
    member this.AsyncResOptimize data =
      Async.Adapt (fun cancellationToken -> this.TryOptimizeAsync (data, cancellationToken))
      >>| AsyncResult<_, _>.ToResult

// EXTENSIONS

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
    member this.AsyncResize (source : Stream, destinationStream : Stream, options) = this.AsyncResize (source, StreamDestination destinationStream, options)

[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destination : IImageDestination, options) =
    this.AsyncResize (source, destination, options) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : string, destination : IImageDestination, options) =
    this.AsyncResize (source, destination, options) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : Stream, destinationStream : Stream, options) =
    this.Resize (source, StreamDestination destinationStream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Resize (this : IImageResizer, source : string, destinationStream : Stream, options) =
    this.Resize (source, StreamDestination destinationStream, options)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResize (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Out>] error : byref<_>) =
    match this.AsyncResResize (source, destination, options) |> Async.RunSynchronously with
    | Ok () ->
      error <- Unchecked.defaultof<_>
      true
    | Error err ->
      error <- err
      false

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResize (this : IImageResizer, source : string, destination : IImageDestination, options, [<Out>] error : byref<_>) =
    match this.AsyncResResize (source, destination, options) |> Async.RunSynchronously with
    | Ok () ->
      error <- Unchecked.defaultof<_>
      true
    | Error err ->
      error <- err
      false

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResize (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Out>] error : byref<_>) =
    this.TryResize (source, StreamDestination destinationStream, options, &error)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResize (this : IImageResizer, source : string, destinationStream : Stream, options, [<Out>] error : byref<_>) =
    this.TryResize (source, StreamDestination destinationStream, options, &error)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member GetImageInfo (this : IImageResizer, source) =
    this.AsyncGetImageInfo source |> Async.RunSynchronously

  [<Extension>]
  static member ResizeAsync (this : IImageResizer, source : string, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.ResizeDirect (source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResize (source, destination, options), cancellationToken = cancellationToken) :> _

  [<Extension>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.ResizeDirect (source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResize (source, destination, options), cancellationToken = cancellationToken) :> _

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : string, destinationStream : Stream, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResizeAsync (this : IImageResizer, source : string, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.TryResizeDirect (source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResResize (source, destination, options) >>| AsyncResult.FromResult, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResizeAsync (this : IImageResizer, source : Stream, destination : IImageDestination, options, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.TryResizeDirect (source, destination, options, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncResResize (source, destination, options) >>| AsyncResult.FromResult, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResizeAsync (this : IImageResizer, source : string, destinationStream : Stream, options, [<Optional>] cancellationToken) =
    this.TryResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryResizeAsync (this : IImageResizer, source : Stream, destinationStream : Stream, options, [<Optional>] cancellationToken) =
    this.TryResizeAsync (source, StreamDestination destinationStream, options, cancellationToken)

  [<Extension>]
  static member GetImageInfoAsync (this : IImageResizer, source, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageResizer as inst -> inst.GetImageInfoDirect (source, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncGetImageInfo source, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member Optimize (this : IImageOptimization, data : byte[]) =
    this.AsyncOptimize data |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryOptimize (this : IImageOptimization, data : byte[], [<Out>] result : byref<byte[]>, [<Out>] error : byref<_>) =
    match this.AsyncResOptimize data |> Async.RunSynchronously with
    | Ok res ->
      result <- res
      error  <- Unchecked.defaultof<_>
      true
    | Error err ->
      result <- Unchecked.defaultof<_>
      error  <- err
      false

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member TryOptimize (this : IImageOptimization, data : byte[], [<Out>] result : byref<byte[]>) =
    let mutable ignored = Unchecked.defaultof<_>
    this.TryOptimize (data, &result, &ignored)

  [<Extension>]
  static member OptimizeAsync (this : IImageOptimization, data : byte[], cancellationToken : CancellationToken) =
    match this with
    | :? AsyncImageOptimization as inst -> inst.OptimizeDirect (data, cancellationToken)
    | _                                 -> Async.StartAsTask (this.AsyncOptimize data, cancellationToken = cancellationToken)

  [<Extension>]
  static member TryOptimizeAsync (this : IImageOptimization, data : byte[], cancellationToken : CancellationToken) =
    match this with
    | :? AsyncImageOptimization as inst -> inst.TryOptimizeDirect (data, cancellationToken)
    | _                                 -> Async.StartAsTask (this.AsyncResOptimize data >>| AsyncResult<_, _>.FromResult, cancellationToken = cancellationToken)