namespace NCoreUtils.Images.Internal
{
    public class NoneResizerFactory : IResizerFactory
    {
        sealed class NoneResizer : IResizer
        {
            public static NoneResizer Instance { get; } = new NoneResizer();

            NoneResizer() { }

            public void Resize(IImage image) { }
        }


        public IResizer CreateResizer(IImage image, ResizeOptions options) => NoneResizer.Instance;
    }
}