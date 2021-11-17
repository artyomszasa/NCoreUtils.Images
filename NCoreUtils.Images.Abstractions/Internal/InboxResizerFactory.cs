using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal
{
    public class InboxResizerFactory : IResizerFactory
    {
        sealed class InboxResize : IResizer
        {
            readonly Rectangle _rect;

            readonly Size _box;

            public InboxResize(Rectangle rect, Size box)
            {
                _rect = rect;
                _box = box;
            }

            public async ValueTask ResizeAsync(IImage image, CancellationToken cancellationToken = default)
            {
                await image.CropAsync(_rect, cancellationToken);
                await image.ResizeAsync(_box, CancellationToken.None);
            }
        }

        public static InboxResizerFactory Instance { get; } = new InboxResizerFactory();

        static Rectangle CalculateRect(int? weightX, int? weightY, Size source, Size box)
        {
            // preconvert to double
            var boxWidth = (double)box.Width;
            var boxHeight = (double)box.Height;
            var sourceWidth = (double)source.Width;
            var sourceHeight = (double)source.Height;
            // ---
            var resizeWidthRatio  = sourceWidth / boxWidth;
            var resizeHeightRatio = sourceHeight / boxHeight;
            if (resizeWidthRatio < resizeHeightRatio)
            {
                // maximize width in box
                var inputHeight = (int)(boxHeight / boxWidth * sourceWidth);
                var margin = (source.Height - inputHeight) / 2;
                if (weightY.HasValue)
                {
                    var diff = (double)weightY.Value - sourceHeight / 2.0;
                    var normalized = diff / (sourceHeight / 2.0);
                    var mul = 1.0 + normalized;
                    margin = (int)((double)margin * mul);
                }
                return new Rectangle(0, margin, source.Width, inputHeight);
            }
            else
            {
                var inputWidth = (int)(boxWidth / boxHeight * sourceHeight);
                var margin = (source.Width - inputWidth) / 2;
                if (weightX.HasValue)
                {
                    var diff = (double)weightX.Value - sourceWidth / 2.0;
                    var normalized = diff / (sourceWidth / 2.0);
                    var mul = 1.0 + normalized;
                    margin = (int)((double)margin * mul);
                }
                return new Rectangle(margin, 0, inputWidth, source.Height);
            }
        }

        InboxResizerFactory() { }

        public IResizer CreateResizer(IImage image, ResizeOptions options)
        {
            if (!(options.Width.HasValue && options.Height.HasValue))
            {
                throw new UnsupportedResizeModeException(
                    "inbox",
                    options.Width,
                    options.Height,
                    "Exact image dimensions must be specified when using inbox resizing."
                );
            }
            var box = new Size(options.Width.Value, options.Height.Value);
            var rect = CalculateRect(options.WeightX, options.WeightY, image.Size, box);
            return new InboxResize(rect, box);
        }
    }
}