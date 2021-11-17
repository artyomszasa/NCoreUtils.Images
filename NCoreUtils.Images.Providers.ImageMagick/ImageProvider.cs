using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images.ImageMagick
{
    public class ImageProvider : IImageProvider
    {
        // private static MagickImageFactory Factory { get; } = new MagickImageFactory();

        private static MagickImageCollectionFactory Factory { get; } = new MagickImageCollectionFactory();

        public long MemoryLimit
        {
            get => (long)ResourceLimits.Memory;
            set => ResourceLimits.Memory = (ulong)value;
        }

        async ValueTask<IImage> IImageProvider.FromStreamAsync(Stream source, CancellationToken cancellationToken)
            => await FromStreamAsync(source,cancellationToken);

        public ValueTask<Image> FromStreamAsync(Stream source, CancellationToken cancellationToken = default)
        {
            var settings = new MagickReadSettings
            {
                Density = new Density(600, 600), // higher PDF quality
#if DEBUG
                Verbose = true
#endif
            };
            var nativeTask = Factory.CreateAsync(source, settings, cancellationToken);
            if (nativeTask.IsCompletedSuccessfully)
            {
                return new ValueTask<Image>(new Image(nativeTask.Result, this));
            }
            return CompleteInstantiation(this, nativeTask);

            static async ValueTask<Image> CompleteInstantiation(ImageProvider provider, Task<IMagickImageCollection<ushort>> task)
            {
                return new Image(await task.ConfigureAwait(false), provider);
            }
        }
    }
}