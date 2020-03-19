namespace NCoreUtils.Images

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionNCoreUtilsImagesImageMagickExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddImageMagickResizer (services : IServiceCollection,
                                       [<Optional; DefaultParameterValue(false)>] suppressDefaultResizers : bool,
                                       [<Optional; DefaultParameterValue(null:Action<ResizerCollectionBuilder>)>] configure : Action<ResizerCollectionBuilder>) =
    services.AddImageResizer<ImageMagick.ImageProvider>(ServiceLifetime.Singleton, suppressDefaultResizers, configure)