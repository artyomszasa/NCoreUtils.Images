namespace NCoreUtils.Images.Internal

open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open NCoreUtils.Images

[<AutoOpen>]
module ImageExt =

  type IImageProvider with
    member this.AsyncFromData (data : byte[]) = async {
      use buffer = new MemoryStream (data, false)
      return! this.AsyncFromStream buffer }


[<Extension>]
[<Sealed; AbstractClass>]
type ImageExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromStream (this : IImageProvider, stream) =
    match this with
    | :? AsyncImageProvider as inst -> inst.FromStreamDirect(stream, CancellationToken.None).GetAwaiter().GetResult()
    | _                             -> this.AsyncFromStream stream |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromStreamAsync (this : IImageProvider, stream, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImageProvider as inst -> inst.FromStreamDirect (stream, cancellationToken)
    | _                             -> Async.StartAsTask (this.AsyncFromStream stream, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromPath (this : IDirectImageProvider, path) =
    match this with
    | :? AsyncDirectImageProvider as inst -> inst.FromPathDirect(path, CancellationToken.None).GetAwaiter().GetResult()
    | _                                   -> this.AsyncFromPath path |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromPathAsync (this : IDirectImageProvider, path, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncDirectImageProvider as inst -> inst.FromPathDirect (path, cancellationToken)
    | _                                   -> Async.StartAsTask (this.AsyncFromPath path, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromData (this : IImageProvider, data) =
    use buffer = new MemoryStream (data, false)
    this.FromStream buffer

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromDataAsync (this : IImageProvider, data, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncFromData data, cancellationToken = cancellationToken)


  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member WriteTo (this : IImage, stream, imageType, [<Optional; DefaultParameterValue(85)>] quality, [<Optional; DefaultParameterValue(true)>] optimize) =
    match this with
    | :? AsyncImage as inst -> inst.WriteToDirect(stream, imageType, quality, optimize, CancellationToken.None).GetAwaiter().GetResult()
    | _                     -> this.AsyncWriteTo (stream, imageType, quality, optimize) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member WriteToAsync (this : IImage, stream, imageType, [<Optional; DefaultParameterValue(85)>] quality, [<Optional; DefaultParameterValue(true)>] optimize, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncImage as inst -> inst.WriteToDirect (stream, imageType, quality, optimize, cancellationToken)
    | _                     -> Async.StartAsTask (this.AsyncWriteTo (stream, imageType, quality, optimize), cancellationToken = cancellationToken) :> _

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member SaveTo (this : IDirectImage, path, imageType, [<Optional; DefaultParameterValue(85)>] quality, [<Optional; DefaultParameterValue(true)>] optimize) =
    match this with
    | :? AsyncDirectImage as inst -> inst.SaveToDirect(path, imageType, quality, optimize, CancellationToken.None).GetAwaiter().GetResult ()
    | _                           -> this.AsyncSaveTo (path, imageType, quality, optimize) |> Async.RunSynchronously

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member SaveToAsync (this : IDirectImage, path, imageType, [<Optional; DefaultParameterValue(85)>] quality, [<Optional; DefaultParameterValue(true)>] optimize, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncDirectImage as inst -> inst.SaveToDirect (path, imageType, quality, optimize, cancellationToken)
    | _                           -> Async.StartAsTask (this.AsyncSaveTo (path, imageType, quality, optimize), cancellationToken = cancellationToken) :> _

// ********************************************************************************************************************
// Resizing

type
  [<Interface>]
  IResizer =
    abstract Resize : image:IImage -> unit

type
  [<Interface>]
  IResizerFactory =
    abstract CreateResizer : image:IImage * options:ResizeOptions -> IResizer

type
  [<Sealed>]
  NoneResizerFactory () =
    static let resizer =
      { new IResizer with
          member __.Resize _ = ()
      }
    interface IResizerFactory with
      member __.CreateResizer (_, _) = resizer

type
  [<Sealed>]
  ExactResizerFactory () =
    interface IResizerFactory with
      member __.CreateResizer (image, options) =
        let box =
          match options.Width.HasValue, options.Height.HasValue with
          | false, false -> invalidOp "Output image dimensions must be specified when using exact resizing."
          | true,  false ->
            let imageSize = image.Size
            { Width  = options.Width.Value
              Height = int (double imageSize.Height / double imageSize.Width * double options.Width.Value) }
          | false, true ->
            let imageSize = image.Size
            { Width  = int (double imageSize.Width / double imageSize.Height * double options.Height.Value)
              Height = options.Height.Value }
          | _ -> { Width = options.Width.Value; Height = options.Height.Value }
        { new IResizer with member __.Resize image = image.Resize box }

type
  [<Sealed>]
  InboxResizerFactory () =
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static let calculateRectangle (weightX : Nullable<int>) (weightY : Nullable<int>) (source : Size) (box : Size) =
      let resizeWidthRatio  = double source.Width  / double box.Width
      let resizeHeightRatio = double source.Height / double box.Height
      match resizeWidthRatio < resizeHeightRatio with
      | true ->
        // maximize width in box
        let inputHeight = int (double box.Height / double box.Width * double source.Width)
        let mutable margin = (source.Height - inputHeight) / 2
        if weightY.HasValue then
          let diff = double weightY.Value - double source.Height / 2.0;
          let normalized = diff / (double source.Height / 2.0);
          let mul = 1.0 + normalized;
          margin <- int (double margin * mul)
        { Point = { X = 0; Y = margin }; Size = { Width = source.Width; Height = inputHeight } }
      | _ ->
        let inputWidth = int (double box.Width / double box.Height * double source.Height)
        let mutable margin = (source.Width - inputWidth) / 2
        if weightX.HasValue then
          let diff = double weightX.Value - double source.Width / 2.0;
          let normalized = diff / (double source.Width / 2.0)
          let mul = 1.0 + normalized
          margin <- int (double margin * mul);
        { Point = { X = margin; Y = 0 }; Size = { Width = inputWidth; Height = source.Height } }
    interface IResizerFactory with
      member __.CreateResizer (_, options) =
        if not (options.Width.HasValue && options.Height.HasValue) then
          invalidOp "Exact image dimensions must be specified when using inbox resizing."
        let box = { Width = options.Width.Value; Height = options.Height.Value }
        { new IResizer with
            member __.Resize image =
              let inputRect = calculateRectangle options.WeightX options.WeightY image.Size box
              image.Crop inputRect
              image.Resize box
        }

type
  [<Extension>]
  [<Sealed; AbstractClass>]
  ImageResizeExtensions =

    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Resize (this : IImage, resizer : IResizer) = resizer.Resize this
