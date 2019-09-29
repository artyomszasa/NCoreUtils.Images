using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using NCoreUtils.Images.WebService;
using NCoreUtils.IO;
using Xunit;

namespace NCoreUtils.Images.Unit
{
    public class WebTest : WebTestBase<Startup>
    {
        sealed class StreamDestination : IImageDestination
        {
            readonly Stream _stream;

            public StreamDestination(Stream stream) => _stream = stream ?? throw new System.ArgumentNullException(nameof(stream));

            public FSharpAsync<Microsoft.FSharp.Core.Unit> AsyncWrite(ContentInfo contentInfo, FSharpFunc<Stream, FSharpAsync<Microsoft.FSharp.Core.Unit>> generator)
            {
                return generator.Invoke(_stream);
            }

            public FSharpAsync<Microsoft.FSharp.Core.Unit> AsyncWriteDelayed(FSharpFunc<FSharpFunc<ContentInfo, Microsoft.FSharp.Core.Unit>, FSharpFunc<Stream, FSharpAsync<Microsoft.FSharp.Core.Unit>>> generator)
            {
                FSharpFunc<ContentInfo, Microsoft.FSharp.Core.Unit> set = FSharpFunc<ContentInfo, Microsoft.FSharp.Core.Unit>.FromConverter(_ => default);
                return generator.Invoke(set).Invoke(_stream);
            }
        }


        public WebTest(WebApplicationFactory<Startup> factory) : base(factory) { }

        ImageResizerClient CreateResizerClient()
        {
            return new ImageResizerClient(
                new ImageResizerClientConfiguration
                {
                    EndPoints = new [] { "http://localhost/xxx", "http://localhost" }
                },
                new DummyLog<ImageResizerClient>(),
                new DummyHttpClientFactory(this.CreateClient)
            );
        }

        protected override void InitializeServices(IServiceCollection services)
        {
            services
                .RemoveAll(typeof(IImageOptimization))
                .RemoveAll(typeof(IImageResizer))
                .AddSingleton<IImageResizer, DummyResizer>();
        }

        [Fact]
        public async Task SuccessfullResize()
        {
            var binary = Resources.Png.X;
            using var producer = new ReusableStreamProducer(new MemoryStream(binary, false));
            var client = CreateResizerClient();

            var info = await client.GetImageInfoAsync(producer.Reuse(), CancellationToken.None);
            Assert.Equal (100, info.Width);

            using (var buffer = new MemoryStream())
            {
                await client.ResizeAsync(producer.Reuse(), new StreamDestination(buffer), new ResizeOptions(imageType: "png"), CancellationToken.None);
                Assert.True (Enumerable.SequenceEqual(binary, buffer.ToArray ()));
            }
        }
    }
}