using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class AzureBlobStorageDestination : IImageDestination, ISerializableImageResource
    {
        protected string ContainerName { get; }

        protected string BlobName { get; }

        public Uri Uri => new UriBuilder
        {
            Scheme = "az",
            Host = ContainerName,
            Path = BlobName
        }.Uri;

        public AzureBlobStorageDestination(string containerName, string blobName)
        {
            ContainerName = containerName;
            BlobName = blobName;
        }

        public AzureBlobStorageDestination(Uri uri)
            : this(uri.Host, uri.AbsolutePath.Trim('/'))
        {
            if (uri.Scheme != "az")
            {
                throw new ArgumentException($"Invalid scheme: \"az\" expected, \"{uri.Scheme}\" found.", nameof(uri));
            }
        }

        public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
            => StreamConsumer.Create(async (stream, cancellationToken) =>
            {
                var client = new BlobClient(
                    connectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"),
                    blobContainerName: ContainerName,
                    blobName: BlobName
                );
                var options = new BlobUploadOptions();
                options.HttpHeaders ??= new BlobHttpHeaders();
                options.HttpHeaders.ContentType = contentInfo.Type ?? "application/octet-stream";
                await client.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
            });
    }
}