namespace NCoreUtils.Images.Providers

open System
open System.IO
open System.Runtime.InteropServices
open NCoreUtils.IO
open NCoreUtils.Images

[<Struct>]
[<StructuralEquality; NoComparison>]
type Size = {
  Width  : int
  Height : int }
  with
    static member Empty = { Width = Unchecked.defaultof<_>; Height = Unchecked.defaultof<_> }

[<Struct>]
[<StructuralEquality; NoComparison>]
type Point = {
  X : int
  Y : int }
  with
    static member Empty = { X = Unchecked.defaultof<_>; Y = Unchecked.defaultof<_> }

[<Struct>]
[<StructuralEquality; NoComparison>]
type Rectangle = {
  Point : Point
  Size  : Size }
  with
    static member Empty = { Point = Unchecked.defaultof<_>; Size = Unchecked.defaultof<_> }
    member this.X      = this.Point.X
    member this.Y      = this.Point.Y
    member this.Width  = this.Size.Width
    member this.Height = this.Size.Height


[<AbstractClass>]
type ImageProvider () =
  abstract AsyncCreate : source:Stream -> Async<IImage>
  abstract Dispose : disposing:bool -> unit
  member this.Dispose () = this.Dispose true
  interface IStreamConsumer<IImage> with
    member this.AsyncConsume source = this.AsyncCreate source
    member this.Dispose () = this.Dispose ()

and IImage =
    inherit IDisposable
    abstract Provider        : ImageProvider
    abstract Size            : Size
    abstract NativeImageType : obj
    abstract ImageType       : string
    abstract AsyncWriteTo    : stream:Stream * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool -> Async<unit>
    abstract AsyncSaveTo     : path:string   * imageType:string * [<Optional; DefaultParameterValue(85)>] quality:int * [<Optional; DefaultParameterValue(true)>] optimize:bool -> Async<unit>
    abstract Resize          : size:Size      -> unit
    abstract Crop            : rect:Rectangle -> unit
    abstract GetImageInfo    : unit -> ImageInfo