using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class DebugImageData
    {
        public sealed class DebugImageSource : IImageSource
        {
            private readonly DebugImageData _data;

            public bool Reusable => true;

            public DebugImageSource(DebugImageData data) => _data = data ?? throw new ArgumentNullException(nameof(data));

            public IStreamProducer CreateProducer()
                => StreamProducer.Create((stream, cancellationToken) => SerializeAsync(_data, stream, cancellationToken));
        }

        public sealed class DebugImageDestination : IImageDestination
        {
            public DebugImageData Data { get; private set; }

            public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
                => StreamConsumer.Create(DeserializeAsync).Bind(data => Data = data);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static ValueTask SerializeAsync(DebugImageData data, Stream destination, CancellationToken cancellationToken)
            => new ValueTask(JsonSerializer.SerializeAsync(destination, data, _jsonOptions, cancellationToken));

        public static ValueTask<DebugImageData> DeserializeAsync(Stream source, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<DebugImageData>(source, _jsonOptions, cancellationToken);

        public static DebugImageSource CreateSource(DebugImageData data)
            => new DebugImageSource(data);

        public static DebugImageDestination CreateDestination()
            => new DebugImageDestination();

        public int Width { get; set; }

        public int Height { get; set; }

        public string ImageType { get; set; } = string.Empty;

        public DebugImageData() { }

        public DebugImageData(int width, int height, string imageType)
        {
            Width = width;
            Height = height;
            ImageType = imageType;
        }
    }
}