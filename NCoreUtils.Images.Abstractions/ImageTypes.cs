using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NCoreUtils.Images;

/// <summary>
/// Defines supported image types and extension to/from media type conversions.
/// </summary>
public static class ImageTypes
{
    private static bool IsImageMime(string mime, [MaybeNullWhen(false)] out string subtype)
    {
        if (mime is null || mime.Length < 7 || !mime.StartsWith("image/"))
        {
            subtype = default;
            return false;
        }
        subtype = mime[6..];
        return true;
    }

    /// <summary>
    /// Represents JPEG image.
    /// </summary>
    public const string Jpeg = "jpeg";

    /// <summary>
    /// Represents BMP image.
    /// </summary>
    public const string Bmp = "bmp";

    /// <summary>
    /// Represents PNG image.
    /// </summary>
    public const string Png = "png";

    /// <summary>
    /// Represents GIF image.
    /// </summary>
    public const string Gif = "gif";

    /// <summary>
    /// Represents TIFF image.
    /// </summary>
    public const string Tiff = "tiff";

    /// <summary>
    /// Represents WEBP image.
    /// </summary>
    public const string WebP = "webp";

    /// <summary>
    /// Represents PDF "image".
    /// </summary>
    public const string Pdf = "pdf";

    /// <summary>
    /// Represents ICO "image".
    /// </summary>
    public const string Ico = "ico";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OfExtension(string ext) => ext switch
    {
        "jpg" => Jpeg,
        "tif" => Tiff,
        _     => ext
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToExtension(string imageType) => imageType switch
    {
        Jpeg => "jpg",
        _    => imageType
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToMediaType(string imageType) => imageType switch
    {
        Jpeg => "image/jpeg",
        Png => "image/png",
        Gif => "image/gif",
        WebP => "image/webp",
        Pdf => "application/pdf",
        Ico => "image/x-icon",
        _ => $"image/{imageType}"
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OfMediaType(string mediaType) => mediaType switch
    {
        "image/jpeg" => Jpeg,
        "image/p-jpeg" => Jpeg,
        "image/jpg" => Jpeg,
        "application/pdf" => Pdf,
        "application/x-pdf" => Pdf,
        "image/x-icon" => Ico,
        _ => IsImageMime(mediaType, out var subtype)
            ? subtype
            : "unknown"
    };
}