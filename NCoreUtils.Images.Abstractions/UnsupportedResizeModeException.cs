using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images;

/// <summary>
/// Thrown if the requested sizing is not supported.
/// </summary>
[Serializable]
public class UnsupportedResizeModeException : ImageException
{
    static int? GetNInt(SerializationInfo info, string key)
    {
        var i = info.GetInt32(key);
        return -1 == i ? (int?)null : i;
    }

    public string ResizeMode { get; }

    public int? Width { get; }

    public int? Height { get; }

    protected UnsupportedResizeModeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ResizeMode = info.GetString(nameof(ResizeMode)) ?? string.Empty;
        Width = GetNInt(info, nameof(Width));
        Height = GetNInt(info, nameof(Height));
    }

    public UnsupportedResizeModeException(string resizeMode, int? width, int? height, string description)
        : base(ErrorCodes.UnsupportedResizeMode, description)
    {
        ResizeMode = resizeMode;
        Width = width;
        Height = height;
    }

    public UnsupportedResizeModeException(string resizeMode, int? width, int? height, string description, Exception innerException)
        : base(ErrorCodes.UnsupportedResizeMode, description, innerException)
    {
        ResizeMode = resizeMode;
        Width = width;
        Height = height;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ResizeMode), ResizeMode);
        info.AddValue(nameof(Width), Width ?? -1);
        info.AddValue(nameof(Height), Height ?? -1);
    }
}