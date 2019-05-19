using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.Images.Proto;

namespace NCoreUtils.Images.Server
{
    public class ImageResizerService
    {
        static ResizeOptions ReadResizeOptions(ServerCallContext context)
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

        static Action<string> CreateImageTypeApplier(ServerCallContext context)
        {
            return imageType => context.WriteResponseHeadersAsync(new Metadata { { "image-type", imageType } }).Wait();
        }

        static IReadOnlyDictionary<string, string> CollectRequestStringMetadata(ServerCallContext context)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var entry in context.RequestHeaders)
            {
                if (entry.Key.StartsWith("target-") || entry.Key.StartsWith("source-"))
                {
                    builder.Add(entry.Key, entry.GetStringValue());
                }
            }
            return builder;
        }

        readonly ServerUtils _serverUtils;

        public ImageResizerService(ServerUtils serverUtils)
        {
            _serverUtils = serverUtils ?? throw new ArgumentNullException(nameof(serverUtils));
        }

        public async Task TransformAsync(
            IAsyncStreamReader<Proto.Chunk> requestStream,
            IServerStreamWriter<Proto.Chunk> responseStream,
            ServerCallContext context,
            CancellationToken cancellationToken)
        {
            using (var producer = new ChunkedStreamReader(requestStream))
            using (var consumer = new ChunkedStreamWriter(responseStream))
            {
                await _serverUtils.ResizeAsync(
                    producer,
                    consumer,
                    ReadResizeOptions(context),
                    CreateImageTypeApplier(context),
                    cancellationToken
                );
            }
        }

        public async Task ProduceAsync(
            IServerStreamWriter<Proto.Chunk> responseStream,
            ServerCallContext context,
            CancellationToken cancellationToken)
        {
            var meta = CollectRequestStringMetadata(context);
            var source = _serverUtils.TryExtractSource(meta);
            if (source.IsNone)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Source metadata is either invalid or unsupported."));
            }
            using (var producer = source.Value.CreateProducer())
            using (var consumer = new ChunkedStreamWriter(responseStream))
            {
                await _serverUtils.ResizeAsync(
                    producer,
                    consumer,
                    ReadResizeOptions(context),
                    CreateImageTypeApplier(context),
                    cancellationToken
                );
            }
        }

        public async Task ConsumeAsync(
            IAsyncStreamReader<Proto.Chunk> requestStream,
            ServerCallContext context,
            CancellationToken cancellationToken)
        {
            var meta = CollectRequestStringMetadata(context);
            var destination = _serverUtils.TryExtractDestination(meta);
            if (destination.IsNone)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Destination metadata is either invalid or unsupported."));
            }
            using (var producer = new ChunkedStreamReader(requestStream))
            using (var consumer = destination.Value.CreateConsumer())
            {
                await _serverUtils.ResizeAsync(
                    producer,
                    consumer,
                    ReadResizeOptions(context),
                    CreateImageTypeApplier(context),
                    cancellationToken
                );
            }
        }

        public async Task PerformAsync(
            ServerCallContext context,
            CancellationToken cancellationToken)
        {
            var meta = CollectRequestStringMetadata(context);
            var destination = _serverUtils.TryExtractDestination(meta);
            if (destination.IsNone)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Destination metadata is either invalid or unsupported."));
            }
            var source = _serverUtils.TryExtractSource(meta);
            if (source.IsNone)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Source metadata is either invalid or unsupported."));
            }
            using (var producer = source.Value.CreateProducer())
            using (var consumer = destination.Value.CreateConsumer())
            {
                await _serverUtils.ResizeAsync(
                    producer,
                    consumer,
                    ReadResizeOptions(context),
                    CreateImageTypeApplier(context),
                    cancellationToken
                );
            }
        }
    }
}