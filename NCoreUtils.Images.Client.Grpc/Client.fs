namespace NCoreUtils.Images

open System.IO
open Google.Protobuf
open Grpc.Core
open NCoreUtils
open NCoreUtils.Images.Proto
open NCoreUtils.IO
open System.Threading.Tasks

[<CLIMutable>]
type GrpcImageResizerClientConfiguration = {
  Host : string
  Port : int }


[<AutoOpen>]
module private GrpcImageResizerClientHelpers =

  let applyOptions (options : IResizeOptions) (metadata : Metadata) =
    metadata
      .AddNotEmpty("o-image-type", options.ImageType)
      .AddNotEmpty("o-width", options.Width)
      .AddNotEmpty("o-height", options.Height)
      .AddNotEmpty("o-resize-mode", options.ResizeMode)
      .AddNotEmpty("o-quality", options.Quality)
      .AddNotEmpty("o-optimize", options.Optimize)
      .AddNotEmpty("o-weight-x", options.WeightX)
      .AddNotEmpty("o-weight-y", options.WeightY)

  let applyValues (values : Client.Meta) (metadata : Metadata) =
    for kv in values do
      metadata.AddNotEmpty (kv.Key, kv.Value) |> ignore
    metadata

  let rec writeTo buffer (writer : IClientStreamWriter<Proto.Chunk>) (source : Stream) = async {
    match! source.AsyncRead (buffer, 0, buffer.Length) with
    | 0 -> do! Async.Adapt (fun _ -> writer.CompleteAsync ())
    | read ->
      do! Async.Adapt (fun _ -> writer.WriteAsync (Proto.Chunk (Data = ByteString.CopyFrom (buffer, 0, read))))
      do! writeTo buffer writer source }

  let rec readFrom (reader : IAsyncStreamReader<Proto.Chunk>) (target : Stream) = async {
    match! Async.Adapt reader.MoveNext with
    | true ->
      // FIXME: async
      reader.Current.Data.WriteTo target
      do! readFrom reader target
    | _ ->
      do! target.AsyncFlush ()
      target.Close () }

type GrpcImageResizerClient =
  inherit Client.ImageResizerClientBase

  // val mutable private client : Proto.ImageService.ImageServiceClient

  val public Configuration : GrpcImageResizerClientConfiguration

  member this.Client =
    // if isNull this.client then
    //   this.client <- Channel (this.Configuration.Host, this.Configuration.Port, ChannelCredentials.Insecure) |> Proto.ImageService.ImageServiceClient
    // this.client
    Channel (this.Configuration.Host, this.Configuration.Port, ChannelCredentials.Insecure) |> Proto.ImageService.ImageServiceClient

  new (configuration : GrpcImageResizerClientConfiguration) =
    // { Configuration = configuration
    //   client        = null }
    { Configuration = configuration }

  override this.CreateTransformation options =
    let buffer = Array.zeroCreate Defaults.ChunkSize
    { new IDependentStreamTransformation<string> with
        member __.AsyncPerform (input, outputSource) = async {
          let! cancellationToken = Async.CancellationToken
          use call = this.Client.ResizeTransform (Metadata () |> applyOptions options, cancellationToken = cancellationToken)
          let readTask =
            let computation = async {
              let! headers = call.ResponseHeadersAsync |> Async.AwaitTask
              let output =
                match Seq.tryFind (fun (e : Metadata.Entry) -> e.Key = "image-type") headers with
                | Some entry -> outputSource entry.Value
                | _          -> failwithf "No image type found in response."
              do! readFrom call.ResponseStream output }
            Async.StartAsTask (computation, cancellationToken = cancellationToken)
          do! writeTo buffer call.RequestStream input
          do! Async.AwaitTask readTask }
        member __.Dispose () = ()
     }

  override this.AsyncTransform (producer : IStreamProducer, consumer : IStreamConsumer, options : IResizeOptions) =
    let transformation =
      let buffer = Array.zeroCreate Defaults.ChunkSize
      { new IStreamTransformation with
          member __.AsyncPerform (input, output) = async {
            let! cancellationToken = Async.CancellationToken
            use call = this.Client.ResizeTransform (Metadata () |> applyOptions options, cancellationToken = cancellationToken)
            let readTask = Async.StartAsTask (readFrom call.ResponseStream output, cancellationToken = cancellationToken)
            do! writeTo buffer call.RequestStream input
            do! Async.AwaitTask readTask }
          member __.Dispose () = ()
      }
    StreamConsumer.asyncConsumeProducer (StreamTransformation.chainProducer transformation producer) consumer

  override this.AsyncTransform (producer : IStreamProducer, target : Client.Meta, options) =
    let consumer =
      let buffer = Array.zeroCreate Defaults.ChunkSize
      { new IStreamConsumer<Proto.Empty> with
          member __.AsyncConsume input = async {
            let headers = Metadata () |> applyOptions options |> applyValues target
            let! cancellationToken = Async.CancellationToken
            use call = this.Client.ResizeConsume (headers, cancellationToken = cancellationToken)
            do! writeTo buffer call.RequestStream input
            return! call.ResponseAsync |> Async.AwaitTask }
          member __.Dispose () = ()
      }
    StreamToResultConsumer.asyncConsumeProducer producer consumer |> Async.Ignore

  override this.AsyncTransform (source : Client.Meta, target : Client.Meta, options) = async {
    let headers = Metadata () |> applyOptions options |> applyValues target |> applyValues source
    let! cancellationToken = Async.CancellationToken
    use call = this.Client.ResizeOperationAsync (Proto.Empty (), headers, cancellationToken = cancellationToken)
    do! call.ResponseAsync |> Async.AwaitTask |> Async.Ignore }

  override this.AsyncTransform (source : Client.Meta, consumer : IStreamConsumer, options) =
    let producer =
      { new IStreamProducer with
          member __.AsyncProduce output = async {
            let headers = Metadata () |> applyOptions options |> applyValues source
            let! cancellationToken = Async.CancellationToken
            use call = this.Client.ResizeProduce (Proto.Empty (), headers, cancellationToken = cancellationToken)
            do! readFrom call.ResponseStream output }
          member __.Dispose () = ()
      }
    StreamConsumer.asyncConsumeProducer producer consumer

  override __.AsyncGetInfo (_ : Client.Meta) : Async<ImageInfo> = notImplemented "WIP"

  override __.AsyncGetInfo (_ : IStreamProducer) : Async<ImageInfo> = notImplemented "WIP"
