using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Images;

/// <summary>
/// Defines various extension methods for <see cref="IImageResizer" />.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ImageResizerExtensions
{
    private sealed class StreamSource : IReadableResource, IAsyncDisposable, IDisposable
    {
        private Stream Stream { get; }

        private bool LeaveOpen { get; }

        public bool Reusable => Stream.CanSeek;

        public StreamSource(Stream stream, bool leaveOpen)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            LeaveOpen = leaveOpen;
        }

        public IStreamProducer CreateProducer()
            => StreamProducer.Create((output, cancellationToken) =>
            {
                if (Stream.CanSeek)
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                }
                return new ValueTask(Stream.CopyToAsync(output, DefaultBufferSize, cancellationToken));
            });

        public void Dispose()
        {
            if (!LeaveOpen)
            {
                Stream.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (!LeaveOpen)
            {
                return Stream.DisposeAsync();
            }
            return default;
        }

        public ValueTask<ResourceInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return new(new ResourceInfo(Stream.Length));
            }
            catch
            {
                return default;
            }
        }
    }

    private sealed class StreamDestination : IWritableResource, IAsyncDisposable, IDisposable
    {
        private Stream Stream { get; }

        private bool LeaveOpen { get; }

        public bool Reusable => false;

        public StreamDestination(Stream stream, bool leaveOpen)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            LeaveOpen = leaveOpen;
        }

        public IStreamConsumer CreateConsumer(ResourceInfo resourceInfo)
            => StreamConsumer.ToStream(Stream);

        public void Dispose()
        {
            if (!LeaveOpen)
            {
                Stream.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (!LeaveOpen)
            {
                return Stream.DisposeAsync();
            }
            return default;
        }
    }

    private sealed class ProducerSource : IReadableResource, IAsyncDisposable
    {
        private IStreamProducer Producer { get; }

        public bool Reusable => false;

        public ProducerSource(IStreamProducer producer)
            => Producer = producer ?? throw new ArgumentNullException(nameof(producer));

        public IStreamProducer CreateProducer()
            => Producer;

        public ValueTask DisposeAsync()
            => Producer.DisposeAsync();

        public ValueTask<ResourceInfo> GetInfoAsync(CancellationToken cancellationToken = default)
            => default;
    }

    private sealed class ConsumerDestination : IWritableResource, IAsyncDisposable
    {
        private IStreamConsumer Consumer { get; }

        public ConsumerDestination(IStreamConsumer consumer)
            => Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));

        public IStreamConsumer CreateConsumer(ResourceInfo resourceInfo)
            => Consumer;

        public ValueTask DisposeAsync()
            => Consumer.DisposeAsync();
    }

    private const int DefaultBufferSize = 16 * 1024;

    private static FileStream OpenRead(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous);

    private static FileStream OpenWrite(string path)
        => new(path, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous);

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

    public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, IWritableResource destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

    public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, IWritableResource destination, ResizeOptions options, CancellationToken cancellationToken = default)
    {
        using var s = new StreamSource(source, false);
        await resizer.ResizeAsync(s, destination, options, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, IWritableResource destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IReadableResource source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(source, new ConsumerDestination(destination), options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

    public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
    {
        using var s = new StreamSource(source, false);
        await resizer.ResizeAsync(s, destination, options, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, IStreamConsumer destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IReadableResource source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(source, new StreamDestination(destination, false), options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(new ProducerSource(source), destination, options, cancellationToken);

    public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
    {
        using var s = new StreamSource(source, false);
        await resizer.ResizeAsync(s, destination, options, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, Stream destination, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(OpenRead(sourcePath), destination, options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IReadableResource source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(source, OpenWrite(destinationPath), options, cancellationToken);

    public static ValueTask ResizeAsync(this IImageResizer resizer, IStreamProducer source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(new ProducerSource(source), destinationPath, options, cancellationToken);

    public static async ValueTask ResizeAsync(this IImageResizer resizer, Stream source, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
    {
        using var s = new StreamSource(source, false);
        await resizer.ResizeAsync(s, destinationPath, options, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask ResizeAsync(this IImageResizer resizer, string sourcePath, string destinationPath, ResizeOptions options, CancellationToken cancellationToken = default)
        => resizer.ResizeAsync(OpenRead(sourcePath), destinationPath, options, cancellationToken);

    public static void Resize(this IImageResizer resizer, IReadableResource source, IWritableResource destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, IStreamProducer source, IWritableResource destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, Stream source, IWritableResource destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, string sourcePath, IWritableResource destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

    public static void Resize(this IImageResizer resizer, IReadableResource source, IStreamConsumer destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, IStreamProducer source, IStreamConsumer destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, Stream source, IStreamConsumer destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, string sourcePath, IStreamConsumer destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

    public static void Resize(this IImageResizer resizer, IReadableResource source, Stream destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, IStreamProducer source, Stream destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, Stream source, Stream destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destination, options));

    public static void Resize(this IImageResizer resizer, string sourcePath, Stream destination, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(sourcePath, destination, options));

    public static void Resize(this IImageResizer resizer, IReadableResource source, string destinationPath, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

    public static void Resize(this IImageResizer resizer, IStreamProducer source, string destinationPath, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

    public static void Resize(this IImageResizer resizer, Stream source, string destinationPath, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(source, destinationPath, options));

    public static void Resize(this IImageResizer resizer, string sourcePath, string destinationPath, ResizeOptions options)
        => AwaitSync(resizer.ResizeAsync(sourcePath, destinationPath, options));
}