namespace NCoreUtils.Images.Resizers

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open NCoreUtils.Images
open NCoreUtils.Images.Providers

// ********************************************************************************************************************
// Resizing

type IResizer =
  abstract Resize : image:IImage -> unit

type IResizerFactory =
  abstract CreateResizer : image:IImage * options:IResizeOptions -> IResizer

type IResizerCollection =
  inherit IReadOnlyDictionary<string, IResizerFactory>

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
