namespace NCoreUtils.Images

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Http
open NCoreUtils.IO
open NCoreUtils
open Microsoft.Extensions.DependencyInjection

[<Sealed>]
type DefaultImageSourceExtractor () =
  static member val SharedInstance = DefaultImageSourceExtractor () :> IImageSourceExtractor
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.AsyncExtractImageSource (httpContext : HttpContext) =
    { new IImageSource with
        member __.AsyncCreateProducer () =
          { new IStreamProducer with
              member __.AsyncProduce output = Stream.asyncCopyTo output httpContext.Request.Body
              member __.Dispose () = ()
          }
          |> async.Return
    }
    |> ValueSome
    |> async.Return

  interface IImageSourceExtractor with
    member this.AsyncExtractImageSource httpContext = this.AsyncExtractImageSource httpContext

[<Sealed>]
type DefaultImageDestinationExtractor () =
  static member val SharedInstance = DefaultImageDestinationExtractor () :> IImageDestinationExtractor
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.AsyncExtractImageDestination (httpContext : HttpContext) =
    { new IImageDestination with
        member __.AsyncWrite (contentInfo, generator) =
          let response = httpContext.Response
          if System.String.IsNullOrEmpty contentInfo.ContentType |> not then
            response.ContentType <- contentInfo.ContentType
          if contentInfo.ContentLength.HasValue then
            response.ContentLength <- contentInfo.ContentLength
          async.Bind (generator response.Body, fun () -> response.Body.AsyncFlush ())
        member __.AsyncWriteDelayed generator =
          let response = httpContext.Response
          let setContentInfo (contentInfo : ContentInfo) =
            if not response.HasStarted then
              if System.String.IsNullOrEmpty contentInfo.ContentType |> not then
                response.ContentType <- contentInfo.ContentType
              if contentInfo.ContentLength.HasValue then
                response.ContentLength <- contentInfo.ContentLength
          async.Bind (generator setContentInfo response.Body, fun () -> response.Body.AsyncFlush ())
    }
    |> ValueSome
    |> async.Return

  interface IImageDestinationExtractor with
    member this.AsyncExtractImageDestination httpContext = this.AsyncExtractImageDestination httpContext

type CompositeImageSourceExtractorBuilder (serviceCollection : IServiceCollection) =
  member val Extractors = ResizeArray<Type> ()

  member val Services   = serviceCollection

  member this.AddExtractor (extractorType : Type, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    let factory = Func<IServiceProvider, obj> (fun serviceProvider -> ActivatorUtilities.CreateInstance (serviceProvider, extractorType))
    this.Services.Add <| ServiceDescriptor (extractorType, factory, lifetime)
    this.Extractors.Add extractorType
    this

  [<RequiresExplicitTypeArguments>]
  member this.AddExtractor<'TExtractor when 'TExtractor :> IImageSourceExtractor> ([<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    this.AddExtractor (typeof<'TExtractor>, lifetime)

  member this.InsertExtractor (index, extractorType : Type, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    let factory = Func<IServiceProvider, obj> (fun serviceProvider -> ActivatorUtilities.CreateInstance (serviceProvider, extractorType))
    this.Services.Add <| ServiceDescriptor (extractorType, factory, lifetime)
    this.Extractors.Insert (index, extractorType)
    this

  [<RequiresExplicitTypeArguments>]
  member this.InsertExtractor<'TExtractor when 'TExtractor :> IImageSourceExtractor> (index, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    this.InsertExtractor (index, typeof<'TExtractor>, lifetime)

type CompositeImageSourceExtractor (builder : CompositeImageSourceExtractorBuilder) =
  let extractorTypes = builder.Extractors.ToArray ()
  interface IImageSourceExtractor with
    member __.AsyncExtractImageSource httpContext =
      let rec impl index =
        match index = extractorTypes.Length with
        | true -> async.Return ValueNone
        | _ ->
          async {
            let extractorType = extractorTypes.[index]
            match httpContext.RequestServices.GetService extractorType with
            | null -> return invalidOpf "Unable to resolve %s" extractorType.FullName
            | :? IImageSourceExtractor as extractor ->
              match! extractor.AsyncExtractImageSource httpContext with
              | ValueSome _ as result -> return result
              | _ -> return! impl (index + 1)
            | inst -> return invalidOpf "Instance of type %s cannot be used as image source extractor" (inst.GetType().FullName) }
      impl 0

type CompositeImageDestinationExtractorBuilder (serviceCollection : IServiceCollection) =
  member val Extractors = ResizeArray<Type> ()
  member val Services   = serviceCollection
  member this.AddExtractor (extractorType : Type, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    let factory = Func<IServiceProvider, obj> (fun serviceProvider -> ActivatorUtilities.CreateInstance (serviceProvider, extractorType))
    this.Services.Add <| ServiceDescriptor (extractorType, factory, lifetime)
    this.Extractors.Add extractorType
    this
  [<RequiresExplicitTypeArguments>]
  member this.AddExtractor<'TExtractor when 'TExtractor :> IImageDestinationExtractor> ([<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    this.AddExtractor (typeof<'TExtractor>, lifetime)
  member this.InsertExtractor (index, extractorType : Type, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    let factory = Func<IServiceProvider, obj> (fun serviceProvider -> ActivatorUtilities.CreateInstance (serviceProvider, extractorType))
    this.Services.Add <| ServiceDescriptor (extractorType, factory, lifetime)
    this.Extractors.Insert (index, extractorType)
    this
  [<RequiresExplicitTypeArguments>]
  member this.InsertExtractor<'TExtractor when 'TExtractor :> IImageDestinationExtractor> (index, [<Optional; DefaultParameterValue(ServiceLifetime.Singleton)>] lifetime : ServiceLifetime) =
    this.InsertExtractor (index, typeof<'TExtractor>, lifetime)

type CompositeImageDestinationExtractor (builder : CompositeImageDestinationExtractorBuilder) =
  let extractorTypes = builder.Extractors.ToArray ()
  interface IImageDestinationExtractor with
    member __.AsyncExtractImageDestination httpContext =
      let rec impl index =
        match index = extractorTypes.Length with
        | true -> async.Return ValueNone
        | _ ->
          async {
            let extractorType = extractorTypes.[index]
            match httpContext.RequestServices.GetService extractorType with
            | null -> return invalidOpf "Unable to resolve %s" extractorType.FullName
            | :? IImageDestinationExtractor as extractor ->
              match! extractor.AsyncExtractImageDestination httpContext with
              | ValueSome _ as result -> return result
              | _ -> return! impl (index + 1)
            | inst -> return invalidOpf "Instance of type %s cannot be used as image destination extractor" (inst.GetType().FullName) }
      impl 0

