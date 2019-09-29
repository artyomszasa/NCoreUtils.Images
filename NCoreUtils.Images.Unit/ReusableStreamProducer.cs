using System;
using System.IO;
using System.Threading;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Unit
{
    public class ReusableStreamProducer : IStreamProducer
    {
        sealed class ReusableProducer : IStreamProducer
        {
            readonly ReusableStreamProducer _reusableStreamProducer;

            public ReusableProducer(ReusableStreamProducer reusableStreamProducer)
            {
                _reusableStreamProducer = reusableStreamProducer;
            }

            public FSharpAsync<Microsoft.FSharp.Core.Unit> AsyncProduce(Stream output)
                => _reusableStreamProducer.AsyncProduce(output);

            public void Dispose() { }
        }

        readonly Stream _stream;
        int _used = 0;
        int _isDisposed = 0;

        public ReusableStreamProducer(Stream stream) => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        public FSharpAsync<Microsoft.FSharp.Core.Unit> AsyncProduce(Stream output)
        {
            if (0 != Interlocked.CompareExchange(ref _used, 1, 0))
            {
                if (!_stream.CanSeek)
                {
                    throw new InvalidOperationException("Cannot reuse a non-seekable stream.");
                }
                _stream.Seek(0, SeekOrigin.Begin);
            }
            return NCoreUtils.StreamModule.AsyncCopyTo(output, _stream);
        }

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                _stream.Dispose();
            }
        }

        public IStreamProducer Reuse() => new ReusableProducer(this);
    }
}