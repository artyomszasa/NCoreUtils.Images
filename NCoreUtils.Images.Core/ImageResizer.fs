namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Serialization
open NCoreUtils
open NCoreUtils.Images.Internal
open System.IO

module private InvalidResizeModeExceptionHelpers =
  [<Literal>]
  let KeyMode = "ResizeMode"

type InvalidResizeModeException =
  inherit ImageResizerException
  val private mode : string
  new (mode : string) =
    { inherit ImageResizerException (ImageResizerError.invalidMode mode)
      mode = mode }
  new (info : SerializationInfo, context) =
    { inherit ImageResizerException (info, context)
      mode = info.GetString InvalidResizeModeExceptionHelpers.KeyMode }
  member this.ResizeMode with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.mode
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (InvalidResizeModeExceptionHelpers.KeyMode, this.mode)

module private InvalidImageTypeExceptionHelpers =
  [<Literal>]
  let KeyImageType = "ImageType"

type InvalidImageTypeException =
  inherit ImageResizerException
  val private imageType : string
  new (imageType : string) =
    { inherit ImageResizerException (ImageResizerError.invalidImageType imageType)
      imageType = imageType }
  new (info : SerializationInfo, context) =
    { inherit ImageResizerException (info, context)
      imageType = info.GetString InvalidImageTypeExceptionHelpers.KeyImageType }
  member this.ImageType with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = this.ImageType
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (InvalidImageTypeExceptionHelpers.KeyImageType, this.imageType)

[<AutoOpen>]
module private ImageResizerHelpers =

  let inline private mkTemp (ext : string) = sprintf "%s%s.%s" (Path.GetTempPath ()) (System.Guid.NewGuid().ToString()) ext;

  let asyncTempFile (ext : string) action = async {
    let path = mkTemp ext
    try return! action path
    finally try if File.Exists path then File.Delete path with _ -> () }

  let inline getResult (res : Result<_, ImageResizerError>) =
    match res with
    | Ok result -> result
    | Error err -> err.RaiseException ()

  let inline decideImageType (image : IImage) (options : ResizeOptions) =
    match options.ImageType with
    | null  -> Ok image.ImageType
    | itype ->
    match ImageType.ofExtension itype with
    | ImageType.Other -> itype |> ImageResizerError.invalidImageType :> ImageResizerError |> Error
    | imageType       -> Ok imageType

type ImageResizer =
  val private provider      : IImageProvider
  val private resizers      : ResizerCollection
  val private options       : IImageResizerOptions
  val private logger        : ILog
  val private optimizations : seq<IImageOptimization>

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new  (provider : IImageProvider,
        resizers,
        logger : ILog<ImageResizer>,
        [<Optional; DefaultParameterValue(null:IImageResizerOptions)>] options : IImageResizerOptions,
        [<Optional; DefaultParameterValue(null:seq<IImageOptimization>)>] optimizations : seq<IImageOptimization>) =
    let opts = if isNull options then (ImageResizerOptions.Default :> IImageResizerOptions) else options
    if opts.MemoryLimit.HasValue then
      provider.MemoryLimit <- opts.MemoryLimit.Value
    { provider      = provider
      resizers      = resizers
      options       = opts
      logger        = logger
      optimizations = optimizations }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member private this.TryGetOptimizationsFor shouldOptimize imageType =
    match shouldOptimize with
    | false -> []
    | _ ->
      let ext = ImageType.toExtension imageType
      let inline supports (o : IImageOptimization) = o.Supports ext
      this.optimizations
      |> Seq.filter supports
      |> Seq.toList

  member inline private this.DecideQuality (options : ResizeOptions) imageType =
    match options.Quality.HasValue with
    | true -> options.Quality.Value
    | _    -> this.options.Quality imageType

  member inline private this.DecideOptimize (options : ResizeOptions) imageType =
    match options.Optimize.HasValue with
    | true -> options.Optimize.Value
    | _    -> this.options.Optimize imageType

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.DoAsyncResResize (source : unit -> Async<Result<IImage, _>>, destination : IImageDestination, options : ResizeOptions) = async {
    this.logger.LogDebug (null, sprintf "Processing options = %A" options)
    let resizeMode = options.ResizeMode |?? "none"
    match this.resizers.TryGetValue resizeMode with
    | None -> return ImageResizerError.invalidMode resizeMode :> ImageResizerError |> Error
    | Some factory ->
      let! image0 = source ()
      match image0 with
      | Error err -> return Error err
      | Ok image ->
        try
          match decideImageType image options with
          | Error err -> return Error err
          | Ok imageType ->
            let resizer = factory.CreateResizer (image, options)
            image.Resize resizer
            let quality        = this.DecideQuality options imageType
            let shouldOptimize = this.DecideOptimize options imageType
            match this.TryGetOptimizationsFor shouldOptimize imageType with
            | [] ->
              do! destination.AsyncWrite (ContentInfo (contentType = ImageType.toMediaType imageType), fun stream -> image.AsyncWriteTo (stream, imageType, quality))
              this.logger.LogDebug (null, sprintf "Successfully created image of type %A" imageType)
              return Ok ()
            | optimizations ->
              this.logger.LogDebug (null, sprintf "Running optimizations for type %A" imageType)
              let! originalData = async {
                use buffer = new MemoryStream ()
                do! image.AsyncWriteTo (buffer, imageType, quality)
                return buffer.ToArray () }
              let rec apply data optimizations = async {
                match optimizations with
                | [] -> return data
                | (optimization : IImageOptimization) :: optimizations' ->
                  let! res = optimization.AsyncResOptimize data
                  return!
                    match res with
                    | Ok    data' -> apply data' optimizations'
                    | Error err   ->
                      this.logger.LogDebug (null, sprintf " (%s) %s: %s" err.Optimizer err.Message err.Description)
                      apply data optimizations' }
              let! finalData = apply originalData optimizations
              do! destination.AsyncWrite (
                    ContentInfo (contentType = ImageType.toMediaType imageType, contentLength = Nullable.mk finalData.LongLength),
                    fun stream -> stream.AsyncWrite finalData)
              this.logger.LogDebug (null, sprintf "Successfully created optimized image of type %A" imageType)
              return Ok ()
        finally image.Dispose () }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncResResize (source : Stream, destination : IImageDestination, options : ResizeOptions) =
    this.DoAsyncResResize ((fun () -> this.provider.AsyncResFromStream source), destination, options)

  member this.AsyncResResize (source : string, destination : IImageDestination, options : ResizeOptions) =
    match this.provider with
    | :? IDirectImageProvider as dprovider ->
      this.DoAsyncResResize ((fun () -> dprovider.AsyncResFromPath source), destination, options)
    | provider -> async {
      use bufferStream = new FileStream (source, FileMode.Open, FileAccess.Read, FileShare.Read)
      return! this.DoAsyncResResize ((fun () -> provider.AsyncResFromStream bufferStream), destination, options) }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncResize (source : Stream, destination : IImageDestination, options : ResizeOptions) =
    this.AsyncResResize (source, destination, options)
    >>| getResult

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncResize (source : string, destination : IImageDestination, options : ResizeOptions) =
    this.AsyncResResize (source, destination, options)
    >>| getResult

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncGetImageInfo stream = async {
    this.logger.LogDebug (null, "Getting image information.")
    use! image = this.provider.AsyncFromStream stream
    let info = image.GetImageInfo ()
    printfn "%A" info
    this.logger.LogDebug (null, "Successfully retrieved image information.")
    return info }

  interface IImageResizer with
    member this.AsyncResize (source : string, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncResize (source : Stream, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncResResize (source : string, destination, options) = this.AsyncResResize (source, destination, options)
    member this.AsyncResResize (source : Stream, destination, options) = this.AsyncResResize (source, destination, options)
    member this.AsyncGetImageInfo stream = this.AsyncGetImageInfo stream