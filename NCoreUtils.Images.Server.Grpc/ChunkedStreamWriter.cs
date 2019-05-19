using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Server
{
    public class ChunkedStreamWriter : AsyncStreamConsumer
    {
        readonly IServerStreamWriter<Proto.Chunk> _streamWriter;

        public ChunkedStreamWriter(IServerStreamWriter<Proto.Chunk> streamWriter)
        {
            _streamWriter = streamWriter ?? throw new ArgumentNullException(nameof(streamWriter));
        }

        public override async Task ConsumeAsync(Stream source, CancellationToken cancellationToken)
        {
            // FIXME: pool
            var buffer = new byte[Defaults.ChunkSize];
            for (var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                read != 0;
                read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken))
            {
                await _streamWriter.WriteAsync(new Proto.Chunk
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                });
            }
        }
    }
}