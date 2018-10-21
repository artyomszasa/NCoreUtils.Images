namespace NCoreUtils.Images

open System
open System.IO
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.IO

[<Interface>]
[<AllowNullLiteral>]
type IHttpClientFactoryAdapter =
  abstract CreateClient : name:string -> HttpClient

[<Interface>]
type ILog =
  abstract LogDebug   : exn:exn * message:string -> unit
  abstract LogWarning : exn:exn * message:string -> unit
  abstract LogError   : exn:exn * message:string -> unit

[<Interface>]
type ILog<'a> = inherit ILog

[<Interface>]
type IImageDestination =
  abstract AsyncWrite        : contentInfo:ContentInfo * generator:(Stream -> Async<unit>) -> Async<unit>
  abstract AsyncWriteDelayed : generator:((ContentInfo -> unit) -> Stream -> Async<unit>) -> Async<unit>

[<Interface>]
type IImageResizer =
  abstract AsyncResize       : source:Stream * destination:IImageDestination * options:ResizeOptions -> Async<unit>
  abstract AsyncResResize    : source:Stream * destination:IImageDestination * options:ResizeOptions -> Async<Result<unit, ImageResizerError>>
  abstract AsyncGetImageInfo : source:Stream -> Async<ImageInfo>

[<Interface>]
type IImageOptimization =
  inherit IStreamTransformation
  abstract Supports         : imageType:string -> bool

// C# interop

// FIXME: move to NCoreUtils.FSharp

[<AbstractClass>]
type AsyncImageDestination () =
  abstract WriteAsync        : contentInfo:ContentInfo * generator:Func<Stream, CancellationToken, Task> * cancellationToken:CancellationToken -> Task
  abstract WriteDelayedAsync : generator:Func<Action<ContentInfo>, Stream, CancellationToken, Task> * cancellationToken:CancellationToken -> Task
  member inline internal this.WriteDirect (contentInfo, generator, cancellationToken) = this.WriteAsync (contentInfo, generator, cancellationToken)
  member inline internal this.WriteDelayedDirect (generator, cancellationToken) = this.WriteDelayedAsync (generator, cancellationToken)
  interface IImageDestination with
    member this.AsyncWrite (contentInfo, generator) =
      let gen = Func<Stream, CancellationToken, Task> (fun stream cancellationToken -> Async.StartAsTask (generator stream, cancellationToken = cancellationToken) :> Task)
      Async.Adapt (fun cancellationToken -> this.WriteAsync (contentInfo, gen, cancellationToken))
    member this.AsyncWriteDelayed generator =
      let gen = Func<Action<ContentInfo>, Stream, CancellationToken, Task> (fun action stream cancellationToken -> Async.StartAsTask (generator action.Invoke stream, cancellationToken = cancellationToken) :> Task)
      Async.Adapt (fun cancellationToken -> this.WriteDelayedAsync (gen, cancellationToken))

[<AbstractClass>]
type AsyncImageResizer () =
  abstract ResizeAsync       : source:Stream * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task
  abstract TryResizeAsync    : source:Stream * destination:IImageDestination * options:ResizeOptions * cancellationToken:CancellationToken -> Task<AsyncResult<ImageResizerError>>
  abstract GetImageInfoAsync : source:Stream * cancellationToken:CancellationToken -> Task<ImageInfo>
  member inline internal this.ResizeDirect (source : Stream, destination, options, cancellationToken) = this.ResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.TryResizeDirect (source : Stream, destination, options, cancellationToken) = this.TryResizeAsync (source, destination, options, cancellationToken)
  member inline internal this.GetImageInfoDirect (source, cancellationToken) = this.GetImageInfoAsync (source, cancellationToken)
  interface IImageResizer with
    member this.AsyncResize (source : Stream, destination, options) = Async.Adapt (fun cancellationToken -> this.ResizeAsync (source, destination, options, cancellationToken))
    member this.AsyncResResize (source : Stream, destination, options) =
      Async.Adapt (fun cancellationToken -> this.TryResizeAsync (source, destination, options, cancellationToken))
      >>| AsyncResult.ToResult
    member this.AsyncGetImageInfo source = Async.Adapt (fun cancellationToken -> this.GetImageInfoAsync (source, cancellationToken))

// [<AbstractClass>]
// type AsyncImageOptimization () =
//   abstract Supports : imageType:string -> bool
//   abstract PerformAsync : input:Stream * output:Stream * cancellationToken:CancellationToken -> Task
//   member inline internal this.PerformDirect (input, output, cancellationToken) = this.PerformAsync (input, output, cancellationToken)
//   interface IImageOptimization with
//     member this.Supports imageType = this.Supports imageType
//     member this.AsyncPerform (input, output) = Async.Adapt (fun cancellationToken -> this.PerformAsync (input, output, cancellationToken))
