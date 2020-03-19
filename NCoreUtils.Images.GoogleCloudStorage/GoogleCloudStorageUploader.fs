namespace NCoreUtils.Images

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Runtime.CompilerServices
open System.Threading
open NCoreUtils
open NCoreUtils.IO

[<Sealed>]
type private GoogleCloudStorageUploader (client : HttpClient, endpoint : Uri, contentType: string) =
  let buffer = new UnmanagedMemoryManager<byte> (512 * 1024)

  let mutable sent = 0L

  let mutable isDisposed = 0

  member private __.Dispose (disposing : bool) =
    if 0 = Interlocked.CompareExchange (&isDisposed, 1, 0) then
      if disposing then
        (buffer :> IDisposable).Dispose ()

  member private __.SendChunk (size: int, final: bool) = async {
    let targetSize = sent + int64 size
    let roMemory   = Memory.op_Implicit (if size = buffer.Size then buffer.Memory else buffer.Memory.Slice (0, size))
    let content    = new ReadOnlyMemoryContent (roMemory)
    let headers    = content.Headers
    headers.ContentLength <- Nullable.mk (int64 size)
    headers.ContentType   <- MediaTypeHeaderValue.Parse contentType
    headers.ContentRange  <- if final then ContentRangeHeaderValue (sent, targetSize - 1L) else ContentRangeHeaderValue (sent, targetSize - 1L, targetSize)
    use request    = new HttpRequestMessage (HttpMethod.Put, endpoint, Content = content)
    return! client.AsyncSend request }

  member this.AsyncConsumeStream (stream : IO.Stream) = async {
    let! read = stream.AsyncRead buffer.Memory
    match read with
    | _ when read = buffer.Size ->
      // partial upload
      use! response = this.SendChunk (read, false)
      match int response.StatusCode with
      | 308 ->
        // chunk upload successfull (TODO: check if response range is set properly)
        sent <- sent + (int64 read)
        do! this.AsyncConsumeStream stream
      | _ ->
        GoogleCloudStorageUploadException (sprintf "Upload chunk failed with status code %A." response.StatusCode) |> raise
    | _ ->
      // FIXME: handle stream being divisable by chunk size
      let! response = this.SendChunk (read, true)
      match response.StatusCode with
      | HttpStatusCode.OK
      | HttpStatusCode.Created -> ()
      | statusCode ->
        GoogleCloudStorageUploadException (sprintf "Upload final chunk failed with status code %A." statusCode) |> raise }

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.Dispose () =
    this.Dispose true
    GC.SuppressFinalize this

  interface IDisposable with
    member this.Dispose () =
      this.Dispose ()

[<Sealed>]
type private GoogleCloudStorageUploaderConsumer =

  val private client : HttpClient

  val private endpoint : Uri

  val private contentType : string

  val mutable private uploader : GoogleCloudStorageUploader voption

  new (client, endpoint, contentType) =
    { client = client
      endpoint = endpoint
      contentType = contentType
      uploader = ValueNone }

  member this.AsyncConsume input =
    // TODO: handle reentrance
    let instance = new GoogleCloudStorageUploader (this.client, this.endpoint, this.contentType)
    this.uploader <- ValueSome instance
    instance.AsyncConsumeStream input

  member this.Dispose () =
    match this.uploader with
    | ValueSome instance -> instance.Dispose ()
    | _                  -> ()
    this.client.Dispose ()

  interface IStreamConsumer with
    member this.AsyncConsume input = this.AsyncConsume input
    member this.Dispose () = this.Dispose ()