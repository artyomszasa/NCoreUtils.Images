using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images
{
    public sealed class DebugImageProvider : IImageProvider
    {
        public long MemoryLimit { get; set; }

        public async ValueTask<IImage> FromStreamAsync(Stream source, CancellationToken cancellationToken = default)
        {
            var data = await DebugImageData.DeserializeAsync(source, cancellationToken);
            return new DebugImage(new Size(data!.Width, data!.Height), data!.ImageType);
        }
    }
}