namespace NCoreUtils.Images.Server

open System
open System.Collections.Generic
open NCoreUtils.Images

type Meta = IReadOnlyDictionary<string, string>

type IImageSourceExtractor =
  abstract TryExtract : meta:Meta -> ValueOption<IImageSource>

type IImageDestinationExtractor =
  abstract TryExtract : meta:Meta -> ValueOption<IImageDestination>

type ImageSourceExtractorCollectionBuilder () =
  let mutable extractors = ResizeArray ()

  member internal __.Extractors = extractors

  member this.Add (``type``) =
    extractors.Add ``type``
    this

  member this.Insert (index : int, ``type``) =
    extractors.Insert (index, ``type``)
    this

  [<RequiresExplicitTypeArguments>]
  member this.Add<'T when 'T :> IImageSourceExtractor> () =
    this.Add typeof<'T>

  [<RequiresExplicitTypeArguments>]
  member this.Insert<'T when 'T :> IImageSourceExtractor> index =
    this.Insert (index, typeof<'T>)

type ImageDestinationExtractorCollectionBuilder () =
  let mutable extractors = ResizeArray ()

  member internal __.Extractors = extractors

  member this.Add (``type``) =
    extractors.Add ``type``
    this

  member this.Insert (index : int, ``type``) =
    extractors.Insert (index, ``type``)
    this

  [<RequiresExplicitTypeArguments>]
  member this.Add<'T when 'T :> IImageDestinationExtractor> () =
    this.Add typeof<'T>

  [<RequiresExplicitTypeArguments>]
  member this.Insert<'T when 'T :> IImageDestinationExtractor> index =
    this.Insert (index, typeof<'T>)

type MetadataExtractorConfigurationBulder () =
  let sources      = ImageSourceExtractorCollectionBuilder ()
  let destinations = ImageDestinationExtractorCollectionBuilder ()

  member __.SourceExtractors = sources

  member __.DestinationExtractors = destinations

  member this.ConfigureSourceExtractors (configure : Action<ImageSourceExtractorCollectionBuilder>) =
    configure.Invoke sources
    this

  member this.ConfigureDestinationExtractors (configure : Action<ImageDestinationExtractorCollectionBuilder>) =
    configure.Invoke destinations
    this

  member this.AddSourceExtractor ``type`` =
    sources.Add ``type`` |> ignore
    this

  member this.AddDestinationExtractor ``type`` =
    destinations.Add ``type`` |> ignore
    this

  member this.AddSourceExtractor<'T when 'T :> IImageSourceExtractor> () =
    this.AddSourceExtractor typeof<'T>

  member this.AddDestinationExtractor<'T when 'T :> IImageDestinationExtractor> () =
    this.AddDestinationExtractor typeof<'T>

[<AllowNullLiteral>]
type MetadataExtractorConfiguration =
  val public SourceExtractors      : IReadOnlyList<Type>
  val public DestinationExtractors : IReadOnlyList<Type>

  new (builder : MetadataExtractorConfigurationBulder) =
    { SourceExtractors      = builder.SourceExtractors.Extractors      |> Seq.toArray
      DestinationExtractors = builder.DestinationExtractors.Extractors |> Seq.toArray }
