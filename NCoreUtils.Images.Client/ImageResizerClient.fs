namespace NCoreUtils.Images

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Runtime.ExceptionServices
open System.Runtime.InteropServices
open System.Text
open NCoreUtils
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.Runtime.CompilerServices
open NCoreUtils.IO
open System.Collections.Generic
open System.Collections.Immutable

type private SerializationInfo = System.Runtime.Serialization.SerializationInfo
type private SerializationException = System.Runtime.Serialization.SerializationException

type ImageResizerClientConfiguration () =
  member val EndPoints = Array.empty<string> with get, set

[<Serializable>]
type RemoteImageResizerError =
  inherit ImageResizerError
  val mutable private uri : string
  internal new (message, description, uri) =
    { inherit ImageResizerError (message, description)
      uri = uri }
  private new () = RemoteImageResizerError (null, null, null)
  member this.Uri = this.uri
  member private this.Uri with set uri = this.uri <- uri
  override this.RaiseException () = RemoteImageResizerException this |> raise

and
  [<Serializable>]
  RemoteImageResizerStatusCodeError =
    inherit RemoteImageResizerError
    val mutable private statusCode : int
    internal new (message, description, uri, statusCode) =
      { inherit RemoteImageResizerError (message, description, uri)
        statusCode = statusCode }
    private new () = RemoteImageResizerStatusCodeError (null, null, null, 0)
    member this.StatusCode = this.statusCode
    member private this.StatusCode with set statusCode = this.statusCode <- statusCode
    override this.RaiseException () = RemoteImageResizerStatusCodeException this |> raise

and
  [<Serializable>]
  RemoteImageResizerClrError =
    inherit RemoteImageResizerError
    val mutable private innerException : exn
    internal new (message, description, uri, innerException) =
      { inherit RemoteImageResizerError (message, description, uri)
        innerException = innerException }
    private new () = RemoteImageResizerClrError (null, null, null, null)
    member this.InnerException = this.innerException
    member private this.InnerException with set innerException = this.innerException <- innerException
    override this.RaiseException () = RemoteImageResizerException (this, this.InnerException) |> raise

and
  [<Serializable>]
  RemoteImageResizerException =
    inherit ImageResizerException
    new (error : RemoteImageResizerError) = { inherit ImageResizerException (error) }
    new (error : RemoteImageResizerError, innerException) = { inherit ImageResizerException (error, innerException) }
    new (info : SerializationInfo, context) = { inherit ImageResizerException (info, context) }
    member this.RequestUri = (this.Error :?> RemoteImageResizerError).Uri

and
  [<Serializable>]
  RemoteImageResizerStatusCodeException =
    inherit RemoteImageResizerException
    new (error : RemoteImageResizerStatusCodeError) = { inherit RemoteImageResizerException (error) }
    new (error : RemoteImageResizerStatusCodeError, innerException) = { inherit RemoteImageResizerException (error, innerException) }
    new (info : SerializationInfo, context) = { inherit RemoteImageResizerException (info, context) }
    member this.StatusCode = (this.Error :?> RemoteImageResizerStatusCodeError).StatusCode

[<AutoOpen>]
module private ImageResizerClientHelpers =
  let genericRemoteError uri statusCode =
    RemoteImageResizerStatusCodeError (
      message     = sprintf "Server responded with %d (uri = %s)." statusCode uri,
      description = "Failed to perform remote image resize operation due to unknown error.",
      uri         = uri,
      statusCode  = statusCode)

  let clrRemoteError uri (exn : exn) =
    RemoteImageResizerClrError (
      message        = sprintf "Exception has been raised while performing operation (uri = %s)." uri,
      description    = exn.Message,
      uri            = uri,
      innerException = exn)

  let emptyResponseError uri =
    RemoteImageResizerError (
      message     = sprintf "Empty response (uri = %s)." uri,
      description = "Remote server reported no errors but sent no data.",
      uri         = uri)

  let invalidResponseError uri =
    RemoteImageResizerError (
      message     = sprintf "Invalid response (uri = %s)." uri,
      description = "Remote server reported no errors but sent invalid data.",
      uri         = uri)

  let noEndpointsError =
    ImageResizerError (
      message = "No endpoints provided.",
      description = "No endpoints provided in current configuration.")

  let infoSerializerSettings = JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ())

  let errorSerializerSettings =
    let settings = JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ())
    settings.Converters.Insert (0, ImageResizerErrorConverter ())
    settings

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

  let toByteArrayAsync (source : IStreamProducer) =
    let consumer =
      { new IStreamConsumer<byte[]> with
          member __.AsyncConsume stream =
            match stream with
            | :? MemoryStream as memoryStream -> memoryStream.ToArray () |> async.Return
            | _ ->
              async {
                use memoryStream = new MemoryStream ()
                do! Stream.asyncCopyTo memoryStream stream
                return memoryStream.ToArray () }
          member __.Dispose () = ()
      }
    StreamToResultConsumer.asyncConsumeProducer source consumer


  [<Struct>]
  type ResponseInfo =
    | Succeeded
    | Retry
    | Failed
    | FailedWithError of Error:ImageResizerError

  let asyncCheck (resp : HttpResponseMessage) =
    match resp.StatusCode with
    | HttpStatusCode.OK -> async.Return Succeeded
    | HttpStatusCode.BadRequest ->
      match resp.Content with
      | null -> async.Return Failed
      | content ->
      match content.Headers.ContentType with
      | null -> async.Return Failed
      | contentType ->
      match contentType.MediaType with
      | EQI "application/json"
      | EQI "text/json" ->
        async {
          try
            let! json  = content.AsyncReadAsString ()
            let  error = JsonConvert.DeserializeObject<ImageResizerError> (json, errorSerializerSettings)
            return
              match box error with
              | null -> Failed
              | _    -> FailedWithError error
          with _ -> return Failed }
      | _ -> async.Return Failed
    | _ -> async.Return Failed

  let inline getResult res =
    match res with
    | Ok res -> res
    | Error (error : ImageResizerError) -> error.RaiseException ()

  let rec private isBrokenPipe (exn : exn) =
    match exn with
    | :? Sockets.SocketException as socketExn ->
      match socketExn.Message.IndexOf ("broken pipe", StringComparison.OrdinalIgnoreCase) with
      | -1 -> None
      | _  -> Some ()
    | :? AggregateException as aggregateExn -> aggregateExn.InnerExceptions |> Seq.tryPick isBrokenPipe
    | _ ->
    match exn.InnerException with
    | null     -> None
    | innerExn -> isBrokenPipe innerExn

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let internal (|IsBrokenPipe|_|) exn = isBrokenPipe exn

[<Struct>]
type private ErrorDesc =
  | TryNext of LastError:ImageResizerError
  | StopWithError of Error:ImageResizerError

type ImageResizerClient =
  val private configuration     : ImageResizerClientConfiguration
  val private logger            : ILog
  val private httpClientFactory : IHttpClientFactoryAdapter
  new (configuration, logger : ILog<ImageResizerClient>, [<Optional; DefaultParameterValue(null:IHttpClientFactoryAdapter)>] httpClientFactory : IHttpClientFactoryAdapter) =
    { configuration     = configuration
      logger            = logger
      httpClientFactory = httpClientFactory }

  member private this.AsyncGetCapabilities (endpoint : string) = async {
    let uriBuilder = UriBuilder endpoint
    uriBuilder.Path <- if System.String.IsNullOrEmpty uriBuilder.Path then "capabilities" else (if uriBuilder.Path.EndsWith "/" then uriBuilder.Path + "capabilities" else uriBuilder.Path + "/capabilities")
    let uri = uriBuilder.Uri
    try
      use  httpClient = if isNull this.httpClientFactory then new HttpClient () else this.httpClientFactory.CreateClient "ImageResizer.Resize"
      let! response   = Async.Adapt (fun _ -> httpClient.GetStringAsync uri)
      return JsonConvert.DeserializeObject<string[]> response |> Set.ofArray
    with _ ->
      return Set.empty }

  member private __.AsyncGetSourceData (source : IStreamProducer, capabilities) =
    match source with
    | :? ISerializableImageProducer as psource when Set.contains Capabilities.serializedImageInfo capabilities ->
      psource.AsyncSerialize ()
    | _ ->
      async {
        let! body = toByteArrayAsync source
        return
          { Body = body
            Info = null } }

  member private __.AsyncGetDestinationData (destination : IImageDestination, capabilities) =
    match destination with
    | :? ISerializableImageDestination as psource when Set.contains Capabilities.serializedImageInfo capabilities ->
      psource.AsyncSerialize ()
    | _ -> async.Return null

  member private this.AsyncResInvokeResize (source, destination : IImageDestination, queryString : string, endpoint : string) = async {
    let uri = UriBuilder(endpoint, Query = queryString).Uri
    try
      let! capabilities          = this.AsyncGetCapabilities endpoint
      let! sourceData            = this.AsyncGetSourceData (source, capabilities)
      let! destinationData       = this.AsyncGetDestinationData (destination, capabilities)
      let  body                  = if isNull sourceData.Body then Array.empty else sourceData.Body
      use  requestMessageContent = new StreamContent (new MemoryStream (body, 0, body.Length, false, true))
      use  requestMessage        = new HttpRequestMessage (HttpMethod.Post, uri, Content = requestMessageContent)
      requestMessageContent.Headers.ContentType <- MediaTypeHeaderValue.Parse "application/octet-stream"
      if not (isNull sourceData.Info) then
        for kv in sourceData.Info do
          requestMessage.Headers.Add (sprintf "X-%s" kv.Key, kv.Value)
      if not (isNull destinationData) then
        for kv in destinationData do
          requestMessage.Headers.Add (sprintf "X-%s" kv.Key, kv.Value)
      use  httpClient            = if isNull this.httpClientFactory then new HttpClient () else this.httpClientFactory.CreateClient "ImageResizer.Resize"
      use! responseMessage       = httpClient.AsyncSend (requestMessage, HttpCompletionOption.ResponseHeadersRead)
      let! status                = asyncCheck responseMessage
      match status with
      | Succeeded ->
        match responseMessage.Content with
        | null ->
          return (
            // If there is destination data, response may be empty.
            match destinationData with
            | null ->
              let error = emptyResponseError uri.AbsoluteUri
              Error (TryNext error)
            | _ -> Ok ())
        | content ->
          // If there is destination data, response is irrelevant
          if isNull destinationData then
            let length = content.Headers.ContentLength
            let ctype  = content.Headers.ContentType |> Option.ofObj >>| (fun x -> x.ToString ()) |> Option.defaultValue null
            do! destination.AsyncWrite (ContentInfo (length, ctype), fun stream -> content.AsyncReadAsStream () >>= Stream.asyncCopyTo stream)
          return Ok ()
      | FailedWithError error ->
        return
          match error with
          | :? InvalidArgumentError -> Error (StopWithError error)
          | _                       -> Error (TryNext error)
      | _ ->
        return Error (TryNext (genericRemoteError uri.AbsoluteUri (int responseMessage.StatusCode)))
    with
      | IsBrokenPipe as exn ->
        // retry on broken pipe...
        this.logger.LogWarning (exn, "Failed to perform operation due to connection error, retrying...")
        return! this.AsyncResInvokeResize (source, destination, queryString, endpoint)
      | exn ->
        return Error (TryNext (clrRemoteError uri.AbsoluteUri exn)) }

  member private this.AsyncResInvokeGetInfo (source, endpoint : string) = async {
    let uriBuilder = UriBuilder endpoint
    uriBuilder.Path <- if System.String.IsNullOrEmpty uriBuilder.Path then "info" else (if uriBuilder.Path.EndsWith "/" then uriBuilder.Path + "info" else uriBuilder.Path + "/info")
    let uri = uriBuilder.Uri
    try
      let! capabilities          = this.AsyncGetCapabilities endpoint
      let! requestData           = this.AsyncGetSourceData (source, capabilities)
      let  body                  = if isNull requestData.Body then Array.empty else requestData.Body
      use  requestMessageContent = new StreamContent (new MemoryStream (body, 0, body.Length, false, true))
      use  requestMessage        = new HttpRequestMessage (HttpMethod.Post, uri, Content = requestMessageContent)
      requestMessageContent.Headers.ContentType <- MediaTypeHeaderValue.Parse "application/octet-stream"
      if not (isNull requestData.Info) then
        for kv in requestData.Info do
          requestMessage.Headers.Add (sprintf "X-%s" kv.Key, kv.Value)
      use  httpClient            = if isNull this.httpClientFactory then new HttpClient () else this.httpClientFactory.CreateClient "ImageResizer.GetInfo"
      use! responseMessage       = httpClient.AsyncSend (requestMessage, HttpCompletionOption.ResponseHeadersRead)
      let! status                = asyncCheck responseMessage
      match status with
      | Succeeded ->
        match responseMessage.Content with
        | null ->
          let error = emptyResponseError uri.AbsoluteUri
          return Error (TryNext error)
        | content ->
          let! json = content.AsyncReadAsString ()
          let res =
            try
              this.logger.LogDebug (null, sprintf "Got JSON reponse (uri = %s): %s" uri.AbsoluteUri json)
              let  info = JsonConvert.DeserializeObject<ImageInfo> (json, infoSerializerSettings)
              Ok info
            with
              | :? SerializationException as exn ->
                this.logger.LogDebug (exn, sprintf "Failed to deserialize JSON response (uri = %s)." uri.AbsoluteUri)
                Error (TryNext (invalidResponseError uri.AbsoluteUri))
              | exn ->
                ExceptionDispatchInfo.Capture(exn).Throw ()
                Unchecked.defaultof<_>
          return res
      | FailedWithError error ->
        return
          match error with
          | :? InvalidArgumentError -> Error (StopWithError error)
          | _                       -> Error (TryNext error)
      | _ ->
        return Error (TryNext (genericRemoteError uri.AbsoluteUri (int responseMessage.StatusCode)))
    with exn -> return Error (TryNext (clrRemoteError uri.AbsoluteUri exn)) }

  member this.AsyncResResize (source : IStreamProducer, destination : IImageDestination, options : ResizeOptions) =
    let queryString = createQueryString options
    let rec doInvoke source lastErr endpoints = async {
      match endpoints with
      | [] -> return lastErr |> Option.defaultValue noEndpointsError |> Error
      | e::es ->
        let! res = this.AsyncResInvokeResize (source, destination, queryString, e)
        match res with
        | Ok () -> return Ok ()
        | Error (TryNext err) ->
          this.logger.LogWarning (null, err.Description)
          return! doInvoke source (Some err) es
        | Error (StopWithError err) -> return Error err }
    doInvoke source None (List.ofArray this.configuration.EndPoints)

  member this.AsyncResResize (source : string, destination, options) = async {
    use producer =
      new FileStream (source, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous ||| FileOptions.SequentialScan)
      |> StreamProducer.ofStream
    return! this.AsyncResResize (producer, destination, options) }

  member this.AsyncResize (producer : IStreamProducer, destination, options) =
    this.AsyncResResize (producer, destination, options)
    >>| getResult

  member this.AsyncResize (source : string, destination, options) =
    this.AsyncResResize (source, destination, options)
    >>| getResult

  member this.AsyncResGetImageInfo (source : IStreamProducer) =
    let rec doInvoke source lastErr endpoints = async {
      match endpoints with
      | [] -> return lastErr |> Option.defaultValue noEndpointsError |> Error
      | e::es ->
        let! res = this.AsyncResInvokeGetInfo (source, e)
        match res with
        | Ok info -> return Ok info
        | Error (TryNext err) ->
          this.logger.LogWarning (null, err.Description)
          return! doInvoke source (Some err) es
        | Error (StopWithError err) -> return Error err }
    doInvoke source None (List.ofArray this.configuration.EndPoints)

  member this.AsyncGetImageInfo (source : IStreamProducer) =
    this.AsyncResGetImageInfo source
    >>| getResult

  interface IImageResizer with
    member this.AsyncResize (source : IStreamProducer, destination, options) = this.AsyncResize (source, destination, options)
    member this.AsyncResResize (source : IStreamProducer, destination, options) = this.AsyncResResize (source, destination, options)
    member this.AsyncGetImageInfo source = this.AsyncGetImageInfo source
