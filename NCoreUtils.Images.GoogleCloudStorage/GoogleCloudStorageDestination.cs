using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.GoogleCloudStorage;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class GoogleCloudStorageDestination : GoogleCloudStorageRecordDescriptor, IImageDestination, ISerializableImageResource
    {
        private static string Choose2(string? option1, string? option2, string fallback)
        {
            if (string.IsNullOrEmpty(option1))
            {
                return string.IsNullOrEmpty(option2) ? fallback : option2!;
            }
            return option1!;
        }

        Uri ISerializableImageResource.Uri
        {
            get
            {
                var builder = new UriBuilder(Uri);
                // TODO: find a way to avoid executing async functionality synchronously
                var accessTokenTask = Credential.GetAccessTokenAsync(GoogleStorageCredential.ReadWriteScopes, CancellationToken.None);
                var accessToken = accessTokenTask.IsCompletedSuccessfully ? accessTokenTask.Result : accessTokenTask.AsTask().GetAwaiter().GetResult();
                var isPublic = IsPublic ? "true" : string.Empty;
                // TODO: what if access token is too large ?! Right now access tokens
                Span<char> buffer = stackalloc char[16 * 1024];
                var qbuilder = new SpanBuilder(buffer);
                var first = true;
                builder.Query = qbuilder
                    .AppendQ(ref first, UriParameters.ContentType, ContentType)
                    .AppendQ(ref first, UriParameters.CacheControl, CacheControl)
                    .AppendQ(ref first, UriParameters.Public, isPublic)
                    .AppendQ(ref first, UriParameters.AccessToken, accessToken)
                    .ToString();
                return builder.Uri;
            }
        }

        public GoogleCloudStorageDestination(
            Uri uri,
            GoogleStorageCredential credential = default,
            string? contentType = default,
            string? cacheControl = default,
            bool isPublic = false,
            GoogleCloudStorageUtils? utils = default,
            ILogger<GoogleCloudStorageDestination>? logger = default)
            : base(uri, credential, contentType, cacheControl, isPublic, utils, logger)
        { }

        public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
            => StreamConsumer.Create(async (stream, cancellationToken) =>
            {
                var bucket = Uri.Host;
                var name = Uri.AbsolutePath.Trim('/');
                var contentType = Choose2(contentInfo.Type, ContentType, "application/octet-stream");
                var accessToken = await Credential
                    .GetAccessTokenAsync(GoogleStorageCredential.ReadWriteScopes, cancellationToken)
                    .ConfigureAwait(false);
                Logger.LogInformation(
                    "Initializing GCS upload to gs://{Bucket}/{Name} with [Content-Type = {ContentType}, Cache-Control = {CacheControl}, IsPublic = {IsPublic}].",
                    bucket,
                    name,
                    contentType,
                    CacheControl,
                    IsPublic
                );
                await Utils.UploadAsync(
                    bucket: bucket,
                    name: name,
                    source: stream,
                    contentType: contentType,
                    cacheControl: CacheControl,
                    isPublic: IsPublic,
                    accessToken: accessToken,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            });
    }
}