using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    class RequestReader
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

        public static async Task<RequestReader> InitializeAsync(IAsyncStreamReader<RequestData> requestStream, CancellationToken cancellationToken)
        {
            var reader = new RequestReader(requestStream);
            await reader.StartAsync(cancellationToken);
            return reader;
        }

        readonly IAsyncStreamReader<RequestData> _requestStream;

        public IResizeOptions Options { get; private set; }

        RequestReader(IAsyncStreamReader<RequestData> requestStream)
        {
            _requestStream = requestStream ?? throw new ArgumentNullException(nameof(requestStream));
        }

        async Task StartAsync(CancellationToken cancellationToken)
        {
            if (await _requestStream.MoveNext(cancellationToken))
            {
                var message = _requestStream.Current;
                switch (message.DataCase)
                {
                    case RequestData.DataOneofCase.Options:
                        var o = message.Options;
                        Options = new ResizeOptions(
                            o.ImageType,
                            o.Width,
                            o.Height,
                            o.Mode,
                            o.Quality,
                            o.Optimize,
                            o.WeightX,
                            o.WeightY
                        );
                        break;
                    case RequestData.DataOneofCase.Chunk:
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, "Missing options."));
                    default:
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, "Empty message received while reading options."));
                }
            }
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "End of stream reached while reading options."));
        }

        public IStreamProducer CreateProducer()
            => StreamProducer.From(async (output, cancellationToken) =>
            {
                while (await _requestStream.MoveNext(cancellationToken))
                {
                    var message = _requestStream.Current;
                    switch (message.DataCase)
                    {
                        case RequestData.DataOneofCase.Chunk:
                            message.Chunk.WriteTo(output);
                            break;
                        case RequestData.DataOneofCase.Options:
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Unexpected options while reading data stream."));
                        default:
                            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Empty message received while reading data stream."));
                    }
                }
                await output.FlushAsync(cancellationToken);
                output.Close();
            });
    }
}