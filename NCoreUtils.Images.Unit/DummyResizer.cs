using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.IO;

namespace NCoreUtils.Images.Unit
{
    public class DummyResizer : AsyncImageResizer
    {
        public override IDependentStreamTransformation<string> CreateTransformation(IResizeOptions options)
        {
            return DependentStreamTransformation.From<string>(async (input, dependentOutput, cancellationToken) =>
            {
                var output = dependentOutput("jpg");
                await input.CopyToAsync(output, Defaults.ChunkSize, cancellationToken);
                output.Close();
            });
        }

        public override Task<ImageInfo> GetInfoAsync(IImageSource source, CancellationToken cancellationToken)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception exn)
            {
                return Task.FromException<ImageInfo>(exn);
            }
        }
    }
}