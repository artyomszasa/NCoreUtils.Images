namespace NCoreUtils.Images.WebService

// open System
open System.IO
// open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.DependencyInjection
open NCoreUtils.Images
open Microsoft.Extensions.Primitives

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
    |> json httpContext

  let resize httpContext (imageResizer : IImageResizer) = async {
    let options = (HttpContext.request httpContext).Query |> readParameters

    let tmp = Path.GetTempFileName ()
    try
      do! async {
        use source  = HttpContext.requestBody httpContext
        let buffer  = new FileStream (tmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous)
        do! source.AsyncCopyTo (buffer, 65536)
        do! buffer.AsyncFlush () }
      // use! buffer = async {
      //   use source  = HttpContext.requestBody httpContext
      //   let buffer  = new FileStream (Path.GetTempFileName (), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.Asynchronous ||| FileOptions.DeleteOnClose)
      //   do! source.AsyncCopyTo (buffer, 65536)
      //   buffer.Seek (0L, SeekOrigin.Begin) |> ignore
      //   return buffer }
      let dest    = HttpContext.response httpContext |> HttpResponseDestination
      do! imageResizer.AsyncResize (tmp, dest, options)
    finally try if File.Exists tmp then File.Delete tmp with _ -> () }

  let inline private async405 httpContext =
    HttpContext.setResponseStatusCode 405 httpContext
    async.Return ()

  let run httpContext asyncNext =
    match HttpContext.path httpContext with
    | [] ->
      match HttpContext.httpMethod httpContext with
      | HttpMethod.HttpPost -> getService<IImageResizer> httpContext.RequestServices |> resize httpContext
      | _                   -> async405 httpContext
    | [ CI "info" ] ->
      match HttpContext.httpMethod httpContext with
      | HttpMethod.HttpPost -> getService<IImageResizer> httpContext.RequestServices |> info httpContext
      | _                   -> async405 httpContext
    | _ -> asyncNext
