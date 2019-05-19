using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Server
{
    public class ChunkedStreamReader : AsyncStreamProducer
    {
        readonly IAsyncStreamReader<Proto.Chunk> _streamReader;

        public ChunkedStreamReader(IAsyncStreamReader<Proto.Chunk> streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        }

        public override async Task ProduceAsync(Stream target, CancellationToken cancellationToken)
        {
            while (await _streamReader.MoveNext(cancellationToken))
            {
                // FIXME: async
                _streamReader.Current.Data.WriteTo(target);
            }
            await target.FlushAsync(cancellationToken);
            target.Close();
        }
    }
}