namespace NCoreUtils.Images

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open NCoreUtils
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

type ImageResizerClientConfiguration () =
  member val EndPoints = Array.empty<string> with get, set

[<AutoOpen>]
module private ImageResizerClientHelpers =
  [<Literal>]
  let FailedMessage = "Failed to perform resize operation"

  let serializerSettings = JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ())

  let inline private appends first (name : string) (value : string) (builder : StringBuilder) =
    match isNull value with
    | true -> builder
    | _ ->
      match !first with
      | true ->
        first := false
        builder.AppendFormat ("?{0}={1}", name, Uri.EscapeDataString value)
      | _ ->
        builder.AppendFormat ("&{0}={1}", name, Uri.EscapeDataString value)

  let inline private appendn first (name : string) (value : Nullable<_>) (builder : StringBuilder) =
    match value.HasValue with
    | false -> builder
    | _ ->
      match !first with
      | true ->
        first := false
        builder.AppendFormat("?{0}={1}", name, Uri.EscapeDataString <| value.Value.ToString ())
      | _ ->
        builder.AppendFormat("&{0}={1}", name, Uri.EscapeDataString <| value.Value.ToString ())


  let inline private builderToString (builder : StringBuilder) = builder.ToString ()

  let createQueryString (options : ResizeOptions) =
    let first = ref true
    StringBuilder ()
    |> appends first "t"  options.ImageType
    |> appendn first "w"  options.Width
    |> appendn first "h"  options.Height
    |> appends first "m"  options.ResizeMode
    |> appendn first "q"  options.Quality
    |> appendn first "x"  options.Optimize
    |> appendn first "cx" options.WeightX
    |> appendn first "cy" options.WeightY
    |> builderToString

  // FIXME: use unified solution
  let toByteArray (stream : Stream) =
    match stream with
    | :? MemoryStream as memoryStream -> memoryStream.ToArray ()
    | _ ->
      use memoryStream = new MemoryStream ()
      stream.CopyTo memoryStream
      memoryStream.ToArray ()


[<Serializable>]
type ImageResizerClientException =
  inherit ImageResizerException
  new (message) = { inherit ImageResizerException (message) }
  new (message : string, innerException) = { inherit ImageResizerException (message, innerException) }
  new (info : System.Runtime.Serialization.SerializationInfo, context) = { inherit ImageResizerException (info, context) }

type ImageResizerClient =
  val private configuration : ImageResizerClientConfiguration
  val private logger        : ILog
  new (configuration, logger : ILog<ImageResizerClient>) =
    { configuration = configuration
      logger        = logger }
  member this.AsyncResize (source : Stream, destination : IImageDestination, options : ResizeOptions) =
    let queryString = createQueryString options
    let data = toByteArray source
    let rec invoke i exns =
      match i = this.configuration.EndPoints.Length with
      | true -> async.Return (false, [])
      | _    ->
        let endpoint = this.configuration.EndPoints.[i]
        let uri = UriBuilder(endpoint, Query = queryString).Uri
        async {
          let! result = async {
            try
              use memoryStream = new MemoryStream (data, false)
              use requestMessageContent = new StreamContent (memoryStream)
              use requestMessage = new HttpRequestMessage (HttpMethod.Post, uri, Content = requestMessageContent)
              requestMessageContent.Headers.ContentType <- MediaTypeHeaderValue.Parse "application/octet-stream"
              use httpClient = new HttpClient ()
              use! responseMessage = httpClient.AsyncSend (requestMessage, HttpCompletionOption.ResponseHeadersRead)
              match responseMessage.StatusCode with
              | HttpStatusCode.OK ->
                match responseMessage.Content with
                | null ->
                  this.logger.LogWarning (null, sprintf "Image server responded with no content (uri = %s)." uri.AbsoluteUri)
                  return (false, exns)
                | content ->
                  let length = content.Headers.ContentLength
                  let ctype  = content.Headers.ContentType |> Option.wrap >>| (fun x -> x.ToString ()) |> Option.defaultValue null
                  do! destination.AsyncWrite (ContentInfo (length, ctype), fun stream -> content.AsyncReadAsStream () >>= Stream.asyncCopyTo stream)
                  return (true, [])
              | HttpStatusCode.BadRequest ->
                this.logger.LogWarning (null, sprintf "Image server responded with bad request (uri = %s)." uri.AbsoluteUri)
                return (false, exns)
              | HttpStatusCode.NotImplemented ->
                this.logger.LogWarning (null, sprintf "Feature not implmented on image server (uri = %s)." uri.AbsoluteUri);
                return (false, exns)
              | statusCode ->
                this.logger.LogWarning (null, sprintf "Image server responded with %A (uri = %s)." statusCode uri.AbsoluteUri)
                return (false, exns)
            with exn ->
              this.logger.LogError (exn, sprintf "Exception thrown while resizing image (uri = %s)." uri.AbsoluteUri)
              return (false, exn :: exns) }
          match result with
          | true, _   -> return  (true, [])
          | _, errors -> return! invoke (i + 1) errors }
    async {
      let! result = invoke 0 []
      match result with
      | true, _ -> ()
      | _, []   -> ImageResizerClientException  ImageResizerClientHelpers.FailedMessage                           |> raise
      | _, exns -> ImageResizerClientException (ImageResizerClientHelpers.FailedMessage, AggregateException exns) |> raise }
  member this.AsyncGetImageInfo (source : Stream) =
    let data = toByteArray source
    let rec invoke i exns =
      match i = this.configuration.EndPoints.Length with
      | true -> async.Return (None, [])
      | _    ->
        let endpoint = this.configuration.EndPoints.[i]
        let uriBuilder = UriBuilder endpoint;
        uriBuilder.Path <- if System.String.IsNullOrEmpty uriBuilder.Path then "info" else (if uriBuilder.Path.EndsWith "/" then uriBuilder.Path + "info" else uriBuilder.Path + "/info")
        let uri = uriBuilder.Uri
        async {
          let! result = async {
            try
              use memoryStream = new MemoryStream (data, false)
              use requestMessageContent = new StreamContent (memoryStream)
              use requestMessage = new HttpRequestMessage (HttpMethod.Post, uri, Content = requestMessageContent)
              requestMessageContent.Headers.ContentType <- MediaTypeHeaderValue.Parse "application/octet-stream"
              use httpClient = new HttpClient ()
              use! responseMessage = httpClient.AsyncSend (requestMessage, HttpCompletionOption.ResponseHeadersRead)
              match responseMessage.StatusCode with
              | HttpStatusCode.OK ->
                match responseMessage.Content with
                | null ->
                  this.logger.LogWarning (null, sprintf "Image server responded with no content (uri = %s)." uri.AbsoluteUri)
                  return (None, exns)
                | content ->
                  let! json = content.AsyncReadAsString ()
                  this.logger.LogDebug (null, sprintf "Got JSON reponse (uri = %s): %s" uri.AbsoluteUri json)
                  let  info = JsonConvert.DeserializeObject<ImageInfo> (json, serializerSettings)
                  return (Some info, [])
              | HttpStatusCode.BadRequest ->
                this.logger.LogWarning (null, sprintf "Image server responded with bad request (uri = %s)." uri.AbsoluteUri)
                return (None, exns)
              | HttpStatusCode.NotImplemented ->
                this.logger.LogWarning (null, sprintf "Feature not implmented on image server (uri = %s)." uri.AbsoluteUri);
                return (None, exns)
              | statusCode ->
                this.logger.LogWarning (null, sprintf "Image server responded with %A (uri = %s)." statusCode uri.AbsoluteUri)
                return (None, exns)
            with exn ->
              this.logger.LogError (exn, sprintf "Exception thrown while getting image information (uri = %s)." uri.AbsoluteUri)
              return (None, exn :: exns) }
          match result with
          | Some _ as info, _   -> return  (info, [])
          | _, errors           -> return! invoke (i + 1) errors }
    async {
      let! result = invoke 0 []
      return
        match result with
        | Some info, _ -> info
        | _, []        -> ImageResizerClientException  ImageResizerClientHelpers.FailedMessage                           |> raise
        | _, exns      -> ImageResizerClientException (ImageResizerClientHelpers.FailedMessage, AggregateException exns) |> raise }
  interface IImageResizer with
    member this.AsyncResize (source, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncResize (path : string, destination, options) = async {
      use buffer = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous ||| FileOptions.SequentialScan)
      do! this.AsyncResize (buffer, destination, options) }
    member this.AsyncGetImageInfo source = this.AsyncGetImageInfo source
