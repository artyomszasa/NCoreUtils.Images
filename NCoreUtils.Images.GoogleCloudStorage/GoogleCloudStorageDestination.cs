using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.GoogleCloudStorage;
using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public class GoogleCloudStorageDestination : GoogleCloudStorageRecordDescriptor, IImageDestination, ISerializableImageResource
    {
        static readonly MediaTypeHeaderValue _jsonContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        static string Choose2(string? option1, string? option2, string fallback)
        {
            if (string.IsNullOrEmpty(option1))
            {
                return string.IsNullOrEmpty(option2) ? fallback : option2!;
            }
            return option1!;
        }

        static string CreateInitEndpoint(string bucketName, string acl)
            => $"https://www.googleapis.com/upload/storage/v1/b/{bucketName}/o?uploadType=resumable&predefinedAcl={acl}";

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
            IHttpClientFactory? httpClientFactory = default,
            ILogger<GoogleCloudStorageDestination>? logger = default)
            : base(uri, credential, contentType, cacheControl, isPublic, httpClientFactory, logger)
        { }

        async Task<IStreamConsumer> CreateUploadConsumerAsync(Uri uri, string? overrideContentType, CancellationToken cancellationToken)
        {
            var client = CreateClient();
            try
            {

                var contentType = Choose2(overrideContentType, ContentType, "application/octet-stream");
                var bucketName = uri.Host;
                var name = uri.AbsolutePath.Trim('/');
                var predefinedAcl = IsPublic ? PredefinedAcls.Public : PredefinedAcls.Private;
                var initEndpoint = CreateInitEndpoint(bucketName, predefinedAcl);
                var token = await Credential.GetAccessTokenAsync(GoogleStorageCredential.ReadWriteScopes, cancellationToken).ConfigureAwait(false);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var gobj = new GoogleObjectData
                {
                    ContentType = contentType,
                    CacheControl = CacheControl,
                    Name = name
                };
                Logger.LogInformation(
                    "Initializing GCS upload to gs://{0}/{1} with [Content-Type = {2}, CacheControl = {3}, PredefinedAcl = {4}].",
                    bucketName,
                    name,
                    contentType,
                    CacheControl,
                    predefinedAcl
                );
                var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(gobj));
                content.Headers.ContentType = _jsonContentType;
                using var request = new HttpRequestMessage(HttpMethod.Post, initEndpoint) { Content = content };
                request.Headers.Add("X-Upload-Content-Type", contentType);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var uploadEndpoint = response.Headers.Location ?? throw new GoogleCloudStorageUploadException("Resumable upload response contans no location.");
                return new GoogleCloudStorageUploaderConsumer(client, uploadEndpoint, contentType);
            }
            catch (Exception)
            {
                // if no GoogleCloudStorageUploaderConsumer is created client should be disposed
                client.Dispose();
                throw;
            }
        }

        public IStreamConsumer CreateConsumer(ContentInfo contentInfo)
            => StreamConsumer.Delay((cancellationToken) => new ValueTask<IStreamConsumer>(CreateUploadConsumerAsync(Uri, contentInfo.Type, cancellationToken)));
    }
}