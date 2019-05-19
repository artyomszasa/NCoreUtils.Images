using System;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Unit
{
    public class ArrayImageSource : IImageSource
    {
        readonly byte[] _data;

        public ArrayImageSource(byte[] data)
        {
            _data = data;
        }

        public IStreamProducer CreateProducer() => StreamProducer.From(async (output, cancellationToken) =>
        {
            await output.WriteAsync(_data, 0, _data.Length, cancellationToken);
            await output.FlushAsync(cancellationToken);
            output.Close();
        });
    }
}