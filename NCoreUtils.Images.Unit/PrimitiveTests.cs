using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NCoreUtils.Images.Logging;
using Xunit;

namespace NCoreUtils.Images
{
    public class PrimitiveTests
    {
        [Serializable]
        private class TestException : Exception
        {
            protected TestException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { }

            public TestException() : base() { }
        }

        private static object Reserialize<T>(T value)
        {
            var formatter = new BinaryFormatter();
            using var buffer = new MemoryStream();
            formatter.Serialize(buffer, value);
            buffer.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(buffer);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
        public void ContentInfoTests()
        {
            ContentInfo ci0 = default;
            var ci1 = new ContentInfo("image/jpeg");
            var ci2 = new ContentInfo("image/png");
            var ci3 = new ContentInfo(200);
            var ci4 = new ContentInfo(400);
            var ci5 = new ContentInfo("image/jpeg", 200);
            Assert.Null(ci0.Type);
            Assert.False(ci0.Length.HasValue);
            Assert.Equal("image/jpeg", ci1.Type);
            Assert.False(ci1.Length.HasValue);
            Assert.Equal("image/png", ci2.Type);
            Assert.False(ci2.Length.HasValue);
            Assert.Null(ci3.Type);
            Assert.True(ci3.Length.HasValue);
            Assert.Equal(200, ci3.Length.Value);
            Assert.Null(ci4.Type);
            Assert.True(ci4.Length.HasValue);
            Assert.Equal(400, ci4.Length.Value);
            Assert.Equal("image/jpeg", ci5.Type);
            Assert.True(ci5.Length.HasValue);
            Assert.Equal(200, ci5.Length.Value);

#pragma warning disable CS1718

            Assert.False(ci0 != ci0);

            Assert.True(ci0 == ci0);
            Assert.False(ci0 == ci1);
            Assert.False(ci0 == ci2);
            Assert.False(ci0 == ci3);
            Assert.False(ci0 == ci4);
            Assert.False(ci0 == ci5);

            Assert.True(ci1 == ci1);
            Assert.False(ci1 == ci2);
            Assert.False(ci1 == ci3);
            Assert.False(ci1 == ci4);
            Assert.False(ci1 == ci5);

            Assert.True(ci2 == ci2);
            Assert.False(ci2 == ci3);
            Assert.False(ci2 == ci4);
            Assert.False(ci2 == ci5);

            Assert.True(ci3 == ci3);
            Assert.False(ci3 == ci4);
            Assert.False(ci3 == ci5);

            Assert.True(ci4 == ci4);
            Assert.False(ci4 == ci5);

            Assert.True(ci5 == ci5);

#pragma warning restore CS1718

            Assert.False(((object)ci1).Equals(ci2));
            Assert.False(((object)ci1).Equals(2));

            Assert.NotEqual(ci0.GetHashCode(), ci1.GetHashCode());
            Assert.NotEqual(ci0.GetHashCode(), ci2.GetHashCode());
            Assert.NotEqual(ci0.GetHashCode(), ci3.GetHashCode());
            Assert.NotEqual(ci0.GetHashCode(), ci4.GetHashCode());
            Assert.NotEqual(ci0.GetHashCode(), ci5.GetHashCode());

            Assert.NotEqual(ci1.GetHashCode(), ci2.GetHashCode());
            Assert.NotEqual(ci1.GetHashCode(), ci3.GetHashCode());
            Assert.NotEqual(ci1.GetHashCode(), ci4.GetHashCode());
            Assert.NotEqual(ci1.GetHashCode(), ci5.GetHashCode());

            Assert.NotEqual(ci2.GetHashCode(), ci3.GetHashCode());
            Assert.NotEqual(ci2.GetHashCode(), ci4.GetHashCode());
            Assert.NotEqual(ci2.GetHashCode(), ci5.GetHashCode());

            Assert.NotEqual(ci3.GetHashCode(), ci4.GetHashCode());
            Assert.NotEqual(ci3.GetHashCode(), ci5.GetHashCode());

            Assert.NotEqual(ci4.GetHashCode(), ci5.GetHashCode());


        }

        [Fact]
        public void ResizeOptionsTests()
        {
            var o0 = new ResizeOptions(filters: new Internal.IFilter[0]);
            var o1 = new ResizeOptions(imageType: ImageTypes.Jpeg);
            var o2 = new ResizeOptions(width: 200);
            var o3 = new ResizeOptions(height: 200);
            var o4 = new ResizeOptions(resizeMode: "inbox");
            var o5 = new ResizeOptions(quality: 50);
            var o6 = new ResizeOptions(optimize: true);
            var o7 = new ResizeOptions(weightX: 0);
            var o8 = new ResizeOptions(weightY: 0);
            var o9 = new ResizeOptions(filters: new Internal.Blur(20.2));
            var o10 = new ResizeOptions(filters: new [] { new Internal.Blur(20.2), new Internal.Blur(5) });
            var o11 = new ResizeOptions(imageType: ImageTypes.Jpeg, width: 200, height: 200);

            Assert.Equal("[]", o0.ToString());
            Assert.Equal("[ImageType = jpeg]", o1.ToString());
            Assert.Equal("[Width = 200]", o2.ToString());
            Assert.Equal("[Height = 200]", o3.ToString());
            Assert.Equal("[ResizeMode = inbox]", o4.ToString());
            Assert.Equal("[Quality = 50]", o5.ToString());
            Assert.Equal("[Optimize = True]", o6.ToString());
            Assert.Equal("[WeightX = 0]", o7.ToString());
            Assert.Equal("[WeightY = 0]", o8.ToString());
            Assert.Equal("[Filter = blur(20.10)]", o9.ToString());
            Assert.Equal("[Filter = blur(20.10), Filter = blur(5)]", o10.ToString());
            Assert.Equal("[ImageType = jpeg, Width = 200, Height = 200]", o11.ToString());

            Assert.Equal("blur(20.10)", new Internal.Blur(20.10).ToString());
        }

        [Fact]
        public void ImageTypesTests()
        {
            Assert.Equal(ImageTypes.Jpeg, ImageTypes.OfExtension("jpg"));
            Assert.Equal(ImageTypes.Jpeg, ImageTypes.OfExtension("jpeg"));
            Assert.Equal(ImageTypes.Tiff, ImageTypes.OfExtension("tif"));
            Assert.Equal(ImageTypes.Tiff, ImageTypes.OfExtension("tiff"));
            Assert.Equal(ImageTypes.Bmp, ImageTypes.OfExtension("bmp"));
            Assert.Equal(ImageTypes.Png, ImageTypes.OfExtension("png"));
            Assert.Equal(ImageTypes.Gif, ImageTypes.OfExtension("gif"));
            Assert.Equal(ImageTypes.WebP, ImageTypes.OfExtension("webp"));

            Assert.Equal("jpg", ImageTypes.ToExtension(ImageTypes.Jpeg));
            Assert.Equal("tiff", ImageTypes.ToExtension(ImageTypes.Tiff));
            Assert.Equal("bmp", ImageTypes.ToExtension(ImageTypes.Bmp));
            Assert.Equal("png", ImageTypes.ToExtension(ImageTypes.Png));
            Assert.Equal("gif", ImageTypes.ToExtension(ImageTypes.Gif));
            Assert.Equal("webp", ImageTypes.ToExtension(ImageTypes.WebP));

            Assert.Equal("image/jpeg", ImageTypes.ToMediaType(ImageTypes.Jpeg));
            Assert.Equal("image/tiff", ImageTypes.ToMediaType(ImageTypes.Tiff));
            Assert.Equal("image/bmp", ImageTypes.ToMediaType(ImageTypes.Bmp));
            Assert.Equal("image/png", ImageTypes.ToMediaType(ImageTypes.Png));
            Assert.Equal("image/gif", ImageTypes.ToMediaType(ImageTypes.Gif));
            Assert.Equal("image/webp", ImageTypes.ToMediaType(ImageTypes.WebP));

            Assert.Equal(ImageTypes.Jpeg, ImageTypes.OfMediaType("image/jpg"));
            Assert.Equal(ImageTypes.Jpeg, ImageTypes.OfMediaType("image/jpeg"));
            Assert.Equal(ImageTypes.Jpeg, ImageTypes.OfMediaType("image/p-jpeg"));
            Assert.Equal(ImageTypes.Tiff, ImageTypes.OfMediaType("image/tiff"));
            Assert.Equal(ImageTypes.Bmp, ImageTypes.OfMediaType("image/bmp"));
            Assert.Equal(ImageTypes.Png, ImageTypes.OfMediaType("image/png"));
            Assert.Equal(ImageTypes.Gif, ImageTypes.OfMediaType("image/gif"));
            Assert.Equal(ImageTypes.WebP, ImageTypes.OfMediaType("image/webp"));
            Assert.Equal("unknown", ImageTypes.OfMediaType("application/pdf"));
        }

        [Fact]
        public void PointTests()
        {
            Internal.Point p0 = default;
            var p1 = new Internal.Point(200, 300);
            var p2 = new Internal.Point(300, 200);

#pragma warning disable CS1718

            Assert.True(p0 == p0);
            Assert.False(p0 == p1);
            Assert.False(p0 == p2);

            Assert.True(p1 == p1);
            Assert.False(p0 == p2);
            Assert.True(p0 != p2);

            Assert.True(p2 == p2);

#pragma warning restore CS1718

            Assert.True(((object)p1).Equals(p1));
            Assert.False(((object)p1).Equals(2));

            Assert.Equal(p0.GetHashCode(), default(Internal.Point).GetHashCode());
            Assert.NotEqual(p0.GetHashCode(), p1.GetHashCode());
            Assert.NotEqual(p0.GetHashCode(), p2.GetHashCode());
            Assert.NotEqual(p1.GetHashCode(), p2.GetHashCode());

        }

        [Fact]
        public void SizeTests()
        {
            Internal.Size p0 = default;
            var p1 = new Internal.Size(200, 300);
            var p2 = new Internal.Size(300, 200);

#pragma warning disable CS1718

            Assert.True(p0 == p0);
            Assert.False(p0 == p1);
            Assert.False(p0 == p2);

            Assert.True(p1 == p1);
            Assert.False(p0 == p2);
            Assert.True(p0 != p2);

            Assert.True(p2 == p2);

#pragma warning restore CS1718

            Assert.True(((object)p1).Equals(p1));
            Assert.False(((object)p1).Equals(2));

            Assert.Equal(p0.GetHashCode(), default(Internal.Size).GetHashCode());
            Assert.NotEqual(p0.GetHashCode(), p1.GetHashCode());
            Assert.NotEqual(p0.GetHashCode(), p2.GetHashCode());
            Assert.NotEqual(p1.GetHashCode(), p2.GetHashCode());

        }

        [Fact]
        public void RectangleTests()
        {
            Internal.Rectangle r0 = default;
            var r1 = new Internal.Rectangle(200, 200, 400, 400);
            var r2 = new Internal.Rectangle(new Internal.Point(200, 200), new Internal.Size(400, 400));

            Assert.Equal(200, r1.X);
            Assert.Equal(200, r1.Y);
            Assert.Equal(400, r1.Width);
            Assert.Equal(400, r1.Height);

            Assert.Equal(200, r2.X);
            Assert.Equal(200, r2.Y);
            Assert.Equal(400, r2.Width);
            Assert.Equal(400, r2.Height);

            Assert.True(r0 != r1);
            Assert.True(r1 == r2);

            Assert.True(((object)r1).Equals(r2));
            Assert.False(((object)r0).Equals(r2));
            Assert.False(((object)r0).Equals(2));

            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.NotEqual(r0.GetHashCode(), r2.GetHashCode());
        }

        [Fact]
        public void ExceptionTests()
        {
            var te = new TestException();
            var exn0 = new InternalImageException("internal_code", "xxx");
            var exn1 = new InternalImageException("internal_code", "xxx", te);
            var exn2 = new InvalidImageException("xxx");
            var exn3 = new InvalidImageException("xxx", te);
            var exn4 = new UnsupportedImageTypeException(ImageTypes.Jpeg, "xxx");
            var exn5 = new UnsupportedImageTypeException(ImageTypes.Jpeg, "xxx", te);
            var exn6 = new UnsupportedResizeModeException("x", 1, 2, "xxx");
            var exn7 = new UnsupportedResizeModeException("x", 1, 2, "xxx", te);
            Assert.Equal(ErrorCodes.InternalError, exn0.ErrorCode);
            Assert.Equal(ErrorCodes.InternalError, exn1.ErrorCode);
            Assert.Equal(ErrorCodes.InvalidImage, exn2.ErrorCode);
            Assert.Equal(ErrorCodes.InvalidImage, exn3.ErrorCode);
            Assert.Equal(ErrorCodes.UnsupportedImageType, exn4.ErrorCode);
            Assert.Equal(ErrorCodes.UnsupportedImageType, exn5.ErrorCode);
            Assert.Equal(ErrorCodes.UnsupportedResizeMode, exn6.ErrorCode);
            Assert.Equal(ErrorCodes.UnsupportedResizeMode, exn7.ErrorCode);

            var e0 = Assert.IsType<InternalImageException>(Reserialize(exn0));
            Assert.Equal("internal_code", e0.InternalCode);
            Assert.Equal(ErrorCodes.InternalError, e0.ErrorCode);
            Assert.Equal("xxx", e0.Message);
            Assert.Null(e0.InnerException);

            var e1 = Assert.IsType<InternalImageException>(Reserialize(exn1));
            Assert.Equal("internal_code", e1.InternalCode);
            Assert.Equal(ErrorCodes.InternalError, e1.ErrorCode);
            Assert.Equal("xxx", e1.Message);
            Assert.NotNull(e1.InnerException);
            Assert.IsType<TestException>(e1.InnerException);

            var e2 = Assert.IsType<InvalidImageException>(Reserialize(exn2));
            Assert.Equal(ErrorCodes.InvalidImage, e2.ErrorCode);
            Assert.Equal("xxx", e2.Message);
            Assert.Null(e2.InnerException);

            var e3 = Assert.IsType<InvalidImageException>(Reserialize(exn3));
            Assert.Equal(ErrorCodes.InvalidImage, e3.ErrorCode);
            Assert.Equal("xxx", e3.Message);
            Assert.NotNull(e3.InnerException);
            Assert.IsType<TestException>(e3.InnerException);

            var e4 = Assert.IsType<UnsupportedImageTypeException>(Reserialize(exn4));
            Assert.Equal(ImageTypes.Jpeg, e4.ImageType);
            Assert.Equal(ErrorCodes.UnsupportedImageType, e4.ErrorCode);
            Assert.Equal("xxx", e4.Message);
            Assert.Null(e4.InnerException);

            var e5 = Assert.IsType<UnsupportedImageTypeException>(Reserialize(exn5));
            Assert.Equal(ImageTypes.Jpeg, e5.ImageType);
            Assert.Equal(ErrorCodes.UnsupportedImageType, e5.ErrorCode);
            Assert.Equal("xxx", e5.Message);
            Assert.NotNull(e5.InnerException);
            Assert.IsType<TestException>(e5.InnerException);

            var e6 = Assert.IsType<UnsupportedResizeModeException>(Reserialize(exn6));
            Assert.Equal("x", e6.ResizeMode);
            Assert.Equal(1, e6.Width);
            Assert.Equal(2, e6.Height);
            Assert.Equal(ErrorCodes.UnsupportedResizeMode, e6.ErrorCode);
            Assert.Equal("xxx", e6.Message);
            Assert.Null(e6.InnerException);

            var e7 = Assert.IsType<UnsupportedResizeModeException>(Reserialize(exn7));
            Assert.Equal("x", e7.ResizeMode);
            Assert.Equal(1, e7.Width);
            Assert.Equal(2, e7.Height);
            Assert.Equal(ErrorCodes.UnsupportedResizeMode, e7.ErrorCode);
            Assert.Equal("xxx", e7.Message);
            Assert.NotNull(e7.InnerException);
            Assert.IsType<TestException>(e7.InnerException);
        }

        [Fact]
        public void ImageInfoTests()
        {
            var exif = new Dictionary<string, string>();
            var iptc = new Dictionary<string, string>();
            var ii = new ImageInfo(200, 300, 400, 500, iptc, exif);
            Assert.Equal(200, ii.Width);
            Assert.Equal(300, ii.Height);
            Assert.Equal(400, ii.XResolution);
            Assert.Equal(500, ii.YResolution);
            Assert.Same(exif, ii.Exif);
            Assert.Same(iptc, ii.Iptc);
        }

        [Fact]
        public void LogEntryTests()
        {
            var options = new ResizeOptions(imageType: ImageTypes.Jpeg, width: 200, height: 200);
            Assert.Equal(
                "Creating transformation with options [ImageType = jpeg, Width = 200, Height = 200].",
                CreatingTransformationEntry.Formatter(new CreatingTransformationEntry(options), default)
            );
            Assert.Equal(
                "Initializing image destination with content type image/jpeg.",
                InitializingDestinationEntry.Formatter(new InitializingDestinationEntry("image/jpeg"), default)
            );
            Assert.Equal(
                "Resizing image with computed options [ImageType = jpeg, Quality = 80, Optimize = True].",
                ResizingImageEntry.Formatter(new ResizingImageEntry(ImageTypes.Jpeg, true, 80, true), default)
            );
            Assert.Equal(
                "Resizing image with computed options [ImageType = jpeg (implicit), Quality = 80, Optimize = True].",
                ResizingImageEntry.Formatter(new ResizingImageEntry(ImageTypes.Jpeg, false, 80, true), default)
            );
        }
    }
}