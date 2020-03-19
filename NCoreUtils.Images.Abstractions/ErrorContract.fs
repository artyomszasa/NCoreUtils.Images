namespace NCoreUtils.Images

open System
open System.Runtime.Serialization

/// Contains predefined error codes.
[<RequireQualifiedAccess>]
module ErrorCodes =

  /// Resize method not supported.
  [<Literal>]
  let UnsupportedResizeMode = "unsupported_resize_mode"

  /// Requested output image type not supported.
  [<Literal>]
  let UnsupportedImageType = "unsupported_image_type"

  /// Input image is invalid or not supported.
  [<Literal>]
  let InvalidImage = "invalid_image"

  /// Provided size incompatible with the provided resize mode.
  [<Literal>]
  let InvalidSize = "invalid_size"

  /// Implementation specific error occured while preforming resize or get-info operation.
  [<Literal>]
  let InternalError = "internal_error"

  /// Implementation specific error occured while preforming resize or get-info operation.
  [<Literal>]
  let GenericError = "generic_error"

[<RequireQualifiedAccess>]
module internal ImageExceptionKeys =

  [<Literal>]
  let ErrorCode = "ErrorCode"

  [<Literal>]
  let ImageType = "ImageType"

  [<Literal>]
  let InternalCode = "InternalCode"

  [<Literal>]
  let ResizeMode = "ResizeMode"

/// <summary>
/// Represents generic image processing error.
/// </summary>
[<Serializable>]
type ImageException =
  inherit Exception
  val public ErrorCode: string
  new (errorCode: string, description: string) =
    if isNull errorCode then nameof errorCode |> nullArg
    { inherit Exception (description)
      ErrorCode = errorCode }
  new (errorCode: string, description: string, innerException: exn) =
    if isNull errorCode then nameof errorCode |> nullArg
    { inherit Exception (description, innerException)
      ErrorCode = errorCode }
  new (info: SerializationInfo, context) =
    { inherit Exception (info, context)
      ErrorCode = info.GetString ImageExceptionKeys.ErrorCode }
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (ImageExceptionKeys.ErrorCode, this.ErrorCode)

/// <summary>
/// Thrown if the requested sizing is not supported.
/// </summary>
[<Serializable>]
type UnsupportedResizeModeException =
  inherit ImageException
  val public ResizeMode : string
  new (resizeMode, description : string) =
    { inherit ImageException (ErrorCodes.UnsupportedResizeMode, description)
      ResizeMode = resizeMode }
  new (resizeMode, description: string, innerException : exn) =
    { inherit ImageException (ErrorCodes.UnsupportedResizeMode, description, innerException)
      ResizeMode = resizeMode }
  new (info: SerializationInfo, context) =
    { inherit ImageException (info, context)
      ResizeMode = info.GetString ImageExceptionKeys.ResizeMode }
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (ImageExceptionKeys.ResizeMode, this.ResizeMode)

/// <summary>
/// Thrown if the requested output image type is not supported.
/// </summary>
[<Serializable>]
type UnsupportedImageTypeException =
  inherit ImageException
  val public ImageType : string
  new (imageType, description : string) =
    { inherit ImageException (ErrorCodes.UnsupportedImageType, description)
      ImageType = imageType }
  new (imageType, description : string, innerException : exn) =
    { inherit ImageException (ErrorCodes.UnsupportedImageType, description, innerException)
      ImageType = imageType }
  new (info: SerializationInfo, context) =
    { inherit ImageException (info, context)
      ImageType = info.GetString ImageExceptionKeys.ImageType }
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (ImageExceptionKeys.ImageType, this.ImageType)

/// <summary>
/// Thrown if the supplied image is either unsupported or unprocessable.
/// </summary>
[<Serializable>]
type InvalidImageException =
  inherit ImageException
  new (description) =
    { inherit ImageException (ErrorCodes.InvalidImage, description) }
  new (description, innerException : exn) =
    { inherit ImageException (ErrorCodes.InvalidImage, description, innerException) }
  new (info: SerializationInfo, context) =
    { inherit ImageException (info, context) }

/// <summary>
/// Represents invalid sizing request.
/// </summary>
[<Serializable>]
type InvalidSizeException =
  inherit ImageException
  new (description) =
    { inherit ImageException (ErrorCodes.InvalidSize, description) }
  new (description, innerException : exn) =
    { inherit ImageException (ErrorCodes.InvalidSize, description, innerException) }
  new (info: SerializationInfo, context) =
    { inherit ImageException (info, context) }

/// <summary>
/// Represents error that has occured in image implementation.
/// </summary>
[<Serializable>]
type InternalImageException =
  inherit ImageException
  val public InternalCode : string
  new (internalCode, description) =
    { inherit ImageException (ErrorCodes.InternalError, description)
      InternalCode = internalCode }
  new (internalCode, description, innerException : exn) =
    { inherit ImageException (ErrorCodes.InternalError, description, innerException)
      InternalCode = internalCode }
  new (info: SerializationInfo, context) =
    { inherit ImageException (info, context)
      InternalCode = info.GetString ImageExceptionKeys.InternalCode }
  override this.GetObjectData (info, context) =
    base.GetObjectData (info, context)
    info.AddValue (ImageExceptionKeys.InternalCode, this.InternalCode)

module ImageError =

  [<CompiledName("ThrowImageException")>]
  let generic (errorCode : string) description =
    ImageException (errorCode, description) |> raise

  [<CompiledName("FormatDescriptionThenThrowImageException")>]
  let genericf errorCode fmt =
    Printf.kprintf (generic errorCode) fmt

  [<CompiledName("ThrowUnsupportedResizeModeException")>]
  let resizeMode (mode: string) description =
    UnsupportedResizeModeException (mode, description) |> raise

  [<CompiledName("FormatDescriptionThenThrowUnsupportedResizeModeException")>]
  let resizeModef mode fmt =
    Printf.kprintf (resizeMode mode) fmt

  [<CompiledName("ThrowUnsupportedImageTypeException")>]
  let unsupportedImage (imageType: string) (description: string) =
    UnsupportedImageTypeException (imageType, description) |> raise

  [<CompiledName("FormatDescriptionThenThrowUnsupportedImageTypeException")>]
  let unsupportedImagef imageType fmt =
    Printf.kprintf (unsupportedImage imageType) fmt

  [<CompiledName("ThrowInvalidImageException")>]
  let invalidImage (description: string) =
    InvalidImageException (description) |> raise

  [<CompiledName("FormatDescriptionThenThrowInvalidImageException")>]
  let invalidImagef fmt =
    Printf.kprintf invalidImage fmt

  [<CompiledName("ThrowInvalidSizeException")>]
  let invalidSize (description: string) =
    InvalidImageException (description) |> raise

  [<CompiledName("FormatDescriptionThenThrowInvalidSizeException")>]
  let invalidSizef fmt =
    Printf.kprintf invalidSize fmt