using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImageMagick;

namespace NCoreUtils.Images.ImageMagick
{
    static class Helpers
    {
        static readonly Dictionary<MagickFormat, string> _magickFormatMap = new Dictionary<MagickFormat, string>
        {
            { MagickFormat.Jpg, ImageTypes.Jpeg },
            { MagickFormat.Jpeg, ImageTypes.Jpeg },
            { MagickFormat.Png, ImageTypes.Png },
            { MagickFormat.Png00, ImageTypes.Png },
            { MagickFormat.Png8, ImageTypes.Png },
            { MagickFormat.Png24, ImageTypes.Png },
            { MagickFormat.Png32, ImageTypes.Png },
            { MagickFormat.Png48, ImageTypes.Png },
            { MagickFormat.Png64, ImageTypes.Png },
            { MagickFormat.Bmp, ImageTypes.Bmp },
            { MagickFormat.Bmp2, ImageTypes.Bmp },
            { MagickFormat.Bmp3, ImageTypes.Bmp },
            { MagickFormat.Gif, ImageTypes.Gif },
            { MagickFormat.Gif87, ImageTypes.Gif },
            { MagickFormat.Tiff64, ImageTypes.Tiff },
            { MagickFormat.Tiff, ImageTypes.Tiff },
            { MagickFormat.WebP, ImageTypes.WebP }
        };

        static readonly Dictionary<string, MagickFormat> _imageTypeMap = new Dictionary<string, MagickFormat>
        {
            { "jpg", MagickFormat.Jpg },
            { ImageTypes.Jpeg, MagickFormat.Jpg },
            { ImageTypes.Png, MagickFormat.Png },
            { ImageTypes.Bmp, MagickFormat.Bmp },
            { ImageTypes.Gif, MagickFormat.Gif87 },
            { ImageTypes.Tiff, MagickFormat.Tiff },
            { ImageTypes.WebP, MagickFormat.WebP }
        };

        static string? ToString<T>(T value) => value?.ToString();

        public static string MagickFormatToImageType(MagickFormat format)
            => _magickFormatMap.TryGetValue(format, out var imageType) ? imageType : format.ToString();

        public static MagickFormat ImageTypeToMagickFormat(string imageType)
            => _imageTypeMap.TryGetValue(imageType, out var format)
                ? format
                : throw new InvalidOperationException($"Invalid image type \"{imageType}\".");

        public static string? StringifyImageData(object? obj)
            => obj switch
            {
                null => null,
                string s => s,
                long[] ix => $"i64[{string.Join(",", ix.Select(ToString))}]",
                int[] ix => $"i32[{string.Join(",", ix.Select(ToString))}]",
                short[] ix => $"i16[{string.Join(",", ix.Select(ToString))}]",
                sbyte[] ix => $"i8[{string.Join(",", ix.Select(ToString))}]",
                ulong[] ix => $"u64[{string.Join(",", ix.Select(ToString))}]",
                uint[] ix => $"u32[{string.Join(",", ix.Select(ToString))}]",
                ushort[] ix => $"u16[{string.Join(",", ix.Select(ToString))}]",
                byte[] ix => $"u8[{string.Join(",", ix.Select(ToString))}]",
                Guid guid => guid.ToString(),
                Rational r => $"f64[{((double)r.Numerator / (double)r.Denominator).ToString(CultureInfo.InvariantCulture)}]",
                Rational[] rs => $"f64[{string.Join(",", rs.Select(r => ((double)r.Numerator / (double)r.Denominator).ToString(CultureInfo.InvariantCulture)))}]",
                _ => null
            };
    }
}