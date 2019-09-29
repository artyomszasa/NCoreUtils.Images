using System;
using System.Net.Http;

namespace NCoreUtils.Images.Unit
{
    public class DummyHttpClientFactory : IHttpClientFactory
    {
        readonly Func<HttpClient> _factory;

        public DummyHttpClientFactory(Func<HttpClient> factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public HttpClient CreateClient(string name) => _factory();
    }
}