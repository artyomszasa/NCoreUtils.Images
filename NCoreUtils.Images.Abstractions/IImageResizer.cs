using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images
{
    public interface IImageResizer
    {
        Task ResizeAsync(IImageSource source, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default);
    }
}