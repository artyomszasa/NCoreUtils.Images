namespace NCoreUtils.Images

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Extensions.DependencyInjection
open NCoreUtils.Images.Internal

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionNCoreUtilsImagesCoreExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddCoreImageResizer<'TProvider when 'TProvider : not struct and 'TProvider :> IImageProvider> (services : IServiceCollection,
                                                                                                               [<Optional; DefaultParameterValue(false)>] suppressDefaultResizers : bool,
                                                                                                               [<Optional; DefaultParameterValue(null:Action<ResizerCollectionBuilder>)>] configure : Action<ResizerCollectionBuilder>) =
    let builder = ResizerCollectionBuilder ()
    if not suppressDefaultResizers then
      builder
        .Add(ResizeModes.none, NoneResizerFactory ())
        .Add(ResizeModes.exact, ExactResizerFactory ())
        .Add(ResizeModes.inbox, InboxResizerFactory ())
        |> ignore
    if not (isNull configure) then
      configure.Invoke builder
    services
      .AddSingleton(builder.Build ())
      .AddSingleton<IImageProvider, 'TProvider>()
      .AddImageResizer<ImageResizer>()
