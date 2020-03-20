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
        static string CreateDownloadEndpoint(string bucketName, string urlEncodedName)
            => $"https://www.googleapis.com/storage/v1/b/{bucketName}/o/{urlEncodedName}?alt=media";

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
            IHttpClientFactory? httpClientFactory = default,
            ILogger<GoogleCloudStorageSource>? logger = default)
            : base(uri, credential, default, default, default, httpClientFactory, logger)
        { }

        public IStreamProducer CreateProducer()
            => StreamProducer.Create(async (output, cancellationToken) =>
            {
                using var client = CreateClient();
                var bucketName = Uri.Host;
                var urlEncodedName = Uri.EscapeDataString(Uri.LocalPath.Trim('/'));
                var requestUri = CreateDownloadEndpoint(bucketName, urlEncodedName);
                var token = await Credential.GetAccessTokenAsync(GoogleStorageCredential.ReadOnlyScopes, cancellationToken).ConfigureAwait(false);
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        await response.Content.CopyToAsync(output).ConfigureAwait(false);
                        break;
                    default:
                        throw new GoogleCloudStorageAccessException($"Objects.get failed, status code = {response.StatusCode}, uri = {requestUri}.");
                }
            });
    }
}