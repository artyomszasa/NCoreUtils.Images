using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images;

/// <summary>
/// Thrown if the supplied image is either unsupported or unprocessable.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class InvalidImageException : ImageException
{
#if !NET8_0_OR_GREATER
    protected InvalidImageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#endif

    public InvalidImageException(string description)
        : base(ErrorCodes.InvalidImage, description)
    { }

    public InvalidImageException(string description, Exception innerException)
        : base(ErrorCodes.InvalidImage, description, innerException)
    { }
}