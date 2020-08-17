using NCoreUtils.IO;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines image destination.
    /// </summary>
    public interface IImageDestination
    {
        /// <summary>
        /// Creates consumer for the underlying image destination.
        /// </summary>
        /// <param name="contentInfo">Content related information.</param>
        /// <returns>Consumer.</returns>
        IStreamConsumer CreateConsumer(ContentInfo contentInfo);
    }
}