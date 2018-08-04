namespace NCoreUtils.Images.WebService

open System.IO
open System.Text
open System.Threading
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.DependencyInjection
open NCoreUtils.Images
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.Runtime.ExceptionServices

type ServiceConfiguration () =
  member val MaxConcurrentOps = 20 with get, set
  override this.ToString () = sprintf "ServiceConfiguration[MaxConcurrentOps = %d]" this.MaxConcurrentOps

module internal Middleware =

  type private HttpResponseDestination (response : HttpResponse) =
    interface IImageDestination with
      member __.AsyncWrite (info, generator) =
        response.ContentType <-
          match info.ContentType with
          | null -> "application/octet-stream"
          | ct   -> ct
        match info.ContentLength with
        | Nullable.Value _ as contentLength ->
          response.ContentLength <- contentLength
          generator response.Body
        | _ -> async {
          use buffer = new FileStream (Path.GetTempFileName (), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.Asynchronous ||| FileOptions.DeleteOnClose)
          do! generator buffer
          buffer.Seek (0L, SeekOrigin.Begin) |> ignore
          response.ContentLength <- Nullable.mk buffer.Length
          do! buffer.AsyncCopyTo (response.Body, 65536)
          do! response.Body.AsyncFlush () }
      member this.AsyncWriteDelayed generator =
        let setContentType (info : ContentInfo) =
          response.ContentType <-
            match info.ContentType with
            | null -> "application/octet-stream"
            | ct   -> ct
        generator setContentType response.Body


  let private errorSerializerSettings =
    let settings = JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = CamelCasePropertyNamesContractResolver ())
    settings.Converters.Insert (0, ImageResizerErrorConverter ())
    settings

  let inline private (|CI|_|) (comparand : string) (value : CaseInsensitive) =
    match CaseInsensitive comparand = value with
    | true -> Some ()
    | _    -> None

  let private readParameters (query : IQueryCollection) =
    let inline v name =
      let mutable values = Unchecked.defaultof<_>
      match query.TryGetValue (name, &values) with
      | true when values.Count > 0 -> Some values.[0]
      | _                          -> None
    let inline s name = v name |> Option.unwrap
    let inline i name = v name >>= tryInt |> Option.toNullable
    let inline b name = v name >>= tryBool |> Option.toNullable
    ResizeOptions (
      imageType  = s "t",
      width      = i "w",
      height     = i "h",
      resizeMode = s "m",
      quality    = i "q",
      optimize   = b "x",
      weightX    = i "cx",
      weightY    = i "cy")

  let info httpContext (imageResizer : IImageResizer) =
    HttpContext.requestBody httpContext
    |> imageResizer.AsyncGetImageInfo
    >>= json httpContext

  let handle (computation : unit -> Async<_>) = async {
    try return! computation () >>| Ok
    with
      | :? ImageResizerException as exn -> return exn.Error |> Error
      | exn                             -> ExceptionDispatchInfo.Capture(exn).Throw (); return Unchecked.defaultof<_> }


  let private resizeRes (semaphore : SemaphoreSlim) httpContext (imageResizer : IImageResizer) = async {
    let options = (HttpContext.request httpContext).Query |> readParameters
    do! Async.Adapt (fun _ -> semaphore.WaitAsync (httpContext.RequestAborted))
    try
      // match httpContext.Request.ContentLength.HasValue with
      // | true when httpContext.Request.ContentLength.Value = 0L -> return Error <| ImageResizerError.invalidImage
      // | true when httpContext.Request.ContentLength.Value < 5L * 1024L * 1024L ->
      //   let dest = HttpContext.response httpContext |> HttpResponseDestination
      //   return! handle (fun () -> imageResizer.AsyncResize (httpContext.Request.Body, dest, options))
      // | _ ->
      //   let tmp = Path.GetTempFileName ()
      //   try
      //     let! inputSize = async {
      //       use source  = HttpContext.requestBody httpContext
      //       let buffer  = new FileStream (tmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous)
      //       do! source.AsyncCopyTo (buffer, 65536)
      //       do! buffer.AsyncFlush ()
      //       return buffer.Length }
      //     match inputSize with
      //     | 0L -> return Error <| ImageResizerError.invalidImage
      //     | _  ->
      //       let dest    = HttpContext.response httpContext |> HttpResponseDestination
      //       return! handle  imageResizer.AsyncResResize (tmp, dest, options)
      //   finally try if File.Exists tmp then File.Delete tmp with _ -> ()
      let dest = HttpContext.response httpContext |> HttpResponseDestination
      return! handle (fun () -> imageResizer.AsyncResize (httpContext.Request.Body, dest, options))
    finally semaphore.Release () |> ignore }

  let private resize semaphore httpContext imageResizer = async {
    let! res = resizeRes semaphore httpContext imageResizer
    match res with
    | Ok _ -> ()
    | Error err ->
      let response = HttpContext.response httpContext
      response.StatusCode <- 400
      response.ContentType <- "application/json"
      let json = JsonConvert.SerializeObject (err, errorSerializerSettings)
      let data = Encoding.UTF8.GetBytes json
      response.ContentLength <- Nullable.mk data.LongLength
      do! response.Body.AsyncWrite data
      do! response.Body.AsyncFlush () }

  let inline private async405 httpContext =
    HttpContext.setResponseStatusCode 405 httpContext
    async.Return ()

  let run semaphore httpContext asyncNext =
    match HttpContext.path httpContext with
    | [] ->
      match HttpContext.httpMethod httpContext with
      | HttpMethod.HttpPost -> getService<IImageResizer> httpContext.RequestServices |> resize semaphore httpContext
      | _                   -> async405 httpContext
    | [ CI "info" ] ->
      match HttpContext.httpMethod httpContext with
      | HttpMethod.HttpPost -> getService<IImageResizer> httpContext.RequestServices |> info httpContext
      | _                   -> async405 httpContext
    | _ -> asyncNext
