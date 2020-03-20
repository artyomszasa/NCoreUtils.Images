using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    internal sealed class GoogleCloudStorageUploader
    {
        readonly UnmanagedMemoryManager<byte> _buffer = new UnmanagedMemoryManager<byte>(512 * 1024);

        long _sent;

        long _isDisposed;

        public HttpClient Client { get; }

        public Uri EndPoint { get; }

        public MediaTypeHeaderValue ContentType { get; }

        public GoogleCloudStorageUploader(HttpClient client, Uri endpoint, string? contentType)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            EndPoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            ContentType = MediaTypeHeaderValue.Parse(contentType ?? "application/octet-stream");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(GoogleCloudStorageUploader));
            }
        }

        async Task<HttpResponseMessage> SendChunkAsync(int size, bool final, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetSize = _sent + size;
            var content = new ReadOnlyMemoryContent(size == _buffer.Size ? _buffer.Memory : _buffer.Memory.Slice(0, size));
            var headers = content.Headers;
            headers.ContentLength = size;
            headers.ContentType = ContentType;
            headers.ContentRange = !final ? new ContentRangeHeaderValue(_sent, targetSize - 1L) : new ContentRangeHeaderValue(_sent, targetSize - 1L, targetSize);
            using var request = new HttpRequestMessage(HttpMethod.Put, EndPoint) { Content = content };
            return await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        }

        async ValueTask<int> FillBuffer(Stream stream, CancellationToken cancellationToken)
        {
            var totalRead = 0;
            int read = 0;
            do
            {
                read = await stream.ReadAsync(_buffer.Memory.Slice(totalRead, _buffer.Size - totalRead), cancellationToken).ConfigureAwait(false);
                totalRead += read;
            }
            while (read > 0 && totalRead < _buffer.Size);
            return totalRead;
        }

        public async Task ConumeStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            var read = await FillBuffer(stream, cancellationToken).ConfigureAwait(false);
            if (read == _buffer.Size)
            {
                // partial upload
                var response = await SendChunkAsync(read, false, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode == 308)
                {
                    // chunk upload successfull (TODO: check if response range is set properly)
                    _sent += read;
                    await ConumeStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new GoogleCloudStorageUploadException($"Upload chunk failed with status code {response.StatusCode}.");
                }
            }
            else
            {
                // FIXME: handle stream being divisable by chunk size
                var response = await SendChunkAsync(read, true, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                {
                    throw new GoogleCloudStorageUploadException($"Upload final chunk failed with status code {response.StatusCode}.");
                }
            }
        }

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                ((IDisposable)_buffer).Dispose();
            }
        }
    }
}