using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images
{
    public sealed class DebugImage : IImage
    {
        public IImageProvider Provider => throw new System.NotImplementedException();

        public Size Size { get; private set; }

        public object NativeImageType => ImageType;

        public string ImageType { get; }

        public DebugImage(Size size, string imageType)
        {
            Size = size;
            ImageType = imageType;
        }

        public void ApplyFilter(IFilter filter) { /* noop */ }

        public void Crop(Rectangle rect)
        {
            Size = rect.Size;
        }

        public void Dispose() { /* noop */ }

        public ImageInfo GetImageInfo()
            => new ImageInfo(
                Size.Width,
                Size.Height,
                96,
                96,
                new Dictionary<string, string>(),
                new Dictionary<string, string>()
            );

        public void Normalize() { /* noop */ }

        public void Resize(Size size)
        {
            Size = size;
        }

        public ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default)
            => DebugImageData.SerializeAsync(new DebugImageData(Size.Width, Size.Height, imageType), stream, cancellationToken);
    }
}