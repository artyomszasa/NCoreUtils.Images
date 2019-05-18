namespace NCoreUtils.Images

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Google.Apis.Storage.v1
open NCoreUtils
open NCoreUtils.IO

[<Sealed>]
type GoogleCloudStorageRecordConsumer =
  val public Descriptor : GoogleCloudStorageRecordDescriptor
  member this.Uri          = this.Descriptor.Uri
  member this.Credential   = this.Descriptor.Credential
  member this.ContentType  = this.Descriptor.ContentType
  member this.CacheControl = this.Descriptor.CacheControl
  member this.IsPublic     = this.Descriptor.IsPublic

  new (descriptor : GoogleCloudStorageRecordDescriptor) =
    { Descriptor = descriptor }

  new (uri,
        credential : GoogleStorageCredential,
        [<Optional; DefaultParameterValue(null : string)>] contentType : string,
        [<Optional; DefaultParameterValue(null : string)>] cacheControl : string,
        [<Optional; DefaultParameterValue(false)>] isPublic : bool) =
    new GoogleCloudStorageRecordConsumer (GoogleCloudStorageRecordDescriptor(uri, credential, contentType, cacheControl, isPublic))

  new (uri : Uri) =
    new GoogleCloudStorageRecordConsumer (uri, ViaDefault)

  member private this.AsyncUpload (input, obj : Data.Object) = async {
    use service = new StorageService ()
    let request = service.Objects.Insert (obj, this.Uri.Host, input, obj.ContentType |?? "application/octet-stream")
    let! token = GoogleStorageCredential.asyncGetAccessToken GoogleStorageCredential.readWriteScopes this.Credential
    request.OauthToken <- token
    do! Async.Adapt (request.UploadAsync : CancellationToken -> Task<_>) |> Async.Ignore }

  member this.AsyncConsume input =
    let acl =
      let acl = ResizeArray ()
      if this.IsPublic then
        acl.Add <| Data.ObjectAccessControl (Entity = "allUsers", Role = "READER")
      acl
    let obj =
      Data.Object (
        Name         = (this.Uri.LocalPath.Trim '/'),
        CacheControl = this.CacheControl,
        Acl          = acl,
        ContentType  = this.ContentType)
    this.AsyncUpload (input, obj)

  interface IStreamConsumer with
    member this.AsyncConsume input = this.AsyncConsume input
    member __.Dispose () = ()

type GoogleCloudStorageImageDestination =
  val public Descriptor : GoogleCloudStorageRecordDescriptor
  member this.Uri          = this.Descriptor.Uri
  member this.Credential   = this.Descriptor.Credential
  member this.ContentType  = this.Descriptor.ContentType
  member this.CacheControl = this.Descriptor.CacheControl
  member this.IsPublic     = this.Descriptor.IsPublic

  new (descriptor : GoogleCloudStorageRecordDescriptor) =
    { Descriptor = descriptor }

  new (uri,
        credential : GoogleStorageCredential,
        [<Optional; DefaultParameterValue(null : string)>] contentType : string,
        [<Optional; DefaultParameterValue(null : string)>] cacheControl : string,
        [<Optional; DefaultParameterValue(false)>] isPublic : bool) =
    GoogleCloudStorageImageDestination (GoogleCloudStorageRecordDescriptor(uri, credential, contentType, cacheControl, isPublic))

  new (uri : Uri) =
    GoogleCloudStorageImageDestination (uri, ViaDefault)

  member this.CreateConsumer () =
    new GoogleCloudStorageRecordConsumer (this.Descriptor)

  member this.AsyncGetMetadata () = async {
    let! recordMetadata = this.Descriptor.AsyncGetMetadata GoogleStorageCredential.readOnlyScopes
    return
      recordMetadata
      |> Seq.map (fun (KeyValue (key, value)) -> KeyValuePair (key, "target-" + value))
      |> ImmutableDictionary.CreateRange
      :> IReadOnlyDictionary<_, _> }

  interface IImageDestination with
    member this.CreateConsumer () = this.CreateConsumer () :> _

  interface IMetadataProvider with
    member this.AsyncGetMetadata () = this.AsyncGetMetadata ()


