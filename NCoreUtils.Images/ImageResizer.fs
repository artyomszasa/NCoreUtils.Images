namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Images.Internal
open NCoreUtils.IO

type ImageResizer =
  val private provider      : IImageProvider
  val private resizers      : ResizerCollection
  val private options       : IImageResizerOptions
  val private logger        : ILogger

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new  (provider : IImageProvider,
        resizers,
        logger : ILogger<ImageResizer>,
        [<Optional; DefaultParameterValue(null:IImageResizerOptions)>] options : IImageResizerOptions) =
    let opts = if isNull options then (ImageResizerOptions.Default :> IImageResizerOptions) else options
    if opts.MemoryLimit.HasValue then
      provider.MemoryLimit <- opts.MemoryLimit.Value
    { provider      = provider
      resizers      = resizers
      options       = opts
      logger        = logger }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member private __.DecideImageType (options : ResizeOptions, image : IImage) =
    match options.ImageType with
    | null  -> struct (false, image.ImageType)
    | itype -> struct (true, ImageType.ofExtension itype)

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member private this.DecideQuality (options : ResizeOptions, imageType) =
    match options.Quality.HasValue with
    | true -> options.Quality.Value
    | _    -> this.options.Quality imageType

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member private this.DecideOptimize (options : ResizeOptions, imageType) =
    match options.Optimize.HasValue with
    | true -> options.Optimize.Value
    | _    -> this.options.Optimize imageType

  member private this.CreateTransformation (setContentType: string -> unit, options : ResizeOptions) =
    this.logger.LogDebug ("Creating transformation with options {0}", options)
    let resizeMode = options.ResizeMode |?? "none"
    match this.resizers.TryGetValue resizeMode with
    | ValueNone -> ImageError.resizeMode resizeMode "Specified resize mode is not supported."
    | ValueSome factory ->
      { new IStreamTransformation with
          member __.Dispose () = ()
          member __.AsyncPerform (input, output) = async {
            use! image = this.provider.AsyncFromStream input
            let struct (isExplicit, imageType) = this.DecideImageType (options, image)
            let quality = this.DecideQuality (options, imageType)
            let optimize = this.DecideOptimize (options, imageType)
            this.logger.LogDebug (
              "Resizing image with options [ImageType = {0}, Quality = {1}, Optimization = {2}]",
              (if isExplicit then imageType else sprintf "%s (implicit)" imageType),
              quality,
              optimize)
            setContentType imageType
            let resizer = factory.CreateResizer (image, options)
            image.Resize resizer
            do! image.AsyncWriteTo (output, imageType, quality, optimize)
            do! output.AsyncFlush ()
            output.Close () }
      }

  member this.AsyncResize (source : IImageSource, destination : IImageDestination, options) = async {
    let contentType = ref null
    use consumer =
      let resizeTransformation = this.CreateTransformation ((fun value -> contentType := value), options)
      let consumer =
        { new IStreamConsumer with
            member __.AsyncConsume input =
              let ct = !contentType |?? "application/octet-stream"
              this.logger.LogDebug ("Initializing image destination with content type {0}.", ct)
              destination.CreateConsumer (ContentInfo (contentType = ct)) |> StreamConsumer.asyncConsume input
            member __.Dispose () = ()
        }
      StreamTransformation.chainConsumer resizeTransformation consumer
    use producer = source.CreateProducer ()
    do! StreamConsumer.asyncConsumeProducer producer consumer }

  member this.AsyncGetImageInfo (source : IImageSource) = async {
    use  consumer = StreamToResultConsumer.Of this.provider.AsyncFromStream
    use  producer = source.CreateProducer ()
    use! image = StreamToResultConsumer.asyncConsumeProducer producer consumer
    return image.GetImageInfo () }

  interface IImageResizer with
    member this.AsyncResize (source, destination, options) =
      this.AsyncResize (source, destination, options)

  interface IImageAnalyzer with
    member this.AsyncGetImageInfo source =
      this.AsyncGetImageInfo source
