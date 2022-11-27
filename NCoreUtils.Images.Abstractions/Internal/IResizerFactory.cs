namespace NCoreUtils.Images.Internal;

public interface IResizerFactory
{
    IResizer CreateResizer(IImage image, ResizeOptions options);
}