namespace NCoreUtils.Images.Providers

open System
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Images
open NCoreUtils.Images.Resizers
open NCoreUtils.IO
open System.Runtime.InteropServices

type ImageResizer =
  val public ServiceProvider : IServiceProvider
  val public ImageProvider   : ImageProvider
  val public Resizers        : IResizerCollection
  val public Logger          : ILogger
  val public Options         : IImageResizerOptions

  new (serviceProvider,
        imageProvider,
        resizers,
        logger : ILogger<ImageResizer>,
        [<Optional; DefaultParameterValue(null : IImageResizerOptions)>] options) =
    { ServiceProvider = serviceProvider
      ImageProvider   = imageProvider
      Resizers        = resizers
      Logger          = logger
      Options         = (options |?? (DefaultImageResizerOptions.Instance :> IImageResizerOptions)) }

  member private this.DecideType (image : IImage, options : IResizeOptions) =
    match options.ImageType with
    | null | "" ->
      let imageType = image.ImageType
      this.Logger.LogDebug ("No explicit output image type specified, targeting input image type ({0}).", imageType)
      imageType
    | imageType ->
      this.Logger.LogDebug ("Targeting specified output image type ({0}).", imageType)
      imageType

  member private this.DecideQuality (options : IResizeOptions) imageType =
    match options.Quality.HasValue with
    | true ->
      this.Logger.LogDebug ("Targeting specified quality ({0}).", options.Quality.Value)
      options.Quality.Value
    | _ ->
      let quality = this.Options.Quality imageType
      this.Logger.LogDebug ("No explicit quality specified, targeting default quality for {0} ({1}).", imageType, quality)
      quality

  member private this.DecideOptimize (options : IResizeOptions) (imageType : string) =
    match options.Optimize.HasValue with
    | true ->
      match options.Optimize.Value with
      | true ->
        this.Logger.LogDebug ("Enabling optimizations for {0} (requested by user).", imageType)
        true
      | _ ->
        this.Logger.LogDebug ("Disabling optimizations for {0} (requested by user).", imageType)
        true
    | _ ->
      let optimize = this.Options.Optimize imageType
      match optimize with
      | true ->
        this.Logger.LogDebug ("Enabling optimizations for {0} (server configuration).", imageType)
        true
      | _ ->
        this.Logger.LogDebug ("Disabling optimizations for {0} (server configuration).", imageType)
        true

  member this.CreateTransformation (options : IResizeOptions) =
    let factory = ref Unchecked.defaultof<_>
    match this.Resizers.TryGetValue (options.ResizeMode |?? "none", factory) with
    | true ->
      DependentStreamTransformation.Of
        (fun input dependentOutput -> async {
          use! image = this.ImageProvider.AsyncCreate input
          this.Logger.LogDebug ("Successfully read image: {0}, {1}x{2}.", image.ImageType, image.Size.Width, image.Size.Height)
          let imageType = this.DecideType (image, options)
          let output = dependentOutput imageType
          let resizer = factory.Value.CreateResizer (image, options)
          image.Resize resizer
          let quality  = this.DecideQuality options imageType
          let optimize = this.DecideOptimize options imageType
          do! image.AsyncWriteTo (output, imageType, quality, optimize)
          do! output.AsyncFlush ()
          this.Logger.LogDebug ("Successfully written image: {0}, {1}x{2}.", imageType, image.Size.Width, image.Size.Height)
          output.Close ()
        })
    | _ ->
      sprintf "Resize mode %s not supported." options.ResizeMode
      |> ImageResizerException.NotSupported
      |> raise

  member this.AsyncGetInfo (source : IImageSource) = async {
    use! image =
      let producer = source.CreateProducer ()
      StreamToResultConsumer.Of this.ImageProvider.AsyncCreate
      |> StreamToResultConsumer.asyncConsumeProducer producer
    return image.GetImageInfo () }

  interface IImageResizer with
    member this.AsyncGetInfo source =
      this.AsyncGetInfo source
    member this.CreateTransformation options =
      this.CreateTransformation options
