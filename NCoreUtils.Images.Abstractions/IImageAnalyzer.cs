using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines image analyzer functionality.
    /// </summary>
    public interface IImageAnalyzer
    {
        /// <summary>
        /// Retrieves image information from the specified source.
        /// </summary>
        /// <param name="source">Image source.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Image information.</returns>
        ValueTask<ImageInfo> AnalyzeAsync(IImageSource source, CancellationToken cancellationToken = default);
    }
}