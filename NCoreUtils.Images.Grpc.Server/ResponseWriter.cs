using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    class ResponseWriter : AsyncStreamConsumer
    {
        readonly IServerStreamWriter<ResponseData> _responseStream;

        public ResponseWriter(IServerStreamWriter<ResponseData> responseStream)
        {
            _responseStream = responseStream ?? throw new ArgumentNullException(nameof(responseStream));
        }

        public Task SendImageType(string imageType)
            => _responseStream.WriteAsync(new ResponseData
            {
                ImageType = imageType
            });

        public override async Task ConsumeAsync(Stream source, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            for (var read = await source.ReadAsync(buffer, 0, 8192, cancellationToken);
                read != 0;
                read = await source.ReadAsync(buffer, 0, 8192, cancellationToken))
            {
                await _responseStream.WriteAsync(new ResponseData
                {
                    Chunk = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                });
            }
        }
    }
}