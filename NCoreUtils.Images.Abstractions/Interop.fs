namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.IO

[<AbstractClass>]
type AsyncImageResizer () =
  abstract CreateTransformation : options:IResizeOptions -> IDependentStreamTransformation<string>
  abstract GetInfoAsync         : source:IImageSource * [<Optional>] cancellationToken:CancellationToken -> Task<ImageInfo>
  member internal this.GetInfoDirect (source, cancellationToken) = this.GetInfoAsync (source, cancellationToken)
  interface IImageResizer with
    member this.CreateTransformation options = this.CreateTransformation options
    member this.AsyncGetInfo source = Async.Adapt (fun cancellationToken -> this.GetInfoDirect (source, cancellationToken))

[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerExtensions =

  [<Extension>]
  static member GetInfoAsync (resizer : IImageResizer, source, [<Optional>] cancellationToken) =
    match resizer with
    | :? AsyncImageResizer as inst -> inst.GetInfoDirect (source, cancellationToken)
    | _                            -> Async.StartAsTask (resizer.AsyncGetInfo source, cancellationToken = cancellationToken)

  [<Extension>]
  static member ResizeAsync (resizer : IImageResizer, source, destination, options, [<Optional>] cancellationToken) =
    Async.StartAsTask (resizer.AsyncResize (source, destination, options), cancellationToken = cancellationToken)

