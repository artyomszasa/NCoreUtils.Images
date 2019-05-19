namespace NCoreUtils.Images.Client

open System.Collections.Generic
open NCoreUtils.Images
open NCoreUtils.IO

type public Meta = IReadOnlyDictionary<string, string>

[<AbstractClass>]
type ImageResizerClientBase =
  new () = { }

  abstract AsyncTransform : producer:IStreamProducer * consumer:IStreamConsumer * options:IResizeOptions -> Async<unit>

  abstract AsyncTransform : producer:IStreamProducer * target:Meta * options:IResizeOptions -> Async<unit>

  abstract AsyncTransform : source:Meta * target:Meta * options:IResizeOptions -> Async<unit>

  abstract AsyncTransform : source:Meta * consumer:IStreamConsumer * options:IResizeOptions -> Async<unit>

  abstract AsyncGetInfo   : source:Meta -> Async<ImageInfo>

  abstract AsyncGetInfo   : producer:IStreamProducer -> Async<ImageInfo>

  abstract CreateTransformation : options:IResizeOptions -> IDependentStreamTransformation<string>

  abstract AsyncGetInfo         : source:IImageSource    -> Async<ImageInfo>

  abstract AsyncTransform : source:IImageSource * destination:IImageDestination * options:IResizeOptions -> Async<unit>

  default this.AsyncGetInfo (source : IImageSource) =
    match source with
    | :? IMetadataProvider as imeta ->
      async {
        let! source = imeta.AsyncGetMetadata ()
        return! this.AsyncGetInfo source }
    | _ ->
      async {
        use producer = source.CreateProducer ()
        return! this.AsyncGetInfo producer }

  default this.AsyncTransform (producer : IStreamProducer, consumer : IStreamConsumer, options : IResizeOptions) = async {
    let transformation = StreamTransformation.chainDependent (this.CreateTransformation options) (fun _ -> ValueNone)
    do! StreamConsumer.asyncConsumeProducer (StreamTransformation.chainProducer transformation producer) consumer }

  default this.AsyncTransform (source : IImageSource, destination : IImageDestination, options) =
    match box source with
    | :? IMetadataProvider as imeta ->
      match box destination with
      | :? IMetadataProvider as ometa ->
        async {
          let! source = imeta.AsyncGetMetadata ()
          let! target = ometa.AsyncGetMetadata ()
          do!  this.AsyncTransform (source, target, options) }
      | _ ->
        async {
          let! source = imeta.AsyncGetMetadata ()
          use  target = destination.CreateConsumer ()
          do!  this.AsyncTransform (source, target, options) }
    | _ ->
      match box destination with
      | :? IMetadataProvider as ometa ->
        async {
          use  source = source.CreateProducer ()
          let! target = ometa.AsyncGetMetadata ()
          do!  this.AsyncTransform (source, target, options) }
      | _ ->
        async {
          use producer = source.CreateProducer ()
          use consumer = destination.CreateConsumer ()
          do! this.AsyncTransform (producer, consumer, options) }

  interface IImageStreamResizer with
    member this.AsyncGetInfo source =
      this.AsyncGetInfo source
    member this.AsyncTransform (source, destination, options) =
      this.AsyncTransform (source, destination, options)
    member this.CreateTransformation options =
      this.CreateTransformation options

