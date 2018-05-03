namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Serialization
open NCoreUtils
open NCoreUtils.Images.Internal

module private InvalidResizeModeExceptionHelpers =
  [<Literal>]
  let DefaultMessage = "Invalid or unsupported resize mode."
  [<Literal>]
  let KeyMode = "ResizeMode"

type InvalidResizeModeException =
  inherit ImageResizerException
  val private mode : string
  new (mode : string) =
    { inherit ImageResizerException (InvalidResizeModeExceptionHelpers.DefaultMessage)
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
  let DefaultMessage = "Invalid or unsupported imageType."
  [<Literal>]
  let KeyImageType = "ImageType"

type InvalidImageTypeException =
  inherit ImageResizerException
  val private imageType : string
  new (mode : string) =
    { inherit ImageResizerException (InvalidImageTypeExceptionHelpers.DefaultMessage)
      imageType = mode }
  new (info : SerializationInfo, context) =
    { inherit ImageResizerException (info, context)
      imageType = info.GetString InvalidImageTypeExceptionHelpers.KeyImageType }
  member this.ImageType with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.ImageType
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (InvalidImageTypeExceptionHelpers.KeyImageType, this.imageType)

type ImageResizer =
  val private provider : IImageProvider
  val private resizers : ResizerCollection
  val private options  : IImageResizerOptions
  val private logger   : ILog

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (provider, resizers, logger : ILog<ImageResizer>, [<Optional>] options) =
    { provider = provider
      resizers = resizers
      options  = if isNull (box options) then ImageResizerOptions.Default else options
      logger   = logger }
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncResize (source, destination : IImageDestination, options : ResizeOptions) = async {
    this.logger.LogDebug (null, sprintf "Processing options = %A" options)
    match this.resizers.TryGetValue (options.ResizeMode |?? "none") with
    | None -> options.ResizeMode |?? "none" |> InvalidResizeModeException |> raise
    | Some factory ->
      use! image     = this.provider.AsyncFromStream source
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
      do! destination.AsyncWrite (ContentInfo (contentType = ImageType.toMediaType imageType), fun stream -> image.AsyncWriteTo (stream, imageType))
      this.logger.LogDebug (null, sprintf "Successfully created image of type %A" imageType) }
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncGetImageInfo stream = async {
    this.logger.LogDebug (null, "Getting image information.")
    use! image = this.provider.AsyncFromStream stream
    let info = image.GetImageInfo ()
    this.logger.LogDebug (null, "Successfully retrieved image information.")
    return info }
  interface IImageResizer with
    member this.AsyncResize (source, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncGetImageInfo stream = this.AsyncGetImageInfo stream