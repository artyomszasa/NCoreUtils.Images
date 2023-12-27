using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images;

/// <summary>
/// Represents generic image processing error.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class ImageException : Exception
{
    /// <summary>
    /// Related error code (see <see cref="ErrorCodes" />).
    /// </summary>
    public string ErrorCode { get; }

#if !NET8_0_OR_GREATER
    protected ImageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => ErrorCode = info.GetString(nameof(ErrorCode)) ?? string.Empty;
#endif

    public ImageException(string errorCode, string description, Exception innerException)
        : base(description, innerException)
        => ErrorCode = errorCode;

    public ImageException(string errorCode, string description)
        : base(description)
        => ErrorCode = errorCode;

#if !NET8_0_OR_GREATER
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
    }
#endif
}