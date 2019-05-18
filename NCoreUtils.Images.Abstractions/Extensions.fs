namespace NCoreUtils.Images
open NCoreUtils.IO

[<AutoOpen>]
module CommonExt =

  type IImageResizer with
    member this.AsyncResize (source, destination, options) =
      match this with
      | :? IImageStreamResizer as streamResizer ->
        streamResizer.AsyncTransform (source, destination, options)
      | _ -> async {
        use producer = source.CreateProducer ()
        use consumer = destination.CreateConsumer ()
        use transformation = StreamTransformation.chainDependent (this.CreateTransformation options) (fun _ -> ValueNone)
        do! StreamConsumer.asyncConsumeProducer (StreamTransformation.chainProducer transformation producer) consumer }