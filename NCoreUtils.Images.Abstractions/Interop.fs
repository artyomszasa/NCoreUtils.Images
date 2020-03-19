namespace NCoreUtils.Images

open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils

[<AbstractClass>]
type AsyncImageResizer () =
  abstract ResizeAsync
    :  source:IImageSource
    *  destination:IImageDestination
    *  options:ResizeOptions
    *  [<Optional>] cancellationToken:CancellationToken
    -> Task

  member inline internal this.ResizeDirect (source : IImageSource, destination, options, cancellationToken) =
    this.ResizeAsync (source, destination, options, cancellationToken)

  interface IImageResizer with
    member this.AsyncResize (source : IImageSource, destination, options) =
      Async.Adapt (fun cancellationToken -> this.ResizeAsync (source, destination, options, cancellationToken))

[<AbstractClass>]
type AsyncImageAnalyzer () =
  abstract GetImageInfoAsync
    :  source:IImageSource
    *  [<Optional>] cancellationToken:CancellationToken
    -> Task<ImageInfo>

  member inline internal this.GetImageInfoDirect (source, cancellationToken) =
    this.GetImageInfoAsync (source, cancellationToken)

  interface IImageAnalyzer with
    member this.AsyncGetImageInfo source =
      Async.Adapt (fun cancellationToken -> this.GetImageInfoAsync (source, cancellationToken))