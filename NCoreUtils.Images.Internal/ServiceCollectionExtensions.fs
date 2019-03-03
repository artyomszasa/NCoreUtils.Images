namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open System

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionImageExtractorsExtensions =

  [<Extension>]
  static member AddImageSourceExtractors (this : IServiceCollection, configure : Action<CompositeImageSourceExtractorBuilder>) =
    let builder = CompositeImageSourceExtractorBuilder this
    configure.Invoke builder
    let compositeExtractor = CompositeImageSourceExtractor builder
    this.AddSingleton<IImageSourceExtractor> compositeExtractor

  [<Extension>]
  static member AddImageDestinationExtractors (this : IServiceCollection, configure : Action<CompositeImageDestinationExtractorBuilder>) =
    let builder = CompositeImageDestinationExtractorBuilder this
    configure.Invoke builder
    let compositeExtractor = CompositeImageDestinationExtractor builder
    this.AddSingleton<IImageDestinationExtractor> compositeExtractor