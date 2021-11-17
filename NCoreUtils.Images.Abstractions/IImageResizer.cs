using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines image resizer and converter functionality.
    /// </summary>
    public interface IImageResizer
    {
        /// <summary>
        /// Asynchronously performs operation on the image defined by <paramref name="source" /> writing out to the
        /// <paramref name="destination" />. Performed operation is defined by the <paramref name="options" />.
        /// </summary>
        /// <param name="source">Input image source.</param>
        /// <param name="destination">Output image destination.</param>
        /// <param name="options">Conversion options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask ResizeAsync(IImageSource source, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default);
    }
}