using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines various extension methods for <see cref="IImageResizer" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ImageResizerExtensions
    {
        private sealed class StreamSource : IImageSource, IDisposable
        {
            readonly Stream _stream;

            public bool Reusable => _stream.CanSeek;

            public StreamSource(Stream stream)
                => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            public IStreamProducer CreateProducer()
                => StreamProducer.Create((output, cancellationToken) =>
                {
                    if (_stream.CanSeek)
                    {
                        _stream.Seek(0, SeekOrigin.Begin);
                    }
                    return new ValueTask(_stream.CopyToAsync(output, 16 * 1024, cancellationToken));
                });

            public void Dispose()
                => _stream.Dispose();
        }

        private sealed class StreamDestination : IImageDestination
        {
            readonly Stream _stream;

            public StreamDestination(Stream stream)
                => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
                => StreamConsumer.ToStream(_stream);
        }

        private sealed class ProducerSource : IImageSource
        {
            readonly IStreamProducer _producer;

            public bool Reusable => false;

            public ProducerSource(IStreamProducer producer)
                => _producer = producer ?? throw new ArgumentNullException(nameof(producer));

            public IStreamProducer CreateProducer()
                => _producer;
        }

        private sealed class ConsumerDestination : IImageDestination
        {
            readonly IStreamConsumer _consumer;

            public ConsumerDestination(IStreamConsumer consumer)
                => _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));

            public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
                => _consumer;
        }

        private static FileStream OpenRead(string path)
            => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous);

        private static FileStream OpenWrite(string path)
            => new(path, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.Asynchronous);

        private static void AwaitSync(ValueTask task)
        {
            if (task.IsCompleted)
            {
                task.GetAwaiter().GetResult();
            }
            else
            {
                task.AsTask().GetAwaiter().GetResult();
            }
        }

        public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

        public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default)
        {
            using var s = new StreamSource(source);
            await resizer.ResizeAsync(s, destination, options, cancellationToken);
        }

        public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, IImageDestination destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IImageSource source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(source, new ConsumerDestination(destination), options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

        public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
        {
            using var s = new StreamSource(source);
            await resizer.ResizeAsync(s, destination, options, cancellationToken);
        }

        public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IImageSource source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(source, new StreamDestination(destination), options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

        public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
        {
            using var s = new StreamSource(source);
            await resizer.ResizeAsync(s, destination, options, cancellationToken);
        }

        public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IImageSource source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(source, OpenWrite(destinationPath), options, cancellationToken);

        public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(new ProducerSource(source), destinationPath, options, cancellationToken);

        public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
        {
            using var s = new StreamSource(source);
            await resizer.ResizeAsync(s, destinationPath, options, cancellationToken);
        }

        public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
            => resizer.ResizeAsync(OpenRead(sourcePath), destinationPath, options, cancellationToken);

        public static void Resize(this IImageResizer resizer, IImageSource source, IImageDestination destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, IStreamProducer source, IImageDestination destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, Stream source, IImageDestination destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, string sourcePath, IImageDestination destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

        public static void Resize(this IImageResizer resizer, IImageSource source, IStreamConsumer destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, IStreamProducer source, IStreamConsumer destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, Stream source, IStreamConsumer destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, string sourcePath, IStreamConsumer destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

        public static void Resize(this IImageResizer resizer, IImageSource source, Stream destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, IStreamProducer source, Stream destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, Stream source, Stream destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destination, options));

        public static void Resize(this IImageResizer resizer, string sourcePath, Stream destination, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

        public static void Resize(this IImageResizer resizer, IImageSource source, string destinationPath, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

        public static void Resize(this IImageResizer resizer, IStreamProducer source, string destinationPath, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

        public static void Resize(this IImageResizer resizer, Stream source, string destinationPath, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

        public static void Resize(this IImageResizer resizer, string sourcePath, string destinationPath, ResizeOptions options)
            => AwaitSync(resizer.ResizeAsync(sourcePath, destinationPath, options));
    }
}