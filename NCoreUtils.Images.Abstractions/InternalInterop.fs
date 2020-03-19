namespace NCoreUtils.Images.Internal

open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.Images

type
  [<AbstractClass>]
  AsyncImageProvider () =
    abstract MemoryLimit : int64 with get, set
    abstract FromStreamAsync : stream:Stream * [<Optional>] cancellationToken:CancellationToken -> Task<IImage>
    member inline internal this.FromStreamDirect (stream, cancellationToken) = this.FromStreamAsync (stream, cancellationToken)
    interface IImageProvider with
      member this.MemoryLimit with get () = this.MemoryLimit and set limit = this.MemoryLimit <- limit
      member this.AsyncFromStream stream = Async.Adapt (fun cancellationToken -> this.FromStreamAsync (stream, cancellationToken))

type
  [<AbstractClass>]
  AsyncDirectImageProvider () =
    inherit AsyncImageProvider ()
    abstract FromPathAsync : path:string * [<Optional>] cancellationToken:CancellationToken -> Task<IImage>
    member inline internal this.FromPathDirect (path, cancellationToken) =
      this.FromPathAsync (path, cancellationToken)
    interface IDirectImageProvider with
      member this.AsyncFromPath path =
        Async.Adapt (fun cancellationToken -> this.FromPathAsync (path, cancellationToken))

type
  [<AbstractClass>]
  AsyncImage =
    val mutable private provider : IImageProvider
    new (provider) = { provider = provider }
    member this.Provider with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.provider
    abstract Size            : Size
    abstract NativeImageType : obj
    abstract ImageType       : string
    abstract WriteToAsync    : stream:Stream * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool * [<Optional>] cancellationToken:CancellationToken -> Task
    abstract Resize          : size:Size      -> unit
    abstract Crop            : rect:Rectangle -> unit
    abstract GetImageInfo    : unit -> ImageInfo
    abstract Dispose         : disposing:bool -> unit
    member inline internal this.WriteToDirect (stream, imageType, quality, optimize, cancellationToken) = this.WriteToAsync (stream, imageType, quality, optimize, cancellationToken)
    interface IImage with
      member this.Provider        = this.Provider
      member this.Size            = this.Size
      member this.NativeImageType = this.NativeImageType
      member this.ImageType       = this.ImageType
      member this.AsyncWriteTo (stream, imageType, quality, optimize) = Async.Adapt (fun cancellationToken -> this.WriteToAsync (stream, imageType, quality, optimize, cancellationToken))
      member this.Resize size     = this.Resize size
      member this.Crop   rect     = this.Crop   rect
      member this.GetImageInfo () = this.GetImageInfo ()
      member this.Dispose () =
        System.GC.SuppressFinalize this
        this.Dispose true

type
  [<AbstractClass>]
  AsyncDirectImage =
    inherit AsyncImage
    new (provider) = { inherit AsyncImage (provider) }
    abstract SaveToAsync : path:string * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool * [<Optional>] cancellationToken:CancellationToken -> Task
    member inline internal this.SaveToDirect (path, imageType, quality, optimize, cancellationToken) = this.SaveToAsync (path, imageType, quality, optimize, cancellationToken)
    interface IDirectImage with
      member this.AsyncSaveTo (path, imageType, quality, optimize) =
        Async.Adapt (fun cancellationToken -> this.SaveToAsync(path, imageType, quality, optimize, cancellationToken))
