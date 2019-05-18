namespace NCoreUtils.Images.Grpc

open System.Runtime.CompilerServices
open NCoreUtils
open NCoreUtils.Images
open NCoreUtils.IO
open Grpc.Core
open Google.Protobuf

[<AbstractClass; Sealed>]
[<Extension>]
type private GrpcExtensions =

  [<Extension>]
  static member AsyncNext (this : IAsyncStreamReader<'T>) = async {
    let! hasNext = Async.Adapt this.MoveNext
    return
      match hasNext with
      | true -> ValueSome this.Current
      | _    -> ValueNone }


[<CLIMutable>]
type ImageResizerClientConfiguration = {
  Host : string
  Port : int }

type ImageResizerClient (configuration : ImageResizerClientConfiguration) =
  let client =
    Channel (configuration.Host, configuration.Port, ChannelCredentials.Insecure)
    |> ImageService.ImageServiceClient

  member __.CreateTransformation (options : IResizeOptions) : IDependentStreamTransformation<string> =
    let opts =
      ResizeOptions (
        ImageType = options.ImageType,
        Width = options.Width,
        Height = options.Height,
        Mode = options.ResizeMode,
        Quality = options.Quality,
        Optimize = options.Optimize,
        WeightX = options.WeightX,
        WeightY = options.WeightY)
    let buffer = Array.zeroCreate 8192
    { new IDependentStreamTransformation<string> with
        member __.AsyncPerform (input, outputSource) = async {
          let! cancellationToken = Async.CancellationToken
          let call = client.ResizeAsync (cancellationToken = cancellationToken)
          // send data
          do! call.RequestStream.WriteAsync (RequestData (Options = opts)) |> Async.AwaitTask
          let rec send (_ : int) = async {
            match! input.AsyncRead (buffer, 0, 8192) with
            | 0    ->
              do! call.RequestStream.CompleteAsync () |> Async.AwaitTask
            | read ->
              do! call.RequestStream.WriteAsync (RequestData (Chunk = ByteString.CopyFrom (buffer, 0, read))) |> Async.AwaitTask
              do! send Unchecked.defaultof<_> }
          do! send Unchecked.defaultof<_>
          // receive data
          match! call.ResponseStream.AsyncNext () with
          | ValueSome message when message.DataCase = ResponseData.DataOneofCase.ImageType ->
            let output = outputSource message.ImageType
            let rec recv (_ : int) = async {
              match! call.ResponseStream.AsyncNext () with
              | ValueSome message when message.DataCase = ResponseData.DataOneofCase.ImageType ->
                failwith "ImageType received while reading data stream."
              | ValueSome message when message.DataCase = ResponseData.DataOneofCase.Chunk ->
                message.Chunk.WriteTo output
                do! recv Unchecked.defaultof<_>
              | ValueSome message when message.DataCase = ResponseData.DataOneofCase.None ->
                failwith "Empty message received while reading data stream."
              | ValueSome _ ->
                failwith "Invalid message received while reading image type."
              | ValueNone -> () }
            do! recv Unchecked.defaultof<_>
          | ValueSome message when message.DataCase = ResponseData.DataOneofCase.Chunk ->
            failwith "Data chunk received while reading image type."
          | ValueSome message when message.DataCase = ResponseData.DataOneofCase.None ->
            failwith "Empty message received while reading image type."
          | ValueSome _ ->
            failwith "Invalid message received while reading image type."
          | ValueNone ->
            failwith "End of stream reached while reading image type." }
        member __.Dispose () = ()
     }

  interface IImageResizer with
    member this.AsyncGetInfo(source: IImageSource): Async<ImageInfo> =
      failwith "Not Implemented"
    member this.CreateTransformation options =
      this.CreateTransformation options
