namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Serialization
open NCoreUtils
open NCoreUtils.Images.Internal
open System.IO

module private InvalidResizeModeExceptionHelpers =
  [<Literal>]
  let KeyMode = "ResizeMode"

type InvalidResizeModeException =
  inherit ImageResizerException
  val private mode : string
  new (mode : string) =
    { inherit ImageResizerException (ImageResizerError.invalidMode mode)
      mode = mode }
  new (info : SerializationInfo, context) =
    { inherit ImageResizerException (info, context)
      mode = info.GetString InvalidResizeModeExceptionHelpers.KeyMode }
  member this.ResizeMode with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.mode
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (InvalidResizeModeExceptionHelpers.KeyMode, this.mode)

module private InvalidImageTypeExceptionHelpers =
  [<Literal>]
  let KeyImageType = "ImageType"

type InvalidImageTypeException =
  inherit ImageResizerException
  val private imageType : string
  new (imageType : string) =
    { inherit ImageResizerException (ImageResizerError.invalidImageType imageType)
      imageType = imageType }
  new (info : SerializationInfo, context) =
    { inherit ImageResizerException (info, context)
      imageType = info.GetString InvalidImageTypeExceptionHelpers.KeyImageType }
  member this.ImageType with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.ImageType
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (InvalidImageTypeExceptionHelpers.KeyImageType, this.imageType)

[<AutoOpen>]
module private ImageResizerHelpers =

  let private mkTemp (ext : string) = sprintf "%s%s.%s" (Path.GetTempPath ()) (System.Guid.NewGuid().ToString()) ext;

  let asyncTempFile (ext : string) action = async {
    let path = mkTemp ext
    try return! action path
    finally try if File.Exists path then File.Delete path with _ -> () }

type ImageResizer =
  val private provider : IImageProvider
  val private resizers : ResizerCollection
  val private options  : IImageResizerOptions
  val private logger   : ILog

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (provider : IImageProvider, resizers, logger : ILog<ImageResizer>, [<Optional; DefaultParameterValue(null:IImageResizerOptions)>] options : IImageResizerOptions) =
    let opts = if isNull options then (ImageResizerOptions.Default :> IImageResizerOptions) else options
    if opts.MemoryLimit.HasValue then
      provider.MemoryLimit <- opts.MemoryLimit.Value
    { provider = provider
      resizers = resizers
      options  = opts
      logger   = logger }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.DoAsyncResize (source : unit -> Async<IImage>, destination : IImageDestination, options : ResizeOptions) = async {
    this.logger.LogDebug (null, sprintf "Processing options = %A" options)
    match this.resizers.TryGetValue (options.ResizeMode |?? "none") with
    | None -> options.ResizeMode |?? "none" |> InvalidResizeModeException |> raise
    | Some factory ->
      use! image     = source ()
      let  imageType =
        match options.ImageType with
        | null  -> image.ImageType
        | itype ->
        match ImageType.ofExtension itype with
        | ImageType.Other -> itype |> InvalidImageTypeException |> raise
        | imageType       -> imageType
      let resizer = factory.CreateResizer (image, options)
      image.Resize resizer
      // FIXME: optimization
      let quality =
        match options.Quality.HasValue with
        | true -> options.Quality.Value
        | _    -> this.options.Quality imageType
      match image with
      | :? IDirectImage as dimage ->
        let extension = ImageType.toExtension imageType
        do! asyncTempFile
              extension
              (fun path -> async {
                do! dimage.AsyncSaveTo (path, quality)
                use buffer = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous ||| FileOptions.SequentialScan)
                do! destination.AsyncWrite (ContentInfo (contentType = ImageType.toMediaType imageType, contentLength = Nullable.mk buffer.Length), fun stream -> buffer.AsyncCopyTo (stream, 65536))
              })
        this.logger.LogDebug (null, sprintf "Successfully created image of type %A (using direct file)" imageType)
      | _ ->
        do! destination.AsyncWrite (ContentInfo (contentType = ImageType.toMediaType imageType), fun stream -> image.AsyncWriteTo (stream, imageType, quality))
        this.logger.LogDebug (null, sprintf "Successfully created image of type %A" imageType) }


  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncResize (source : Stream, destination : IImageDestination, options : ResizeOptions) =
    this.DoAsyncResize ((fun () -> this.provider.AsyncFromStream source), destination, options)
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncGetImageInfo stream = async {
    this.logger.LogDebug (null, "Getting image information.")
    use! image = this.provider.AsyncFromStream stream
    let info = image.GetImageInfo ()
    printfn "%A" info
    this.logger.LogDebug (null, "Successfully retrieved image information.")
    return info }
  member this.AsyncResize (source : string, destination : IImageDestination, options : ResizeOptions) =
    match this.provider with
    | :? IDirectImageProvider as dprovider ->
      this.DoAsyncResize ((fun () -> dprovider.AsyncFromPath source), destination, options)
    | provider -> async {
      use bufferStream = new FileStream (source, FileMode.Open, FileAccess.Read, FileShare.Read)
      do! this.DoAsyncResize ((fun () -> provider.AsyncFromStream bufferStream), destination, options) }

  interface IImageResizer with
    member this.AsyncResize (source : string, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncResize (source : Stream, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncGetImageInfo stream = this.AsyncGetImageInfo stream