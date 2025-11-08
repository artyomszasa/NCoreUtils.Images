using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images;

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

    public ValueTask ApplyFilterAsync(IFilter filter, CancellationToken cancellationToken)
        => default;

    public ValueTask CropAsync(Rectangle rect, CancellationToken cancellationToken)
    {
        Size = rect.Size;
        return default;
    }

    public ValueTask DisposeAsync()
        => default;

    public ValueTask<ImageInfo> GetImageInfoAsync(CancellationToken cancellationToken)
        => new(new ImageInfo(
            (int)Size.Width,
            (int)Size.Height,
            96,
            96,
            new Dictionary<string, string>(),
            new Dictionary<string, string>()
        ));

    public ValueTask NormalizeAsync(CancellationToken cancellationToken)
        => default;

    public ValueTask ResizeAsync(Size size, CancellationToken cancellationToken)
    {
        Size = size;
        return default;
    }

    public ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default)
        => DebugImageData.SerializeAsync(new DebugImageData((int)Size.Width, (int)Size.Height, imageType), stream, cancellationToken);
}