using System.Runtime.CompilerServices;

namespace NCoreUtils.Images
{
    public static class ImageTypes
    {
        public const string Jpeg = "jpeg";

        public const string Bmp = "bmp";

        public const string Png = "png";

        public const string Gif = "gif";

        public const string Tiff = "tiff";

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
    }
}