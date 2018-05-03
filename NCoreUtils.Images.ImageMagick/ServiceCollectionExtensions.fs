namespace NCoreUtils.Images

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionNCoreUtilsImagesImageMagickExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddImageMagickResizer (services : IServiceCollection) =
    services.AddCoreImageResizer<ImageMagick.ImageProvider>()