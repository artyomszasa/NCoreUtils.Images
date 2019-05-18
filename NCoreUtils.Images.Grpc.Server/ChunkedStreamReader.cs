using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Grpc
{
    public class ChunkedStreamReader : AsyncStreamProducer
    {
        readonly IAsyncStreamReader<Chunk> _streamReader;

        public ChunkedStreamReader(IAsyncStreamReader<Chunk> streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        }

        public override async Task ProduceAsync(Stream target, CancellationToken cancellationToken)
        {
            while (await _streamReader.MoveNext(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // FIXME: async
                _streamReader.Current.Data.WriteTo(target);
            }
        }
    }
}