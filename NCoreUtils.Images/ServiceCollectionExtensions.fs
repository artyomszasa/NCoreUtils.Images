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
  static member AddImageResizer<'TProvider when 'TProvider : not struct and 'TProvider :> IImageProvider> (services : IServiceCollection,
                                                                                                           serviceLifetime : ServiceLifetime,
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
      |> ignore
    match serviceLifetime with
    | ServiceLifetime.Transient ->
      services
        .AddTransient<ImageResizer>()
        .AddTransient<IImageResizer, ImageResizer>()
        .AddTransient<IImageAnalyzer, ImageResizer>()
    | _ ->
      services.Add(ServiceDescriptor(typeof<ImageResizer>, typeof<ImageResizer>, serviceLifetime))
      services
        .AddTransient<IImageResizer>(fun serviceProvider -> serviceProvider.GetRequiredService<ImageResizer>() :> _)
        .AddTransient<IImageAnalyzer>(fun serviceProvider -> serviceProvider.GetRequiredService<ImageResizer>() :> _)


  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddImageResizer<'TProvider when 'TProvider : not struct and 'TProvider :> IImageProvider> (services : IServiceCollection,
                                                                                                           [<Optional; DefaultParameterValue(false)>] suppressDefaultResizers : bool,
                                                                                                           [<Optional; DefaultParameterValue(null:Action<ResizerCollectionBuilder>)>] configure : Action<ResizerCollectionBuilder>) =
    services.AddImageResizer<'TProvider>(ServiceLifetime.Transient, suppressDefaultResizers, configure)
