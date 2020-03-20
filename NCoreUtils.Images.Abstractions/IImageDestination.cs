using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    public interface IImageDestination
    {
        IStreamConsumer CreateConsumer(ContentInfo contentInfo);
    }
}