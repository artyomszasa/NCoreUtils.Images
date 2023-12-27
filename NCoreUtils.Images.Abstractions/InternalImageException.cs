using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images;

/// <summary>
/// Represents error that has occured in image implementation.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class InternalImageException : ImageException
{
    public string InternalCode { get; }

#if !NET8_0_OR_GREATER
    protected InternalImageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => InternalCode = info.GetString(nameof(InternalCode)) ?? string.Empty;
#endif

    public InternalImageException(string internalCode, string description)
        : base(ErrorCodes.InternalError, description)
        => InternalCode = internalCode;

    public InternalImageException(string internalCode, string description, Exception innerException)
        : base(ErrorCodes.InternalError, description, innerException)
        => InternalCode = internalCode;

#if !NET8_0_OR_GREATER
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(InternalCode), InternalCode);
    }
#endif
}