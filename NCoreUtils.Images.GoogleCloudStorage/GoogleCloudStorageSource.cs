using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.GoogleCloudStorage;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class GoogleCloudStorageSource : GoogleCloudStorageRecordDescriptor, IImageSource, ISerializableImageResource
    {
        Uri ISerializableImageResource.Uri
        {
            get
            {
                var builder = new UriBuilder(Uri);
                // TODO: find a way to avoid executing async functionality synchronously
                var accessTokenTask = Credential.GetAccessTokenAsync(GoogleStorageCredential.ReadWriteScopes, CancellationToken.None);
                var accessToken = accessTokenTask.IsCompletedSuccessfully ? accessTokenTask.Result : accessTokenTask.AsTask().GetAwaiter().GetResult();
                builder.Query = $"?{UriParameters.AccessToken}={accessToken}";
                return builder.Uri;
            }
        }

        public bool Reusable => true;

        public GoogleCloudStorageSource(
            Uri uri,
            GoogleStorageCredential credential = default,
            GoogleCloudStorageUtils? utils = default,
            ILogger<GoogleCloudStorageSource>? logger = default)
            : base(uri, credential, default, default, default, utils, logger)
        { }

        public IStreamProducer CreateProducer()
            => StreamProducer.Create(async (output, cancellationToken) =>
            {
                var bucket = Uri.Host;
                var name = Uri.AbsolutePath.Trim('/');
                var accessToken = await Credential
                    .GetAccessTokenAsync(GoogleStorageCredential.ReadWriteScopes, cancellationToken)
                    .ConfigureAwait(false);
                Logger.LogInformation(
                    "Downloading GCS object from gs://{Bucket}/{Name}.",
                    bucket,
                    name
                );
                await Utils.DownloadAsync(bucket, name, output, accessToken, cancellationToken).ConfigureAwait(false);
            });
    }
}