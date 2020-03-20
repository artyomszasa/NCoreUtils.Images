using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images
{
    public interface IImageAnalyzer
    {
        Task<ImageInfo> AnalyzeAsync(IImageSource source, CancellationToken cancellationToken = default);
    }
}