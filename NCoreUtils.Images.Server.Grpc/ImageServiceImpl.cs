using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.Images.Proto;

namespace NCoreUtils.Images.Server
{
    public class ImageServiceImpl : ImageService.ImageServiceBase
    {
        public override Task ResizeTransform(IAsyncStreamReader<Chunk> requestStream, IServerStreamWriter<Chunk> responseStream, ServerCallContext context)
        {
            return context.GetRequestServices()
                .Activate<ImageResizerService>()
                .TransformAsync(requestStream, responseStream, context, context.CancellationToken);
        }

        public override async Task<Empty> ResizeConsume(IAsyncStreamReader<Chunk> requestStream, ServerCallContext context)
        {
            await context.GetRequestServices()
                .Activate<ImageResizerService>()
                .ConsumeAsync(requestStream, context, context.CancellationToken);
            return new Empty();
        }

        public override Task ResizeProduce(Empty request, IServerStreamWriter<Chunk> responseStream, ServerCallContext context)
        {
            return context.GetRequestServices()
                .Activate<ImageResizerService>()
                .ProduceAsync(responseStream, context, context.CancellationToken);
        }

        public override async Task<Empty> ResizeOperation(Empty request, ServerCallContext context)
        {
            await context.GetRequestServices()
                .Activate<ImageResizerService>()
                .PerformAsync(context, context.CancellationToken);
            return new Empty();
        }
    }
}