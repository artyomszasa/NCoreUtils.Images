using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public interface IImageSource
    {
        bool Reusable { get; }

        IStreamProducer CreateProducer();
    }
}