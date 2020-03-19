namespace NCoreUtils.Images.GoogleCloudStorage

open System.Runtime.CompilerServices
open Google.Apis.Auth.OAuth2
open NCoreUtils

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
    static member op_Implicit (accessToken : string) =
      ViaAccessToken accessToken
    static member op_Implicit (googleCredential : GoogleCredential) =
      GoogleStorageCredential.FromCredential googleCredential

[<RequireQualifiedAccess>]
module internal GoogleStorageCredential =

  [<CompiledName("AsyncGetAccessTokenForRequest")>]
  let private asyncGetAccessTokenForRequest (cred: GoogleCredential) =
    Async.Adapt (fun cancellationToken -> cred.UnderlyingCredential.GetAccessTokenForRequestAsync (cancellationToken = cancellationToken))

  [<CompiledName("AsyncGetApplicationDefault")>]
  let private asyncGetApplicationDefault () =
    Async.Adapt (fun _ -> GoogleCredential.GetApplicationDefaultAsync ())

  [<CompiledName("CreateScopedCredential")>]
  let private createScopedCredential scopes (googleCredential: GoogleCredential) =
    googleCredential.CreateScoped scopes

  [<CompiledName("ReadOnlyScopes")>]
  let readOnlyScopes = [| "https://www.googleapis.com/auth/devstorage.read_only" |]

  [<CompiledName("ReadWriteScopes")>]
  let readWriteScopes = [| "https://www.googleapis.com/auth/devstorage.read_write" |]

  [<CompiledName("AsyncGetAccessToken")>]
  let asyncGetAccessToken scopes cred  =
    match cred with
    | ViaAccessToken accessToken -> async.Return accessToken
    | ViaGoogleCredential credential ->
      credential.CreateScoped scopes
      |> asyncGetAccessTokenForRequest
    | ViaDefault ->
      asyncGetApplicationDefault ()
      >>| createScopedCredential scopes
      >>= asyncGetAccessTokenForRequest
