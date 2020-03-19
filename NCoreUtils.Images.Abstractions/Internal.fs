namespace NCoreUtils.Images.Internal

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NCoreUtils.Images

type
  [<Struct>]
  [<StructuralEquality; NoComparison>]
  Size = {
    Width  : int
    Height : int }
  with
    static member Empty = { Width = 0; Height = 0 }

type
  [<Struct>]
  [<StructuralEquality; NoComparison>]
  Point = {
    X : int
    Y : int }
  with
    static member Empty = { X = 0; Y = 0 }

type
  [<Struct>]
  [<StructuralEquality; NoComparison>]
  Rectangle = {
    Point : Point
    Size  : Size }
  with
    static member Empty = { Point = Point.Empty; Size = Size.Empty }
    member this.X      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Point.X
    member this.Y      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Point.Y
    member this.Width  with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Size.Width
    member this.Height with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Size.Height
    member this.Top    with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Y
    member this.Left   with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.X
    member this.Bottom with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.Y + this.Height
    member this.Right  with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.X + this.Width

[<CompiledName("ImageTypes")>]
[<RequireQualifiedAccess>]
[<Extension>]
module ImageType =

  [<Literal>]
  let Jpeg = "jpeg"

  [<Literal>]
  let Bmp = "bmp"

  [<Literal>]
  let Png = "png"

  [<Literal>]
  let Gif = "gif"

  [<Literal>]
  let Tiff = "tiff"

  [<Literal>]
  let WebP = "webp"

  [<CompiledName("OfExtension")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let ofExtension (ext: string) =
    match ext.ToLowerInvariant () with
    | "jpg" -> Jpeg
    | "tif" -> Tiff
    |  ext  -> ext

  [<CompiledName("ToExtension")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  let toExtension imageType =
    match imageType with
    | Jpeg -> "jpg"
    | _    -> imageType

  [<CompiledName("ToMediaType")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  let toMediaType imageType =
    match imageType with
    | Jpeg -> "image/jpeg"
    | Png  -> "image/png"
    | Gif  -> "image/gif"
    | WebP -> "image/webp"
    | _    -> sprintf "image/%s" imageType


type
  [<Interface>]
  IImageProvider =
    abstract MemoryLimit : int64 with get, set
    abstract AsyncFromStream : stream:Stream -> Async<IImage>

and
  [<Interface>]
  IImage =
    inherit IDisposable
    abstract Provider        : IImageProvider
    abstract Size            : Size
    abstract NativeImageType : obj
    abstract ImageType       : string
    abstract AsyncWriteTo    : stream:Stream * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool -> Async<unit>
    abstract Resize          : size:Size      -> unit
    abstract Crop            : rect:Rectangle -> unit
    abstract GetImageInfo    : unit -> ImageInfo

type
  [<Interface>]
  IDirectImageProvider =
    inherit IImageProvider
    abstract AsyncFromPath : path:string -> Async<IImage>

type
  [<Interface>]
  IDirectImage =
    inherit IImage
    abstract AsyncSaveTo : path:string * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool -> Async<unit>