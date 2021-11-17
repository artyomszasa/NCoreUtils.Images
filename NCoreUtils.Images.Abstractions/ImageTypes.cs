using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines supported image types and extension to/from media type conversions.
    /// </summary>
    public static class ImageTypes
    {
        private static readonly Regex _regexImageMime = new("^image/(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OfExtension(string ext)
            => ext switch
            {
                "jpg" => Jpeg,
                "tif" => Tiff,
                _     => ext
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToExtension(string imageType)
            => imageType switch
            {
                Jpeg => "jpg",
                _    => imageType
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToMediaType(string imageType)
            => imageType switch
            {
                Jpeg => "image/jpeg",
                Png => "image/png",
                Gif => "image/gif",
                WebP => "image/webp",
                _ => $"image/{imageType}"
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OfMediaType(string mediaType)
            => mediaType switch
            {
                "image/jpeg" => Jpeg,
                "image/p-jpeg" => Jpeg,
                "image/jpg" => Jpeg,
                _ => _regexImageMime.Match(mediaType) switch
                {
                    Match m when m.Success => m.Groups[1].Value,
                    _ => "unknown"
                }
            };
    }
}