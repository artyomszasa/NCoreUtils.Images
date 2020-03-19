namespace NCoreUtils.Images

open System
open System.Net
open System.Net.Http
open System.Runtime.InteropServices
open Google.Apis.Auth.OAuth2
open NCoreUtils
open NCoreUtils.Images.GoogleCloudStorage
open NCoreUtils.IO

[<AutoOpen>]
module private GoogleCloudStorageSourceHelpers =

  let inline createDownloadEndpoint bucketName name =
    sprintf "https://www.googleapis.com/storage/v1/b/%s/o/%s?alt=media" bucketName name

type GoogleCloudStorageSource =
  inherit GoogleCloudStorageRecordDescriptor

  new (uri, credential: GoogleStorageCredential, [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory) =
    { inherit GoogleCloudStorageRecordDescriptor(uri, credential, httpClientFactory = httpClientFactory) }

  new (uri, credential: GoogleCredential, [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory : IHttpClientFactory) =
    GoogleCloudStorageSource (uri, ViaGoogleCredential credential, httpClientFactory = httpClientFactory)

  new (uri, accessToken: string, [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory : IHttpClientFactory) =
    GoogleCloudStorageSource (uri, ViaAccessToken accessToken, httpClientFactory = httpClientFactory)

  new (uri, [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory : IHttpClientFactory) =
    GoogleCloudStorageSource (uri, ViaDefault, httpClientFactory = httpClientFactory)

  interface IImageSource with
    member this.CreateProducer () =
      { new IStreamProducer with
          member __.AsyncProduce output = async {
            let! token      = GoogleStorageCredential.asyncGetAccessToken GoogleStorageCredential.readOnlyScopes this.Credential
            use  client     = this.CreateClient ()
            let  bucketName = this.Uri.Host
            let  name       = this.Uri.LocalPath.Trim '/' |> Uri.EscapeDataString
            let  requestUri = createDownloadEndpoint bucketName name
            use  request    = new HttpRequestMessage (HttpMethod.Get, requestUri)
            request.Headers.Authorization <- Headers.AuthenticationHeaderValue ("Bearer", token)
            use! response = client.AsyncSend (request, HttpCompletionOption.ResponseHeadersRead)
            do!
              Async.Adapt
                (fun _ ->
                  match response.StatusCode with
                  | HttpStatusCode.OK ->
                    response.Content.CopyToAsync output
                  | statusCode ->
                    sprintf "Objects.get failed, status code = %A, uri = %s" statusCode requestUri
                    |> GoogleCloudStorageAccessException
                    |> raise)
            output.Close () }
          member __.Dispose () = ()
      }

  interface ISerializableImageResource with
    member this.Uri = this.Uri

