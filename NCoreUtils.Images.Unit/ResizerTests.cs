using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NCoreUtils.Images
{
    public class ResizerTests : TestBase
    {
        [Fact]
        public async Task Noop()
        {
            var resizer = _serviceProvider.GetRequiredService<IImageResizer>();
            var source = DebugImageData.CreateSource(new DebugImageData(400, 400, ImageTypes.Jpeg));
            var destination = DebugImageData.CreateDestination();
            await resizer.ResizeAsync(source, destination, new ResizeOptions());
            Assert.NotNull(destination.Data);
            Assert.Equal(400, destination.Data.Width);
            Assert.Equal(400, destination.Data.Height);
            Assert.Equal(ImageTypes.Jpeg, destination.Data.ImageType);
            await resizer.ResizeAsync(source, destination, new ResizeOptions(imageType: ImageTypes.Png));
            Assert.NotNull(destination.Data);
            Assert.Equal(400, destination.Data.Width);
            Assert.Equal(400, destination.Data.Height);
            Assert.Equal(ImageTypes.Png, destination.Data.ImageType);
        }

        [Theory]
        [InlineData(200, 200, 300, 300)]
        [InlineData(1024, 800, 333, 222)]
        public async Task Inbox(int sourceWidth, int sourceHeight, int desiredWidth, int desiredHeight)
        {
            var resizer = _serviceProvider.GetRequiredService<IImageResizer>();
            var source = DebugImageData.CreateSource(new DebugImageData(sourceWidth, sourceHeight, ImageTypes.Jpeg));
            var destination = DebugImageData.CreateDestination();
            await resizer.ResizeAsync(source, destination, new ResizeOptions(resizeMode: "inbox", width: desiredWidth, height: desiredHeight));
            Assert.NotNull(destination.Data);
            Assert.Equal(desiredWidth, destination.Data.Width);
            Assert.Equal(desiredHeight, destination.Data.Height);
            Assert.Equal(ImageTypes.Jpeg, destination.Data.ImageType);
        }

        [Theory]
        [InlineData(200, 200, 300, 300)]
        [InlineData(1024, 800, 333, 222)]
        public async Task ExactBoth(int sourceWidth, int sourceHeight, int desiredWidth, int desiredHeight)
        {
            var resizer = _serviceProvider.GetRequiredService<IImageResizer>();
            var source = DebugImageData.CreateSource(new DebugImageData(sourceWidth, sourceHeight, ImageTypes.Jpeg));
            var destination = DebugImageData.CreateDestination();
            await resizer.ResizeAsync(source, destination, new ResizeOptions(resizeMode: "exact", width: desiredWidth, height: desiredHeight));
            Assert.NotNull(destination.Data);
            Assert.Equal(desiredWidth, destination.Data.Width);
            Assert.Equal(desiredHeight, destination.Data.Height);
            Assert.Equal(ImageTypes.Jpeg, destination.Data.ImageType);
        }

        [Theory]
        [InlineData(200, 200, 300, null)]
        [InlineData(777, 800, 1920, null)]
        [InlineData(5104, 4105, 1920, null)]
        [InlineData(200, 200, null, 300)]
        [InlineData(777, 800, null, 1920)]
        [InlineData(5104, 4105, null, 1920)]
        public async Task Exact(int sourceWidth, int sourceHeight, int? desiredWidth, int? desiredHeight)
        {
            var resizer = _serviceProvider.GetRequiredService<IImageResizer>();
            var source = DebugImageData.CreateSource(new DebugImageData(sourceWidth, sourceHeight, ImageTypes.Jpeg));
            var destination = DebugImageData.CreateDestination();
            await resizer.ResizeAsync(source, destination, new ResizeOptions(resizeMode: "exact", width: desiredWidth, height: desiredHeight));
            Assert.NotNull(destination.Data);
            if (desiredWidth.HasValue)
            {
                Assert.Equal(desiredWidth.Value, destination.Data.Width);
                var h = (int)((double)desiredWidth.Value * sourceHeight / sourceWidth);
                Assert.Equal(h, destination.Data.Height);
            }
            if (desiredHeight.HasValue)
            {
                Assert.Equal(desiredHeight.Value, destination.Data.Height);
                var w = (int)((double)desiredHeight.Value * sourceWidth / sourceHeight);
                Assert.Equal(w, destination.Data.Width);
            }
            Assert.Equal(ImageTypes.Jpeg, destination.Data.ImageType);
        }
    }
}