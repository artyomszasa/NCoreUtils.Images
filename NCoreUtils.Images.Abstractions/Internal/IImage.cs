using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal;

public interface IImage : IAsyncDisposable
{
    IImageProvider Provider { get; }

    Size Size { get; }

    object NativeImageType { get; }

    string ImageType { get; }

    ValueTask ApplyFilterAsync(IFilter filter, CancellationToken cancellationToken = default);

    ValueTask CropAsync(Rectangle rect, CancellationToken cancellationToken = default);

    ValueTask<ImageInfo> GetImageInfoAsync(CancellationToken cancellationToken = default);

    ValueTask NormalizeAsync(CancellationToken cancellationToken = default);

    ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default);

    ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default);
}