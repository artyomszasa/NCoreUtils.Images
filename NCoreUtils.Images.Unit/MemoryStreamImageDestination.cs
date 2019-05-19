using System.IO;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Unit
{
    public class MemoryStreamImageDestination : IImageDestination
    {
        readonly MemoryStream _stream;

        public MemoryStreamImageDestination(MemoryStream stream)
        {
            _stream = stream;
        }

        public IStreamConsumer CreateConsumer() => StreamConsumer.From((input, cancellationToken) => input.CopyToAsync(_stream, Defaults.ChunkSize, cancellationToken));
    }
}