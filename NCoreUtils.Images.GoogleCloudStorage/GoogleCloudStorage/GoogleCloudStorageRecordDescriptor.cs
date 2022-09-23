using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    public class GoogleCloudStorageRecordDescriptor
    {
        private sealed class DummyHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
                => new();
        }

        public const string HttpClient = "NCoreUtils.Images.GoogleCloudStorage";

        private static GoogleCloudStorageUtils? _defaultUtils = default;

        public static GoogleCloudStorageUtils DefaultUtils
        {
            get
            {
                _defaultUtils ??= new GoogleCloudStorageUtils(new DummyHttpClientFactory());
                return _defaultUtils;
            }
        }


        public Uri Uri { get; }

        public GoogleStorageCredential Credential { get; }

        public string? ContentType { get; }

        public string? CacheControl { get; }

        public bool IsPublic { get; }

        public GoogleCloudStorageUtils Utils { get; }

        public ILogger Logger { get; }

        public GoogleCloudStorageRecordDescriptor(
            Uri uri,
            GoogleStorageCredential credential = default,
            string? contentType = default,
            string? cacheControl = default,
            bool isPublic = false,
            GoogleCloudStorageUtils? utils = default,
            ILogger<GoogleCloudStorageRecordDescriptor>? logger = default)
        {
            Uri = uri;
            Credential = credential;
            ContentType = contentType;
            CacheControl = cacheControl;
            IsPublic = isPublic;
            Utils = utils ?? DefaultUtils;
            Logger = logger ?? SuppressLogger<GoogleCloudStorageRecordDescriptor>.Instance;
        }
    }
}