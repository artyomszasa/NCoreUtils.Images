using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class AzureBlobStorageSource : IImageSource, ISerializableImageResource
    {
        protected string ContainerName { get; }

        protected string BlobName { get; }

        public Uri Uri => new UriBuilder
        {
            Scheme = "az",
            Host = ContainerName,
            Path = BlobName
        }.Uri;

        public bool Reusable => throw new NotImplementedException();

        public AzureBlobStorageSource(string containerName, string blobName)
        {
            ContainerName = containerName;
            BlobName = blobName;
        }

        public AzureBlobStorageSource(Uri uri)
            : this(uri.Host, uri.AbsolutePath.Trim('/'))
        {
            if (uri.Scheme != "az")
            {
                throw new ArgumentException($"Invalid scheme: \"az\" expected, \"{uri.Scheme}\" found.", nameof(uri));
            }
        }

        public IStreamProducer CreateProducer()
            => StreamProducer.Create(async (stream, cancellationToken) =>
            {
                var client = new BlobClient(
                    connectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"),
                    blobContainerName: ContainerName,
                    blobName: BlobName
                );
                #if NETSTANDARD2_1
                await using var source
                #else
                using var source
                #endif
                    = await client.OpenReadAsync(new BlobOpenReadOptions(false), cancellationToken);
                await source.CopyToAsync(stream, 32 * 1024, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            });
    }
}