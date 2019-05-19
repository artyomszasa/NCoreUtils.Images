namespace NCoreUtils.Images.Server

open System
open System.Runtime.InteropServices
open NCoreUtils
open NCoreUtils.Images
open NCoreUtils.IO

type ResizeOptions = {
  ImageType  : string
  Width      : Nullable<int>
  Height     : Nullable<int>
  ResizeMode : string
  Quality    : Nullable<int>
  Optimize   : Nullable<bool>
  WeightX    : Nullable<int>
  WeightY    : Nullable<int> }
  with
    interface IResizeOptions with
      member this.Height     = this.Height
      member this.ImageType  = this.ImageType
      member this.Optimize   = this.Optimize
      member this.Quality    = this.Quality
      member this.ResizeMode = this.ResizeMode
      member this.WeightX    = this.WeightX
      member this.WeightY    = this.WeightY
      member this.Width      = this.Width


type ServerUtils =
  val private serviceProvider : IServiceProvider
  val private imageResizer    : IImageResizer
  val private extractors      : MetadataExtractorConfiguration

  new (serviceProvider,
        imageResizer,
        [<Optional; DefaultParameterValue(null : MetadataExtractorConfiguration)>] extractors) =
    let extrs =
      match isNull extractors with
      | true -> MetadataExtractorConfiguration (MetadataExtractorConfigurationBulder ())
      | _    -> extractors
    { serviceProvider = serviceProvider
      imageResizer    = imageResizer
      extractors      = extrs }

  member this.TryExtractSource (meta : Meta) =
    let rec impl index =
      match index >= this.extractors.SourceExtractors.Count with
      | true -> ValueNone
      | _    ->
        let res =
          use extractorSvc = this.serviceProvider.GetOrActivateService this.extractors.SourceExtractors.[index]
          let extractor = extractorSvc.BoxedService :?> IImageSourceExtractor
          extractor.TryExtract meta
        match res with
        | ValueNone -> impl (index + 1)
        | _         -> res
    impl 0

  member this.TryExtractDestination (meta : Meta) =
    let rec impl index =
      match index >= this.extractors.DestinationExtractors.Count with
      | true -> ValueNone
      | _    ->
        let res =
          use extractorSvc = this.serviceProvider.GetOrActivateService this.extractors.DestinationExtractors.[index]
          let extractor = extractorSvc.BoxedService :?> IImageDestinationExtractor
          extractor.TryExtract meta
        match res with
        | ValueNone -> impl (index + 1)
        | _         -> res
    impl 0

  member this.AsyncTryExtractSource (provider : IMetadataProvider) =
    provider.AsyncGetMetadata ()
    >>| this.TryExtractSource

  member this.AsyncTryExtractDestination (provider : IMetadataProvider) =
    provider.AsyncGetMetadata ()
    >>| this.TryExtractDestination

  member this.AsyncResize (producer : IStreamProducer, consumer : IStreamConsumer, options, applyImageType: string -> unit) = async {
    use transformation = StreamTransformation.chainDependent (this.imageResizer.CreateTransformation options) (fun imageType -> applyImageType imageType; ValueNone)
    do! StreamConsumer.asyncConsumeProducer (StreamTransformation.chainProducer transformation producer) consumer }

