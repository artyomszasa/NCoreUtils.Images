using System.Runtime.CompilerServices;
using ImageMagick;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images.ImageMagick
{
    internal static class RectangleExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MagickGeometry ToMagickGeometry(this in Rectangle rect)
            => new(rect.X, rect.Y, rect.Width, rect.Height);
    }
}