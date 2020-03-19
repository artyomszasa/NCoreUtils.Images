namespace NCoreUtils.Images.GoogleCloudStorage

open System
open System.Net.Http
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

type GoogleCloudStorageRecordDescriptor =
  val public Uri               : Uri
  val public Credential        : GoogleStorageCredential
  val public ContentType       : string
  val public CacheControl      : string
  val public IsPublic          : bool
  val public HttpClientFactory : IHttpClientFactory
  new (uri,
        credential,
        [<Optional; DefaultParameterValue(null : string)>] contentType,
        [<Optional; DefaultParameterValue(null : string)>] cacheControl,
        [<Optional; DefaultParameterValue(false)>] isPublic,
        [<Optional; DefaultParameterValue(null : IHttpClientFactory)>] httpClientFactory) =
    if isNull uri then nullArg "uri"
    { Uri               = uri
      Credential        = credential
      ContentType       = contentType
      CacheControl      = cacheControl
      IsPublic          = isPublic
      HttpClientFactory = httpClientFactory }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.CreateClient () =
    match this.HttpClientFactory with
    | null    -> new HttpClient ()
    | factory -> factory.CreateClient HttpClient.Name