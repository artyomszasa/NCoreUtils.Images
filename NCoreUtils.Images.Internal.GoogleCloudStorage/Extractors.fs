namespace NCoreUtils.Images

open System
open System.IO
open System.Text
open NCoreUtils
open NCoreUtils.AspNetCore

module ValueOption = Microsoft.FSharp.Core.ValueOption

type GoogleCloudStorageSourceExtractor () =
  interface IImageSourceExtractor with
    member __.AsyncExtractImageSource httpContext =
      let result =
        let headers = HttpContext.requestHeaders httpContext
        match Headers.tryGetFirstValue "X-SourceType" headers with
        | ValueSome (EQI "GoogleCloudStorageRecord") ->
          match Headers.tryGetFirstValue "X-SourceAccessToken" headers with
          | ValueSome accessToken ->
            { new IImageSource with
                member __.AsyncCreateProducer () = async {
                  use  reader    = new StreamReader (httpContext.Request.Body, Encoding.ASCII)
                  let! uriString = Async.Adapt (fun _ -> reader.ReadToEndAsync ())
                  let  uri       = Uri (uriString, UriKind.Absolute)
                  return new GoogleStorageRecordProducer (uri, accessToken) :> _ }
            }
            |> ValueSome
          | _ -> ValueNone
        | _ -> ValueNone
      async.Return result

type GoogleCloudStorageDestinationExtractor () =
  interface IImageDestinationExtractor with
    member __.AsyncExtractImageDestination httpContext =
      let result =
        let headers = HttpContext.requestHeaders httpContext
        match Headers.tryGetFirstValue "X-DestinationType" headers with
        | ValueSome (EQI "GoogleCloudStorageRecord") ->
          match Headers.tryGetFirstValue "X-DestinationAccessToken" headers with
          | ValueSome accessToken ->
            match Headers.tryGetFirstValue "X-DestinationUri" headers with
            | ValueSome uriString ->
              let contentType =
                Headers.tryGetFirstValue "X-DestinationContentType" headers
                |? null
              let cacheControl =
                Headers.tryGetFirstValue "X-DestinationCacheControl" headers
                |? null
              let isPublic =
                Headers.tryGetFirstValue "X-DestinationIsPublic" headers
                |> ValueOption.contains "true"
              let uri = Uri (uriString, UriKind.Absolute)
              new GoogleStorageRecordDestination (uri, ViaAccessToken accessToken, contentType, cacheControl, isPublic)
              :> IImageDestination
              |> ValueSome
            | _ -> ValueNone
          | _ -> ValueNone
        | _ -> ValueNone
      async.Return result



