using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal;

public interface IImageProvider
{
    long MemoryLimit { get; set; }

    ValueTask<IImage> FromStreamAsync(Stream source, CancellationToken cancellationToken = default);
}