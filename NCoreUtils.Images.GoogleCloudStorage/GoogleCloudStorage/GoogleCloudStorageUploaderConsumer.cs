using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    internal sealed class GoogleCloudStorageUploaderConsumer : IStreamConsumer
    {
        readonly HttpClient _client;

        readonly Uri _endpoint;

        readonly string? _contentType;

        GoogleCloudStorageUploader? _uploader;

        public GoogleCloudStorageUploaderConsumer(HttpClient client, Uri endpoint, string? contentType)
        {
            _client = client;
            _endpoint = endpoint;
            _contentType = contentType;
        }

        public ValueTask ConsumeAsync(Stream input, CancellationToken cancellationToken = default)
        {
            _uploader = new GoogleCloudStorageUploader(_client, _endpoint, _contentType);
            return new ValueTask(_uploader.ConumeStreamAsync(input, cancellationToken));
        }

        public ValueTask DisposeAsync()
        {
            _uploader?.Dispose();
            _client.Dispose();
            return default;
        }
    }
}