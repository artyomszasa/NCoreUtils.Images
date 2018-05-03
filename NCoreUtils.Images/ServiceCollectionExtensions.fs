namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open NCoreUtils.Images.Internal

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionNCoreUtilsImagesCoreExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddCoreImageResizer<'TProvider when 'TProvider : not struct and 'TProvider :> IImageProvider> (services : IServiceCollection) =
    services
      .AddSingleton<IImageProvider, 'TProvider>()
      .AddImageResizer<ImageResizer>()
