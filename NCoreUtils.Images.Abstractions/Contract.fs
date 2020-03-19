namespace NCoreUtils.Images

open System
open NCoreUtils.IO

/// Image stream producer source.
[<Interface>]
[<AllowNullLiteral>]
type IImageSource =
  /// Asynchronously creates stream producer that produces image binary data.
  abstract CreateProducer : unit -> IStreamProducer

/// Non-reentrable image destination writer.
[<Interface>]
[<AllowNullLiteral>]
type IImageDestination =
  abstract CreateConsumer : contentInfo:ContentInfo -> IStreamConsumer

/// Core image resizer functionality.
[<Interface>]
type IImageResizer =
  abstract AsyncResize : source:IImageSource * destination:IImageDestination * options:ResizeOptions -> Async<unit>

/// Core image analyzer functionality.
[<Interface>]
type IImageAnalyzer =
  abstract AsyncGetImageInfo : source:IImageSource -> Async<ImageInfo>

/// Serializable image resource functionality.
[<Interface>]
[<AllowNullLiteral>]
type ISerializableImageResource =
  abstract Uri : Uri
