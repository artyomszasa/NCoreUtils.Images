using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Images.Internal
{
    public class NoneResizerFactory : IResizerFactory
    {
        private sealed class NoneResizer : IResizer
        {
            public static NoneResizer Instance { get; } = new NoneResizer();

            NoneResizer() { }

            public ValueTask ResizeAsync(IImage image, CancellationToken cancellationToken = default)
                => default;
        }


        public IResizer CreateResizer(IImage image, ResizeOptions options) => NoneResizer.Instance;
    }
}