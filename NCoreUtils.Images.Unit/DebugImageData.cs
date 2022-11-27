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
        public sealed class DebugImageSource : IReadableResource
        {
            private readonly DebugImageData _data;

            public bool Reusable => true;

            public DebugImageSource(DebugImageData data) => _data = data ?? throw new ArgumentNullException(nameof(data));

            public IStreamProducer CreateProducer()
                => StreamProducer.Create((stream, cancellationToken) => SerializeAsync(_data, stream, cancellationToken));

            public ValueTask<ResourceInfo> GetInfoAsync(CancellationToken cancellationToken = default)
                => default;
        }

        public sealed class DebugImageDestination : IWritableResource
        {
            public DebugImageData? Data { get; private set; }

            public IStreamConsumer CreateConsumer(ResourceInfo contentInfo)
                => StreamConsumer.Create(DeserializeAsync).Bind(data => Data = data);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static ValueTask SerializeAsync(DebugImageData data, Stream destination, CancellationToken cancellationToken)
            => new(JsonSerializer.SerializeAsync(destination, data, _jsonOptions, cancellationToken));

        public static ValueTask<DebugImageData?> DeserializeAsync(Stream source, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<DebugImageData>(source, _jsonOptions, cancellationToken);

        public static DebugImageSource CreateSource(DebugImageData data)
            => new(data);

        public static DebugImageDestination CreateDestination()
            => new();

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