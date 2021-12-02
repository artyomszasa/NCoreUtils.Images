using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Internal;
using NCoreUtils.Images.Logging;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class ImageResizer : IImageResizer, IImageAnalyzer
    {
        #if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        #endif
        private static (bool IsExplicit, string ImageType) DecideImageType(ResizeOptions options, IImage image)
            => options.ImageType switch
            {
                null => (false, image.ImageType),
                string itype => (true, itype)
            };

        protected IImageProvider Provider { get; }

        protected ResizerCollection Resizers { get; }

        protected IImageResizerOptions Options { get; }

        protected ILogger Logger { get; }

        public ImageResizer(
            IImageProvider provider,
            ResizerCollection resizers,
            ILogger<ImageResizer> logger,
            IImageResizerOptions? options = default)
        {
            var opts = options ?? ImageResizerOptions.Default;
            if (opts.MemoryLimit.HasValue)
            {
                provider.MemoryLimit = opts.MemoryLimit.Value;
            }
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Resizers = resizers ?? throw new ArgumentNullException(nameof(resizers));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = opts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DecideQuality(ResizeOptions options, string imageType)
            => options.Quality ?? Options.Quality(imageType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DecideOptimize(ResizeOptions options, string imageType)
            => options.Optimize ?? Options.Optimize(imageType);

        private IStreamTransformation CreateTransformation(Action<string> setContentType, ResizeOptions options)
        {
            // Logger.LogDebug("Creating transformation with options {0}", options);
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.Log(LogLevel.Debug, default, new CreatingTransformationEntry(options), default!, CreatingTransformationEntry.Formatter);
            }
            var resizeMode = options.ResizeMode ?? ResizeModes.None;
            if (Resizers.TryGetValue(resizeMode, out var resizerFactory))
            {
                return StreamTransformation.Create((input, output, cancellationToken) => TransformAsync(input, output, resizerFactory, options, setContentType, cancellationToken));
            }
            throw new UnsupportedResizeModeException(resizeMode, options.Width, options.Height, "Specified resize mode is not supported.");
        }

        protected virtual async ValueTask TransformAsync(
            Stream input,
            Stream output,
            IResizerFactory resizerFactory,
            ResizeOptions options,
            Action<string> setContentType,
            CancellationToken cancellationToken)
        {
            await using var image = await Provider.FromStreamAsync(input, cancellationToken);
            await image.NormalizeAsync(cancellationToken);
            var (isExplicit, imageType) = DecideImageType(options, image);
            var quality = DecideQuality(options, imageType);
            var optimize = DecideOptimize(options, imageType);
            // Logger.LogDebug (
            //   "Resizing image with computed options [ImageType = {0}, Quality = {1}, Optimization = {2}]",
            //   isExplicit ? imageType : $"{imageType} (implicit)",
            //   quality,
            //   optimize
            // );
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.Log(LogLevel.Debug, default, new ResizingImageEntry(imageType, isExplicit, quality, optimize), default!, ResizingImageEntry.Formatter);
            }
            setContentType(ImageTypes.ToMediaType(imageType));
            var resizer = resizerFactory.CreateResizer(image, options);
            await resizer.ResizeAsync(image, cancellationToken);
            foreach (var filter in options.Filters)
            {
                await image.ApplyFilterAsync(filter, cancellationToken);
            }
            await image.WriteToAsync(output, imageType, quality, optimize, cancellationToken);
            await output.FlushAsync(cancellationToken);
            output.Close();
        }

        public virtual ValueTask<ImageInfo> AnalyzeAsync(IImageSource source, CancellationToken cancellationToken = default)
            => source
                .CreateProducer()
                .ConsumeAsync(StreamConsumer.Create(async (input, cancellationToken) =>
                {
                    await using var image = await Provider.FromStreamAsync(input, cancellationToken);
                    return await image.GetImageInfoAsync(cancellationToken);
                }), cancellationToken);

        public virtual ValueTask ResizeAsync(IImageSource source, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default)
        {
            string? contentType = null;
            return source.CreateProducer()
                .Chain(CreateTransformation(ct => contentType = ct, options))
                .ConsumeAsync(StreamConsumer.Delay(_ =>
                {
                    var ct = contentType ?? "application/octet-stream";
                    // Logger.LogDebug("Initializing image destination with content type {0}.", ct);
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.Log(LogLevel.Debug, default, new InitializingDestinationEntry(ct), default!, InitializingDestinationEntry.Formatter);
                    }
                    return new ValueTask<IStreamConsumer>(destination.CreateConsumer(new ContentInfo(ct)));
                }), cancellationToken);
        }
    }
}