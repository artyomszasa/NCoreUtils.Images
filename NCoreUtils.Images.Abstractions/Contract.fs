namespace NCoreUtils.Images

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.Serialization
open NCoreUtils.IO

[<RequireQualifiedAccess>]
module Defaults =

  [<Literal>]
  let ChunkSize = 262144

[<NoEquality; NoComparison>]
type ImageInfo = {
  Width       : int
  Height      : int
  XResolution : int
  YResolution : int
  Iptc        : ImmutableDictionary<string, string>
  Exif        : ImmutableDictionary<string, string> }

type IResizeOptions =
  abstract ImageType  : string
  abstract Width      : Nullable<int>
  abstract Height     : Nullable<int>
  abstract ResizeMode : string
  abstract Quality    : Nullable<int>
  abstract Optimize   : Nullable<bool>
  abstract WeightX    : Nullable<int>
  abstract WeightY    : Nullable<int>

type IMetadataProvider =
  abstract AsyncGetMetadata : unit -> Async<IReadOnlyDictionary<string, string>>

[<AllowNullLiteral>]
type IImageSource =
  abstract CreateProducer : unit -> IStreamProducer

[<AllowNullLiteral>]
type IImageDestination =
  abstract CreateConsumer : unit -> IStreamConsumer

[<Interface>]
[<AllowNullLiteral>]
type IImageResizerOptions =
  abstract Quality  : imageType:string -> int
  abstract Optimize : imageType:string -> bool

type DefaultImageResizerOptions private () =
  static member val Instance = DefaultImageResizerOptions ()
  member __.Quality imageType =
    match imageType with
    | "jpg"
    | "jpeg" -> 75
    | "png"  -> 95
    | _      -> 100
  interface IImageResizerOptions with
    member this.Quality imageType = this.Quality imageType
    member __.Optimize _ = false


type IImageResizer =
  abstract CreateTransformation : options:IResizeOptions -> IDependentStreamTransformation<string>
  abstract AsyncGetInfo         : source:IImageSource    -> Async<ImageInfo>

type IImageStreamResizer =
  inherit IImageResizer
  abstract AsyncTransform : source:IImageSource * destination:IImageDestination * options:IResizeOptions -> Async<unit>

type ImageResizerErrorCategory =
  | Invalid      = 0
  | Internal     = 1
  | NotSupported = 2

[<Serializable>]
type ImageResizerException =
  inherit Exception

  static member NotSupported message =
    ImageResizerException (ImageResizerErrorCategory.NotSupported, message)

  val Category : ImageResizerErrorCategory

  new (category, message) =
    { inherit Exception (message)
      Category = category }

  new (category, message : string, innerException) =
    { inherit Exception (message, innerException)
      Category = category }

  new (info : SerializationInfo, context) =
    { inherit Exception (info, context)
      Category = (enum<ImageResizerErrorCategory> <| info.GetInt32 "ErrorCategory") }

  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue ("ErrorCategory", int this.Category)
