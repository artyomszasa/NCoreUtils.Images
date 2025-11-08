using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal;

public class ExactResizerFactory : IResizerFactory
{
    private sealed class ExactResizer(Size size) : IResizer
    {
        readonly Size _size = size;

        public ValueTask ResizeAsync(IImage image, CancellationToken cancellationToken = default)
            => image.ResizeAsync(_size, cancellationToken);
    }

    public static ExactResizerFactory Instance { get; } = new ExactResizerFactory();

    private ExactResizerFactory() { }

    public IResizer CreateResizer(IImage image, ResizeOptions options)
    {
        Size size;
        if (options.Width is int width)
        {
            if (options.Height is int height)
            {
                size = new Size((uint)width, (uint)height);
            }
            else
            {
                var imageSize = image.Size;
                size = new Size(
                    (uint)width,
                    (uint)((double)imageSize.Height / imageSize.Width * width)
                );
            }
        }
        else
        {
            if (options.Height is int height)
            {
                var imageSize = image.Size;
                size = new Size(
                    (uint)((double)imageSize.Width / imageSize.Height * height),
                    (uint)height
                );
            }
            else
            {
                throw new InvalidOperationException("Output image dimensions must be specified when using exact resizing.");
            }
        }
        return new ExactResizer(size);
    }
}