using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images.ImageMagick
{
    public class ImageProvider : IImageProvider
    {
        public long MemoryLimit
        {
            get => (long)ResourceLimits.Memory;
            set => ResourceLimits.Memory = (ulong)value;
        }

        public IImage FromStream(Stream source)
            => new Image(new MagickImage(source), this);

        public ValueTask<IImage> FromStreamAsync(Stream source, CancellationToken cancellationToken = default)
            => new ValueTask<IImage>(FromStream(source));
    }
}