using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    public class ChunkedStreamWriter : AsyncStreamConsumer
    {
        readonly IServerStreamWriter<Chunk> _streamWriter;

        public ChunkedStreamWriter(IServerStreamWriter<Chunk> streamWriter)
        {
            _streamWriter = streamWriter ?? throw new ArgumentNullException(nameof(streamWriter));
        }

        public override async Task ConsumeAsync(Stream source, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            for (var read = await source.ReadAsync(buffer, 0, 8192, cancellationToken);
                read != 0;
                read = await source.ReadAsync(buffer, 0, 8192, cancellationToken))
            {
                await _streamWriter.WriteAsync(new Chunk
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                });
            }
        }
    }
}