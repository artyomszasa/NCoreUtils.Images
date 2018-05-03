namespace NCoreUtils.Images

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open NCoreUtils
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

type ImageResizerClientConfiguration () =
  member val EndPoints = Array.empty<string> with get, set

[<AutoOpen>]
module private ImageResizerClientHelpers =

  let serializerSettings = JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ())

  let inline private appends first (name : string) (value : string) (builder : StringBuilder) =
    match isNull value with
    | true -> builder
    | _ ->
      match !first with
      | true ->
        first := false
        builder.AppendFormat ("?{0}={1}", name, Uri.EscapeDataString value)
      | _ ->
        builder.AppendFormat ("&{0}={1}", name, Uri.EscapeDataString value)

  let inline private appendn first (name : string) (value : Nullable<_>) (builder : StringBuilder) =
    match value.HasValue with
    | false -> builder
    | _ ->
      match !first with
      | true ->
        first := false
        builder.AppendFormat("?{0}={1}", name, Uri.EscapeDataString <| value.Value.ToString ())
      | _ ->
        builder.AppendFormat("&{0}={1}", name, Uri.EscapeDataString <| value.Value.ToString ())


  let inline private builderToString (builder : StringBuilder) = builder.ToString ()

  let createQueryString (options : ResizeOptions) =
    let first = ref true
    StringBuilder ()
    |> appends first "t"  options.ImageType
    |> appendn first "w"  options.Width
    |> appendn first "h"  options.Height
    |> appends first "m"  options.ResizeMode
    |> appendn first "q"  options.Quality
    |> appendn first "x"  options.Optimize
    |> appendn first "cx" options.WeightX
    |> appendn first "cy" options.WeightY
    |> builderToString



type ImageResizerClient =
  val private configuration : ImageResizerClientConfiguration
  val private logger        : ILog
  new (configuration, logger : ILog<ImageResizerClient>) =
    { configuration = configuration
      logger        = logger }
  member this.AsyncResize (source : Stream, destination : IImageDestination, options : ResizeOptions) =
    let queryString = createQueryString options
    let rec invoke i =
      match i = this.configuration.EndPoints.Length with
      | true -> async.Return false
      | _    ->
        let endpoint = this.configuration.EndPoints.[i]
        let uri = UriBuilder(endpoint, Query = queryString).Uri;
        async {
          let! success = async {
            use requestMessageContent = new StreamContent (source)
            use requestMessage = new HttpRequestMessage (HttpMethod.Post, uri, Content = requestMessageContent)
            requestMessageContent.Headers.ContentType <- MediaTypeHeaderValue.Parse "application/octet-stream"
            use httpClient = new HttpClient ()
            use! responseMessage = httpClient.AsyncSend (requestMessage, HttpCompletionOption.ResponseHeadersRead)
            match responseMessage.StatusCode with
            | HttpStatusCode.OK ->
              match responseMessage.Content with
              | null ->
                this.logger.LogWarning (null, sprintf "Image server responded with no content (endpoint = %s)." endpoint)
                return false
              | content ->
                let length = content.Headers.ContentLength
                let ctype  = content.Headers.ContentType |> Option.wrap >>| (fun x -> x.ToString ()) |> Option.defaultValue null
                do! destination.AsyncWrite (ContentInfo (length, ctype), fun stream -> content.AsyncReadAsStream () >>= (fun sourceStream -> sourceStream.AsyncCopyTo stream))
                return true
            | HttpStatusCode.BadRequest ->
              this.logger.LogWarning (null, "Image server responded with bad request.")
              return false
            | HttpStatusCode.NotImplemented ->
              this.logger.LogWarning (null, "Feature not implmented on image server.");
              return false
            | statusCode ->
              this.logger.LogWarning (null, sprintf "Image server responded with %A." statusCode)
              return false }
          match success with
          | true -> return true
          | _    -> return! invoke (i + 1)
        }
    async {
      let! success = invoke 0
      match success with
      | true -> ()
      | _    -> failwith "failed" //FIXME: exception + message
    }