namespace NCoreUtils.Images

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.InteropServices
open System.Runtime.Serialization
open Google.Apis.Auth.OAuth2
open NCoreUtils

[<Serializable>]
type GoogleCloudStorageAccessException =
  inherit Exception

  new (message) = { inherit Exception (message) }

  new (message, innerExn : exn) = { inherit Exception (message, innerExn) }

  new (info : SerializationInfo, context) = { inherit Exception (info, context) }

[<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
type GoogleStorageCredential =
  | ViaAccessToken      of AccessToken : string
  | ViaGoogleCredential of Credential  : GoogleCredential
  | ViaDefault
  with
    static member FromCredential credential =
      match credential with
      | null -> ViaDefault
      | _    -> ViaGoogleCredential credential
    static member op_Implicit (accessToken : string) = ViaAccessToken accessToken
    static member op_Implicit (googleCredential : GoogleCredential) =
      GoogleStorageCredential.FromCredential googleCredential

[<RequireQualifiedAccess>]
module GoogleStorageCredential =

  [<CompiledName("ReadOnlyScopes")>]
  let readOnlyScopes  = [| "https://www.googleapis.com/auth/devstorage.read_only"  |]

  [<CompiledName("ReadWriteScopes")>]
  let readWriteScopes = [| "https://www.googleapis.com/auth/devstorage.read_write" |]

  [<CompiledName("AsyncGetAccessToken")>]
  let asyncGetAccessToken scopes cred  =
    match cred with
    | ViaAccessToken accessToken -> async.Return accessToken
    | ViaGoogleCredential credential ->
      let c = credential.CreateScoped scopes
      Async.Adapt (fun cancellationToken -> c.UnderlyingCredential.GetAccessTokenForRequestAsync (cancellationToken = cancellationToken))
    | ViaDefault ->
      async {
        let! credential = Async.Adapt (fun _ -> GoogleCredential.GetApplicationDefaultAsync ())
        let  c = credential.CreateScoped scopes
        return! Async.Adapt (fun cancellationToken -> c.UnderlyingCredential.GetAccessTokenForRequestAsync (cancellationToken = cancellationToken)) }

[<Sealed>]
type GoogleCloudStorageRecordDescriptor =
  val public Uri          : Uri
  val public Credential   : GoogleStorageCredential
  val public ContentType  : string
  val public CacheControl : string
  val public IsPublic     : bool
  new (uri,
        credential,
        [<Optional; DefaultParameterValue(null : string)>] contentType,
        [<Optional; DefaultParameterValue(null : string)>] cacheControl,
        [<Optional; DefaultParameterValue(false)>] isPublic) =
    if isNull uri then nullArg "uri"
    { Uri          = uri
      Credential   = credential
      ContentType  = contentType
      CacheControl = cacheControl
      IsPublic     = isPublic }
  member this.AsyncGetMetadata scopes = async {
    let! accessToken = GoogleStorageCredential.asyncGetAccessToken scopes this.Credential
    let builder = ImmutableDictionary.CreateBuilder ()
    builder.Add ("uri", this.Uri.AbsoluteUri)
    builder.Add ("access-token", accessToken)
    if not (String.IsNullOrEmpty this.ContentType) then
      builder.Add ("content-type", this.ContentType)
    if not (String.IsNullOrEmpty this.CacheControl) then
      builder.Add ("cache-control", this.CacheControl)
    if this.IsPublic then
      builder.Add ("is-public", "true")
    return builder.ToImmutable () :> IReadOnlyDictionary<_, _> }


