namespace NCoreUtils.Images

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Net.Http
open Google.Apis.Auth.OAuth2
open NCoreUtils
open NCoreUtils.IO

[<Sealed>]
type GoogleCloudStorageRecordProducer =
  val public Descriptor : GoogleCloudStorageRecordDescriptor
  member this.Uri        = this.Descriptor.Uri
  member this.Credential = this.Descriptor.Credential

  new (descriptor : GoogleCloudStorageRecordDescriptor) =
    if isNull (box descriptor) then nullArg "descriptor"
    { Descriptor = descriptor }

  new (uri, credential : GoogleStorageCredential) =
    if isNull uri then nullArg "uri"
    new GoogleCloudStorageRecordProducer (GoogleCloudStorageRecordDescriptor (uri, credential))

  new (uri, credential : GoogleCredential) =
    let cred = GoogleStorageCredential.FromCredential credential
    new GoogleCloudStorageRecordProducer (uri, cred)

  new (uri : Uri) =
    new GoogleCloudStorageRecordProducer (uri, ViaDefault)

  member this.AsyncProduce output = async {
    let! token      = GoogleStorageCredential.asyncGetAccessToken GoogleStorageCredential.readOnlyScopes this.Credential
    use  client     = new HttpClient () // FIXME: HttpClientFactory
    let  requestUri = sprintf "https://www.googleapis.com/storage/v1/b/%s/o/%s?alt=media" this.Uri.Host (this.Uri.LocalPath.Trim '/' |> Uri.EscapeDataString )
    use  request    = new HttpRequestMessage (HttpMethod.Get, requestUri)
    request.Headers.Authorization <- Headers.AuthenticationHeaderValue ("Bearer", token)
    use! response = client.AsyncSend (request, HttpCompletionOption.ResponseHeadersRead)
    do!
      Async.Adapt
        (fun _ ->
          match response.StatusCode with
          | Net.HttpStatusCode.OK ->
            response.Content.CopyToAsync output
          | statusCode ->
            sprintf "Objects.get failed, status code = %A, uri = %s" statusCode requestUri
            |> GoogleCloudStorageAccessException
            |> raise)
    output.Close () }

  interface IStreamProducer with
    member this.AsyncProduce output = this.AsyncProduce output
    member __.Dispose () = ()

type GoogleCloudStorageImageSource =
  val public Descriptor : GoogleCloudStorageRecordDescriptor
  member this.Uri        = this.Descriptor.Uri
  member this.Credential = this.Descriptor.Credential

  new (uri, credential : GoogleStorageCredential) =
    if isNull uri then nullArg "uri"
    { Descriptor = GoogleCloudStorageRecordDescriptor (uri, credential) }

  new (uri) =
    GoogleCloudStorageImageSource (uri, ViaDefault)

  member this.CreateProducer () = new GoogleCloudStorageRecordProducer (this.Descriptor)

  member this.AsyncGetMetadata () = async {
    let! recordMetadata = this.Descriptor.AsyncGetMetadata GoogleStorageCredential.readOnlyScopes
    return
      recordMetadata
      |> Seq.map (fun (KeyValue (key, value)) -> KeyValuePair (key, "source-" + value))
      |> ImmutableDictionary.CreateRange
      :> IReadOnlyDictionary<_, _> }

  interface IImageSource with
    member this.CreateProducer () = this.CreateProducer () :> _

  interface IMetadataProvider with
    member this.AsyncGetMetadata () = this.AsyncGetMetadata ()


