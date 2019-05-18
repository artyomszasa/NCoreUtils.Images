namespace NCoreUtils.Images

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Extensions.DependencyInjection
open NCoreUtils.Images.Configuration
open NCoreUtils.Images.Providers

[<Extension>]
[<AbstractClass; Sealed>]
type ServiceCollectionImageResizerExtensions =

  [<Extension>]
  static member AddImageResizer<'TImageResizer, 'TImageProvider when 'TImageResizer :> IImageResizer and 'TImageProvider :> ImageProvider>
    (services : IServiceCollection,
      imageResizerLifetime : ServiceLifetime,
      imageProviderLifetime : ServiceLifetime,
      [<Optional; DefaultParameterValue(null : Action<ImageResizerConfigurationBuilder>)>] configure : Action<ImageResizerConfigurationBuilder>
    ) =
      let builder = ImageResizerConfigurationBuilder ()
      if not (isNull configure) then
        configure.Invoke builder
      services.AddSingleton builder.Options |> ignore
      services.AddSingleton (builder.ResizerCollectionBuilder.Build ()) |> ignore
      services.Add <| ServiceDescriptor (typeof<IImageResizer>, typeof<'TImageResizer>,  imageResizerLifetime)
      services.Add <| ServiceDescriptor (typeof<ImageProvider>, typeof<'TImageProvider>, imageProviderLifetime)
      services

  [<Extension>]
  static member AddImageResizer<'TImageResizer, 'TImageProvider when 'TImageResizer :> IImageResizer and 'TImageProvider :> ImageProvider>
    (services : IServiceCollection,
      [<Optional; DefaultParameterValue(null : Action<ImageResizerConfigurationBuilder>)>] configure : Action<ImageResizerConfigurationBuilder>
    ) =
      services.AddImageResizer<'TImageResizer, 'TImageProvider> (ServiceLifetime.Singleton, ServiceLifetime.Singleton, configure)

  [<Extension>]
  static member AddImageResizer<'TImageProvider when 'TImageProvider :> ImageProvider>
    (services : IServiceCollection,
      [<Optional; DefaultParameterValue(null : Action<ImageResizerConfigurationBuilder>)>] configure : Action<ImageResizerConfigurationBuilder>
    ) =
      services.AddImageResizer<ImageResizer, 'TImageProvider> (configure)
