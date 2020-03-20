using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images.ImageMagick
{
    public sealed class Image : IImage
    {
        readonly MagickImage _native;

        int _isDisposed;

        IImageProvider IImage.Provider => Provider;

        public ImageProvider Provider { get; }

        public Size Size { get; }

        public object NativeImageType => _native.Format;

        public string ImageType { get; }

        public Image(MagickImage native, ImageProvider provider)
        {
            _native = native;
            Provider = provider;
            Size = new Size(native.Width, native.Height);
            ImageType = Helpers.MagickFormatToImageType(native.Format);
        }

        void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(Image));
            }
        }

        void WriteTo(Stream stream, string imageType, int quality = 85, bool optimize = true)
        {
            ThrowIfDisposed();
            _native.Quality = quality;
            if (optimize)
            {
                _native.Strip();
                switch (imageType)
                {
                    case ImageTypes.Jpeg:
                        _native.Interlace = Interlace.Jpeg;
                        _native.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
                        break;
                    case ImageTypes.Png:
                        _native.Interlace = Interlace.Png;
                        break;
                    case ImageTypes.Gif:
                        _native.Interlace = Interlace.Gif;
                        break;
                }
            }
            _native.Write(stream, Helpers.ImageTypeToMagickFormat(imageType));

        }

        public void Crop(Rectangle rect)
        {
            ThrowIfDisposed();
            _native.Crop(rect.ToMagickGeometry());
            _native.RePage();
        }

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                _native.Dispose();
            }
        }

        public ImageInfo GetImageInfo()
        {
            ThrowIfDisposed();
            var iptc = new Dictionary<string, string>();
            var exif = new Dictionary<string, string>();
            try
            {
                var iptcProfile = _native.GetIptcProfile();
                if (null != iptcProfile)
                {
                    foreach (var value in iptcProfile.Values)
                    {
                        var tag = value.Tag.ToString();
                        if (!iptc.ContainsKey(tag))
                        {
                            iptc.Add(tag, value.Value);
                        }
                    }
                }
            }
            catch (Exception exn)
            {
                Console.WriteLine(exn);
            }
            try
            {
                var exifProfile = _native.GetExifProfile();
                if (null != exifProfile)
                {
                    foreach (var value in exifProfile.Values)
                    {
                        var v = Helpers.StringifyImageData(value.GetValue());
                        if (null != v)
                        {
                            var tag = value.Tag.ToString();
                            if (!exif.ContainsKey(tag))
                            {
                                exif.Add(tag, v);
                            }
                        }
                    }
                }
            }
            catch (Exception exn)
            {
                Console.WriteLine(exn);
            }
            int xResolution;
            int yResolution;
            var density = _native.Density;
            if (density.Units == DensityUnit.PixelsPerCentimeter)
            {
                xResolution = (int)Math.Round(density.X * 2.54);
                yResolution = (int)Math.Round(density.Y * 2.54);
            }
            else
            {
                xResolution = (int)density.X;
                yResolution = (int)density.Y;
            }
            return new ImageInfo(Size.Width, Size.Height, xResolution, yResolution, iptc, exif);
        }

        public void Normalize()
        {
            ThrowIfDisposed();
            _native.AutoOrient();
            try
            {
                _native.GetExifProfile()?.SetValue(ExifTag.Orientation, (ushort)0);
            }
            catch { }
        }

        public void Resize(Size size)
        {
            ThrowIfDisposed();
            _native.Resize(size.Width, size.Height);
        }

        public ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteTo(stream, imageType, quality, optimize);
            return default;
        }
    }
}