namespace NCoreUtils.Images.GoogleCloudStorage

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Google.Apis.Auth.OAuth2
open NCoreUtils.Images

[<AutoOpen>]
module ImageResizerGoogleCloudStorageExt =

  type IImageResizer with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Uri, sourceCredential : GoogleCredential, destination : Uri, destinationCredential : GoogleCredential, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source, sourceCredential),
        new GoogleStorageRecordDestination (destination, GoogleStorageCredential.FromCredential destinationCredential),
        options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Uri, sourceAccessToken : string, destination : Uri, destinationAccessToken : string, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source, sourceAccessToken),
        new GoogleStorageRecordDestination (destination, ViaAccessToken destinationAccessToken),
        options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResizeOnGoogleStorage (source, destination, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source),
        new GoogleStorageRecordDestination (destination, ViaDefault),
        options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Uri, sourceCredential : GoogleCredential, destination : GoogleCloudStorageRecordDescriptor, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source, sourceCredential),
        new GoogleStorageRecordDestination (destination),
        options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResize (source : Uri, sourceAccessToken : string, destination : GoogleCloudStorageRecordDescriptor, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source, sourceAccessToken),
        new GoogleStorageRecordDestination (destination),
        options)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncResizeOnGoogleStorage (source, destination : GoogleCloudStorageRecordDescriptor, options) =
      this.AsyncResize (
        new GoogleStorageRecordProducer (source),
        new GoogleStorageRecordDestination (destination),
        options)

[<Extension>]
[<Sealed; AbstractClass>]
type ImageResizerGoogleCloudStorageExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source, sourceCredential : GoogleCredential, destination, destinationCredential : GoogleCredential, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source, sourceCredential),
      new GoogleStorageRecordDestination (destination, GoogleStorageCredential.FromCredential destinationCredential),
      options,
      cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source, sourceAccessToken : string, destination, destinationAccessToken : string, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source, sourceAccessToken),
      new GoogleStorageRecordDestination (destination, ViaAccessToken destinationAccessToken),
      options,
      cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeOnGoogleStorageAsync (this : IImageResizer, source, destination, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source),
      new GoogleStorageRecordDestination (destination, ViaDefault),
      options,
      cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source, sourceCredential : GoogleCredential, destination : GoogleCloudStorageRecordDescriptor, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source, sourceCredential),
      new GoogleStorageRecordDestination (destination),
      options,
      cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeAsync (this : IImageResizer, source, sourceAccessToken : string, destination : GoogleCloudStorageRecordDescriptor, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source, sourceAccessToken),
      new GoogleStorageRecordDestination (destination),
      options,
      cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member ResizeOnGoogleStorageAsync (this : IImageResizer, source, destination : GoogleCloudStorageRecordDescriptor, options, [<Optional>] cancellationToken) =
    this.ResizeAsync (
      new GoogleStorageRecordProducer (source),
      new GoogleStorageRecordDestination (destination),
      options,
      cancellationToken)