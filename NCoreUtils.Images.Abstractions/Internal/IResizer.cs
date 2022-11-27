using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal;

public interface IResizer
{
    ValueTask ResizeAsync(IImage image, CancellationToken cancellationToken = default);
}