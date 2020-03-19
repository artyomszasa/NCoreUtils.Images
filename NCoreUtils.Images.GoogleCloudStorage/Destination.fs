namespace NCoreUtils.Images
#nowarn "9"

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Runtime.ExceptionServices
open System.Runtime.InteropServices
open System.Text.Json
open Microsoft.FSharp.NativeInterop
open NCoreUtils
open NCoreUtils.Images.GoogleCloudStorage
open NCoreUtils.IO

[<AutoOpen>]
module private GoogleCloudStorageDestinationHelpers =

  [<CompiledName("JsonContentType")>]
  let jsonContentType = MediaTypeHeaderValue.Parse "application/json; charset=utf-8"

  let inline choose2 (option1: string) (option2: string) (fallback: string) =
    match String.IsNullOrEmpty option1 with
    | false -> option1
    | _ ->
    match String.IsNullOrEmpty option2 with
    | false -> option2
    | _     -> fallback

  let inline createInitEndpoint bucketName predefinedAcl =
    sprintf "https://www.googleapis.com/upload/storage/v1/b/%s/o?uploadType=resumable&predefinedAcl=%s" bucketName predefinedAcl

  let inline appends (first : byref<bool>) (key : string) value (builder : inref<SpanBuilder>) =
    match String.IsNullOrEmpty value with
    | false ->
      builder.Append (if first then '?' else '&')
      builder.Append key
      builder.Append '='
      builder.Append (Uri.EscapeDataString value)
      first <- false
    | _ -> ()

type GoogleCloudStorageDestination =
  inherit GoogleCloudStorageRecordDescriptor

  new (uri,
        credential,
        [<Optional; DefaultParameterValue(null : string)>] contentType,
        [<Optional; DefaultParameterValue(null : string)>] cacheControl,
        [<Optional; DefaultParameterValue(false)>] isPublic,
        [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory) =
    { inherit GoogleCloudStorageRecordDescriptor (uri, credential, contentType, cacheControl, isPublic, httpClientFactory) }

  new (descriptor : GoogleCloudStorageRecordDescriptor) =
    { inherit GoogleCloudStorageRecordDescriptor (descriptor.Uri, descriptor.Credential, descriptor.ContentType, descriptor.CacheControl, descriptor.IsPublic, descriptor.HttpClientFactory) }

  new (uri, [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory) =
    { inherit GoogleCloudStorageRecordDescriptor (uri, ViaDefault, Unchecked.defaultof<_>, Unchecked.defaultof<_>, false, httpClientFactory) }

  member private this.AsyncCreateUploadConsumer (uri: Uri, overrideContentType: string) = async {
    let client = this.CreateClient ()
    try
      let  contentType   = choose2 overrideContentType this.ContentType "application/octet-stream"
      let  bucketName    = uri.Host
      let  name          = uri.AbsolutePath.Trim '/'
      let  predefinedAcl = if this.IsPublic then PredefinedAcls.Public else PredefinedAcls.Private
      let  initEndpoint  = createInitEndpoint bucketName predefinedAcl
      let! token         = GoogleStorageCredential.asyncGetAccessToken GoogleStorageCredential.readWriteScopes this.Credential
      client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)
      let! endpoint = async {
        let object    = { ContentType = contentType; CacheControl = this.CacheControl; Name = name }
        let content   = new ByteArrayContent (JsonSerializer.SerializeToUtf8Bytes object)
        content.Headers.ContentType <- jsonContentType
        use request   = new HttpRequestMessage (HttpMethod.Post, initEndpoint)
        request.Content <- content
        request.Headers.Add ("X-Upload-Content-Type", contentType)
        use! response = client.AsyncSend request
        response.EnsureSuccessStatusCode () |> ignore
        return
          match response.Headers.Location with
          | null -> GoogleCloudStorageUploadException "Resumable upload response contans no location." |> raise
          | uri  -> uri }
      // GoogleCloudStorageUploaderConsumer disposes the client
      return new GoogleCloudStorageUploaderConsumer (client, endpoint, contentType)
    with exn ->
      // if no GoogleCloudStorageUploaderConsumer is created client should be disposed
      client.Dispose ()
      ExceptionDispatchInfo.Capture(exn).Throw ()
      return Unchecked.defaultof<_> }

  interface IImageDestination with
    member this.CreateConsumer (contentInfo: ContentInfo): IStreamConsumer =
      StreamConsumer.Of
        (fun input -> async {
          use! consumer = this.AsyncCreateUploadConsumer (this.Uri, contentInfo.ContentType)
          do! consumer.AsyncConsume input
        })

  interface ISerializableImageResource with
    member this.Uri =
      let builder = UriBuilder this.Uri
      // TODO: find a way to avoid executing async functionality synchronously
      let accessToken = GoogleStorageCredential.asyncGetAccessToken GoogleStorageCredential.readWriteScopes this.Credential |> Async.RunSynchronously
      let isPublic = if this.IsPublic then "true" else String.Empty
      // TODO: what if access token is too large ?! Right now access tokens
      let buffer = Span<char> (NativePtr.stackalloc<char> (16 * 1024) |> NativePtr.toVoidPtr, 16 * 1024)
      let qbuilder = SpanBuilder buffer
      let mutable first = true
      appends &first UriParameters.ContentType  this.ContentType  &qbuilder
      appends &first UriParameters.CacheControl this.CacheControl &qbuilder
      appends &first UriParameters.Public       isPublic          &qbuilder
      appends &first UriParameters.AccessToken  accessToken       &qbuilder
      builder.Query <- qbuilder.ToString ()
      builder.Uri

