namespace NCoreUtils.Images.ImageMagick

open System
open System.Collections.Generic
open System.IO
open System.Runtime.CompilerServices
open System.Threading
open ImageMagick
open NCoreUtils
open NCoreUtils.Images
open NCoreUtils.Images.Internal

[<CompiledName("RectangleHelpers")>]
[<RequireQualifiedAccess>]
module private Rectangle =

  let inline toMagickGeometry (rect : Rectangle) =
    MagickGeometry (rect.X, rect.Y, rect.Width, rect.Height)

[<AutoOpen>]
module private ImageHelpers =

  let private magickFormatMap =
    Map.ofList
      [ MagickFormat.Jpg,    ImageType.Jpeg
        MagickFormat.Jpeg,   ImageType.Jpeg
        MagickFormat.Png,    ImageType.Png
        MagickFormat.Png00,  ImageType.Png
        MagickFormat.Png8,   ImageType.Png
        MagickFormat.Png24,  ImageType.Png
        MagickFormat.Png32,  ImageType.Png
        MagickFormat.Png48,  ImageType.Png
        MagickFormat.Png64,  ImageType.Png
        MagickFormat.Bmp,    ImageType.Bmp
        MagickFormat.Bmp2,   ImageType.Bmp
        MagickFormat.Bmp3,   ImageType.Bmp
        MagickFormat.Gif,    ImageType.Gif
        MagickFormat.Gif87,  ImageType.Gif
        MagickFormat.Tiff64, ImageType.Tiff
        MagickFormat.Tiff,   ImageType.Tiff
        MagickFormat.WebP,   ImageType.WebP ]

  let private imageTypeMap =
    Map.ofList
      [ ImageType.Jpeg, MagickFormat.Jpg
        ImageType.Png, MagickFormat.Png
        ImageType.Bmp, MagickFormat.Bmp
        ImageType.Gif, MagickFormat.Gif87
        ImageType.Tiff, MagickFormat.Tiff
        ImageType.WebP, MagickFormat.WebP ]

  let magickFormatToImageType format =
    match Map.tryFind format magickFormatMap with
    | Some imageType -> imageType
    | _              -> format.ToString ()

  let imageTypeToMagickFormat imageType =
    match Map.tryFind imageType imageTypeMap with
    | Some magickFormat -> magickFormat
    | _                 -> invalidArgf "imageType" "Not valid image type (%A)" imageType

  let inline toString (x : ^a) = x.ToString ()

  let stringify (o : obj) =
    match o with
    | null -> null
    | :? string as s -> s
    | :? (int64[])  as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "i64[%s]"
    | :? (int32[])  as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "i32[%s]"
    | :? (int16[])  as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "i16[%s]"
    | :? (int8[])   as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "i8[%s]"
    | :? (uint64[]) as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "u64[%s]"
    | :? (uint32[]) as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "u32[%s]"
    | :? (uint16[]) as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "u16[%s]"
    | :? (uint8[])  as ix -> ix |> Seq.map toString |> String.concat "," |> sprintf "u8[%s]"
    | :? Guid as guid -> guid.ToString ()
    | :? Rational as r -> sprintf "f64[%f]" (float r.Numerator/ float r.Denominator)
    | :? (Rational[]) as ra -> ra |> Seq.map (fun r ->  float r.Numerator / float r.Denominator |> sprintf "%f") |> String.concat "," |> sprintf "f64[%s]"
    | _ -> null

[<AutoOpen>]
module private AsyncHelpers =

  let inline asAsync factory =
    Async.FromContinuations (fun (succ, err, _) -> try succ (factory ()) with exn -> err exn)

type ImageProvider () =
  /// <summary>
  /// Creates <c>Image</c> object from the specified stream.
  /// </summary>
  /// <param name="stream">Stream to create an image from.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.FromStream (stream : Stream) =
    new Image (new MagickImage (stream), this)

  /// <summary>
  /// Creates <c>Image</c> object from the specified path.
  /// </summary>
  /// <param name="path">Path to create an image from.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.FromPath (path: string) =
    new Image (new MagickImage (path), this)

  interface IImageProvider with
    member __.MemoryLimit
      with get ()    = int64 ResourceLimits.Memory
      and  set limit =
        ResourceLimits.Memory <- uint64 limit
        printfn "ResourceLimits.Memory = %d" ResourceLimits.Memory
        printfn "ResourceLimits.Disk = %d" ResourceLimits.Disk
        printfn "ResourceLimits.Thread = %d" ResourceLimits.Thread

    member this.AsyncFromStream stream =
      asAsync (fun () -> this.FromStream stream :> IImage)

  interface IDirectImageProvider with
    member this.AsyncFromPath path =
      asAsync (fun () -> this.FromPath path :> IImage)

and
  [<Sealed>]
  Image =
    val mutable private isDisposed : int
    val private provider   : ImageProvider
    val private native     : MagickImage
    val private size       : Size
    val private imageType  : string
    internal new (native : MagickImage, provider : ImageProvider) =
      { isDisposed = 0
        provider   = provider
        native     = native
        size       = { Width = native.Width; Height = native.Height }
        imageType  = magickFormatToImageType native.Format }
    member private this.IsDisposed with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = 0 <> this.isDisposed
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member private this.ThrowIfDisposed () = if this.IsDisposed then ObjectDisposedException "Image" |> raise
    member this.Provider with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.provider
    member this.Size with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.size
    member this.ImageType with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.imageType
    member this.WriteTo (stream : Stream, imageType : string, quality, optimize) =
      this.ThrowIfDisposed ()
      this.native.Quality <- quality
      if optimize then
        this.native.Strip ()
        match imageType with
        | ImageType.Jpeg ->
          this.native.Interlace <- Interlace.Jpeg
          this.native.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0")
        | ImageType.Png ->
          this.native.Interlace <- Interlace.Png
        | ImageType.Gif ->
          this.native.Interlace <- Interlace.Gif
        | _ -> ()
      this.native.Write(stream, imageTypeToMagickFormat imageType)
    member this.SaveTo (path : string, imageType, quality, optimize) =
      this.ThrowIfDisposed ()
      use stream = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.None)
      this.WriteTo (stream, imageType, quality, optimize)
    member this.Resize size =
      this.ThrowIfDisposed ()
      this.native.Resize (size.Width, size.Height)
    member this.Crop (rect : Rectangle) =
      this.ThrowIfDisposed ()
      rect
        |> Rectangle.toMagickGeometry
        |> this.native.Crop
      this.native.RePage ()
    member this.GetImageInfo () : ImageInfo =
      this.ThrowIfDisposed ()
      let iptc =
        try
          let iptcBuilder = Dictionary ()
          let iptc = this.native.GetIptcProfile ()
          if not (isNull iptc) then
            for value in iptc.Values do
              let tag = value.Tag.ToString ()
              if not (iptcBuilder.ContainsKey tag) then
                iptcBuilder.Add (tag, value.Value)
          iptcBuilder :> IReadOnlyDictionary<_, _>
        with exn ->
          // FIXME: log
          eprintfn "%A" exn
          Dictionary () :> _
      let exif =
        try
          let exif = this.native.GetExifProfile ()
          let exifBuilder = Dictionary ()
          if not (isNull exif) then
            for value in exif.Values do
              match stringify (value.GetValue ()) with
              | null -> ()
              | v ->
                let tag = value.Tag.ToString ()
                if not (exifBuilder.ContainsKey tag) then
                  exifBuilder.Add (tag, v)
          exifBuilder :> IReadOnlyDictionary<_, _>
        with exn ->
          // FIXME: log
          eprintfn "%A" exn
          Dictionary () :> _
      let struct (xResolution, yResolution) =
        let density = this.native.Density
        match density.Units with
        | DensityUnit.PixelsPerCentimeter -> struct (int (density.X * 2.54), int (density.Y * 2.54))
        | _                               -> struct (int density.X, int density.Y)
      { Width       = this.size.Width
        Height      = this.Size.Height
        XResolution = xResolution
        YResolution = yResolution
        Iptc        = iptc
        Exif        = exif }

    interface IImage with
      member this.Provider        = this.provider :> _
      member this.Size            = this.size
      member this.NativeImageType = box this.native.Format
      member this.ImageType       = this.imageType
      member this.AsyncWriteTo (stream, imageType, quality, optimize) =
        asAsync (fun () -> this.WriteTo (stream, imageType, quality, optimize))
      member this.Resize size     = this.Resize size
      member this.Crop rect       = this.Crop rect
      member this.GetImageInfo () = this.GetImageInfo ()
      member this.Dispose () =
        if 0 = Interlocked.CompareExchange (&this.isDisposed, 1, 0) then
          this.native.Dispose ()

    interface IDirectImage with
      member this.AsyncSaveTo (path, imageType, quality, optimize) =
        asAsync (fun () -> this.SaveTo (path, imageType, quality, optimize))
