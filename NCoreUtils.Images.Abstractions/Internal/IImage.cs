using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal
{
    public interface IImage : IDisposable
    {
        IImageProvider Provider { get; }

        Size Size { get; }

        object NativeImageType { get; }

        string ImageType { get; }

        void Crop(Rectangle rect);

        ImageInfo GetImageInfo();

        void Normalize();

        void Resize(Size size);

        ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default);
    }
}