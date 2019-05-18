using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    public class ImageServiceImpl
    {
        sealed class ResizeOptions : IResizeOptions
        {
            public string ImageType { get; }

            public int? Width { get; }

            public int? Height { get; }

            public string ResizeMode { get; }

            public int? Quality { get; }

            public bool? Optimize { get; }

            public int? WeightX { get; }

            public int? WeightY { get; }

            public ResizeOptions(string imageType, int? width, int? heigth, string resizeMode, int? quality, bool? optimize, int? weightX, int? weightY)
            {
                ImageType = imageType;
                Width = width;
                Height = heigth;
                ResizeMode = resizeMode;
                Quality = quality;
                Optimize = optimize;
                WeightX = weightX;
                WeightY = weightY;
            }
        }



        readonly IImageResizer _imageResizer;

        // readonly

        ResizeOptions ReadResizeOptions(ServerCallContext context)
        {
            string imageType = null;
            int? width = null;
            int? height = null;
            string resizeMode = "none";
            int? quality = null;
            bool? optimize = null;
            int? weightX = null;
            int? weightY = null;
            foreach (var entry in context.RequestHeaders)
            {
                if ("o-image-type" == entry.Key)
                {
                    imageType = entry.GetStringValue();
                }
                if ("o-width" == entry.Key)
                {
                    width = entry.GetInt32Value();
                }
                if ("o-height" == entry.Key)
                {
                    height = entry.GetInt32Value();
                }
                if ("o-resize-mode" == entry.Key)
                {
                    resizeMode = entry.GetStringValue();
                }
                if ("o-quality" == entry.Key)
                {
                    quality = entry.GetInt32Value();
                }
                if ("o-optimize" == entry.Key)
                {
                    optimize = entry.GetBooleanValue();
                }
                if ("o-weight-x" == entry.Key)
                {
                    weightX = entry.GetInt32Value();
                }
                if ("o-weight-y" == entry.Key)
                {
                    weightY = entry.GetInt32Value();
                }
            }
            return new ResizeOptions(imageType, width, height, resizeMode, quality, optimize, weightX, weightY);
        }

        async Task DoResizeAsync(IStreamProducer source, IStreamConsumer target, Action<string> updateImageType, IResizeOptions options, CancellationToken cancellationToken)
        {
            using (var outputProducer =
                _imageResizer.CreateTransformation(options)
                    .Chain((string imageType, out IStreamTransformation next) =>
                    {
                        updateImageType(imageType);
                        next = default(IStreamTransformation);
                        return false;
                    })
                    .Chain(source))
            {
                await target.ConsumeAsync(outputProducer, cancellationToken);
            }
        }

        public Task ResizeTransform(IAsyncStreamReader<Chunk> requestStream, IServerStreamWriter<Chunk> responseStream, ServerCallContext context)
            => DoResizeAsync(
                source: new ChunkedStreamReader(requestStream),
                target: new ChunkedStreamWriter(responseStream),
                updateImageType: imageType => context.WriteResponseHeadersAsync(new Metadata { { "image-type", imageType } }).Wait(),
                options: ReadResizeOptions(context),
                context.CancellationToken
            );

        public override async Task ResizeAsync(IAsyncStreamReader<Chunk> requestStream, IServerStreamWriter<Chunk> responseStream, ServerCallContext context)
        {
            var reader = await RequestReader.InitializeAsync(requestStream, context.CancellationToken);
            var serviceProvider = (IServiceProvider)context.UserState[DependencyInjectorInterceptor.ServiceProviderKey];
            var imageResizer = serviceProvider.GetRequiredService<IImageResizer>();

            var consumer = new ResponseWriter(responseStream);

            var resizeTransformation = imageResizer.CreateTransformation(reader.Options).Chain((string imageType, out IStreamTransformation next) =>
            {
                consumer.SendImageType(imageType).Wait();
                next = null;
                return false;
            });

            using (var resultProducer = resizeTransformation.Chain(reader.CreateProducer()))
            {
                await consumer.ConsumeAsync(resultProducer, context.CancellationToken);
            }
        }
    }
}