using System;

namespace NCoreUtils.Images.Internal
{
    public class ExactResizerFactory : IResizerFactory
    {
        sealed class ExactResizer : IResizer
        {
            readonly Size _size;

            public ExactResizer(Size size)
                => _size = size;

            public void Resize(IImage image)
                => image.Resize(_size);
        }

        public static ExactResizerFactory Instance { get; } = new ExactResizerFactory();

        ExactResizerFactory() { }

        public IResizer CreateResizer(IImage image, ResizeOptions options)
        {
            Size size;
            if (options.Width.HasValue)
            {
                if (options.Height.HasValue)
                {
                    size = new Size(options.Width.Value, options.Height.Value);
                }
                else
                {
                    var imageSize = image.Size;
                    size = new Size(
                        options.Width.Value,
                        (int)((double)imageSize.Height / (double)imageSize.Width * (double)options.Width.Value)
                    );
                }
            }
            else
            {
                if (options.Height.HasValue)
                {
                    var imageSize = image.Size;
                    size = new Size(
                        (int)((double)imageSize.Width / (double)imageSize.Height * (double)options.Height.Value),
                        options.Height.Value
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
}