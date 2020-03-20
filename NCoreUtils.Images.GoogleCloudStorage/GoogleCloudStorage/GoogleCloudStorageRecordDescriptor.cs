using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    public class GoogleCloudStorageRecordDescriptor
    {
        public const string HttpClient = "NCoreUtils.Images.GoogleCloudStorage";

        public Uri Uri { get; }

        public GoogleStorageCredential Credential { get; }

        public string? ContentType { get; }

        public string? CacheControl { get; }

        public bool IsPublic { get; }

        public IHttpClientFactory? HttpClientFactory { get; }

        public ILogger Logger { get; }

        public GoogleCloudStorageRecordDescriptor(
            Uri uri,
            GoogleStorageCredential credential = default,
            string? contentType = default,
            string? cacheControl = default,
            bool isPublic = false,
            IHttpClientFactory? httpClientFactory = default,
            ILogger<GoogleCloudStorageRecordDescriptor>? logger = default)
        {
            Uri = uri;
            Credential = credential;
            ContentType = contentType;
            CacheControl = cacheControl;
            IsPublic = isPublic;
            HttpClientFactory = httpClientFactory;
            Logger = logger ?? SuppressLogger<GoogleCloudStorageRecordDescriptor>.Instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected HttpClient CreateClient()
            => HttpClientFactory?.CreateClient() ?? new HttpClient();
    }
}