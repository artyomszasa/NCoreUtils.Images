using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    public class ImageServiceWrapper : ImageService.ImageServiceBase
    {
        public override async Task ResizeAsync(IAsyncStreamReader<RequestData> requestStream, IServerStreamWriter<ResponseData> responseStream, ServerCallContext context)
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