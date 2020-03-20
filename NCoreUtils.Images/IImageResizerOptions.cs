namespace NCoreUtils.Images
{
    public interface IImageResizerOptions
    {
        long? MemoryLimit { get; }

        int Quality(string imageType);

        bool Optimize(string imageType);
    }
}