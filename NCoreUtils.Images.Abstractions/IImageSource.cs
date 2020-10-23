using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines functionality to access input image.
    /// </summary>
    public interface IImageSource
    {
        /// <summary>
        /// Whether producer can be created multiple times.
        /// </summary>
        bool Reusable { get; }

        /// <summary>
        /// Initializes new instance of stream producer.
        /// </summary>
        IStreamProducer CreateProducer();
    }
}