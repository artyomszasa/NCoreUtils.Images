using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images;

/// <summary>
/// Thrown if the requested output image type is not supported.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class UnsupportedImageTypeException : ImageException
{
    public string ImageType { get; }

#if !NET8_0_OR_GREATER
    protected UnsupportedImageTypeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => ImageType = info.GetString(nameof(ImageType)) ?? string.Empty;
#endif

    public UnsupportedImageTypeException(string imageType, string description, Exception innerException)
        : base(ErrorCodes.UnsupportedImageType, description, innerException)
        => ImageType = imageType;

    public UnsupportedImageTypeException(string imageType, string description)
        : base(ErrorCodes.UnsupportedImageType, description)
        => ImageType = imageType;

#if !NET8_0_OR_GREATER
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ImageType), ImageType);
    }
#endif
}