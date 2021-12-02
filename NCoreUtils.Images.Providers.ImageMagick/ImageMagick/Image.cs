using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using NCoreUtils.Images.Internal;
using NCoreUtils.IO;

namespace NCoreUtils.Images.ImageMagick
{
    public sealed class Image : IImage, IDisposable
    {
        private static HashSet<string> MultiImageTypes { get; } = new HashSet<string>
        {
            ImageTypes.Pdf,
            ImageTypes.Gif,
            ImageTypes.WebP
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SupportsMultiImageOutput(string imageType)
            => MultiImageTypes.Contains(imageType);


        private static ValueTask WriteToAsync(IMagickImage<ushort> source, Stream stream, string imageType, int quality, bool optimize, CancellationToken cancellationToken)
        {
            source.Quality = quality;
            if (optimize)
            {
                // strips all meta but the color profile.
                foreach (var profile in source.ProfileNames)
                {
                    if (profile != "icc" && source.HasProfile(profile))
                    {
                        source.RemoveProfile(profile);
                    }
                }
                switch (imageType)
                {
                    case ImageTypes.Jpeg:
                        source.Interlace = Interlace.Jpeg;
                        source.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
                        break;
                    case ImageTypes.Png:
                        source.Interlace = Interlace.Png;
                        break;
                    case ImageTypes.Gif:
                        source.Interlace = Interlace.Gif;
                        break;
                }
            }
            var task = source.WriteAsync(stream, Helpers.ImageTypeToMagickFormat(imageType), cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                return default;
            }
            return new ValueTask(task);
        }

        private static ValueTask WriteToAsync(IMagickImageCollection<ushort> images, Stream stream, string imageType, int quality, bool optimize, CancellationToken cancellationToken)
        {
            foreach (var source in images)
            {
                source.Quality = quality;
                if (optimize)
                {
                    // strips all meta but the color profile.
                    foreach (var profile in source.ProfileNames)
                    {
                        if (profile != "icc" && source.HasProfile(profile))
                        {
                            source.RemoveProfile(profile);
                        }
                    }
                    switch (imageType)
                    {
                        case ImageTypes.Jpeg:
                            source.Interlace = Interlace.Jpeg;
                            source.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
                            break;
                        case ImageTypes.Png:
                            source.Interlace = Interlace.Png;
                            break;
                        case ImageTypes.Gif:
                            source.Interlace = Interlace.Gif;
                            break;
                    }
                }
            }
            var task = images.WriteAsync(stream, Helpers.ImageTypeToMagickFormat(imageType), cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                return default;
            }
            return new ValueTask(task);
        }

        readonly IMagickImageCollection<ushort> _native;

        int _isDisposed;

        IImageProvider IImage.Provider => Provider;

        object IImage.NativeImageType => NativeImageType;

        public ImageProvider Provider { get; }

        public Size Size { get; }

        public MagickFormat NativeImageType => _native.Count switch
        {
            0 => MagickFormat.Unknown,
            1 => _native[0].Format,
            _ => _native[0].Format == MagickFormat.Gif || _native[0].Format == MagickFormat.Gif87
                ? MagickFormat.Gif
                : _native[0].Format == MagickFormat.Pdf
                    ? MagickFormat.Pdf
                    : MagickFormat.Unknown
        };

        public string ImageType { get; }

        public Image(IMagickImageCollection<ushort> native, ImageProvider provider)
        {
            _native = native;
            Provider = provider;
            Size = _native.Count switch
            {
                0 => default,
                1 => new Size(native[0].Width, native[0].Height),
                _ => native.Aggregate(default(Size), (size, i) => new Size(Math.Max(size.Width, i.Width), Math.Max(size.Height, i.Height)))
            };
            var nativeImageType = NativeImageType;
            ImageType = Helpers.MagickFormatToImageType(nativeImageType);
            // GIF: ensure all subimages have the same size
            if (_native.Count > 0 && nativeImageType != MagickFormat.Pdf)
            {
                _native.Coalesce();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException("Image");
            }
        }

        public ValueTask WriteToAsync(Stream stream, string imageType, int quality = 85, bool optimize = true, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _native.Count switch
            {
                0 => throw new InvalidOperationException("Unable to write empty image."),
                1 => WriteToAsync(_native[0], stream, imageType, quality, optimize, cancellationToken),
                _ when !SupportsMultiImageOutput(imageType) => WriteToAsync(_native[0], stream, imageType, quality, optimize, cancellationToken),
                _ => WriteToAsync(_native, stream, imageType, quality, optimize, cancellationToken),
            };
        }

        private async ValueTask ApplyWaterMarkAsync(WaterMark waterMark, CancellationToken cancellationToken)
        {
            using var waterMarkImage = await waterMark.WaterMarkSource
                .CreateProducer()
                .ConsumeAsync(StreamConsumer.Create(Provider.FromStreamAsync), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (waterMarkImage._native.Count > 0)
            {
                if (waterMark.X.HasValue && waterMark.Y.HasValue)
                {
                    await waterMarkImage.ResizeAsync(new Size(waterMark.X.Value, waterMark.Y.Value), cancellationToken);
                }
                var gravity = waterMark.Gravity switch
                {
                    WaterMarkGravity.Center => Gravity.Center,
                    WaterMarkGravity.Undefined => Gravity.Undefined,
                    WaterMarkGravity.Northwest => Gravity.Northwest,
                    WaterMarkGravity.North => Gravity.North,
                    WaterMarkGravity.Northeast => Gravity.Northeast,
                    WaterMarkGravity.West => Gravity.West,
                    WaterMarkGravity.East => Gravity.East,
                    WaterMarkGravity.Southwest => Gravity.Southwest,
                    WaterMarkGravity.South => Gravity.South,
                    WaterMarkGravity.Southeast => Gravity.Southeast,
                    _ => throw new NotImplementedException($"'{waterMark.Gravity}' gravity not implmeneted."),
                };
                foreach (var img in _native)
                {
                    img.Composite(waterMarkImage._native[0], gravity);
                }
            }
        }

        public ValueTask ApplyFilterAsync(IFilter filter, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            switch (filter)
            {
                case Blur blur:
                    foreach (var img in _native)
                    {
                        img.Blur(0.0, blur.Sigma);
                    }
                    return default;
                case WaterMark waterMark:
                    return ApplyWaterMarkAsync(waterMark, cancellationToken);
                default:
                    throw new InternalImageException("not_supported_filter", $"Filter {filter} is not supported by imagemagick provider.");
            }
        }

        public ValueTask CropAsync(Rectangle rect, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var img in _native)
            {
                img.Crop(rect.ToMagickGeometry());
            }
            _native.RePage();
            return default;
        }

        public ValueTask<ImageInfo> GetImageInfoAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var iptc = new Dictionary<string, string>();
            var exif = new Dictionary<string, string>();
            foreach (var img in _native)
            {
                try
                {
                    var iptcProfile = img.GetIptcProfile();
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
                    var exifProfile = img.GetExifProfile();
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
            }
            int xResolution;
            int yResolution;
            if (_native.Count > 0)
            {
                var density = _native[0].Density;
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
            }
            else
            {
                xResolution = 0;
                yResolution = 0;
            }
            return new ValueTask<ImageInfo>(new ImageInfo(Size.Width, Size.Height, xResolution, yResolution, iptc, exif));
        }

        public ValueTask NormalizeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            foreach (var img in _native)
            {
                img.AutoOrient();
                try
                {
                    img.GetExifProfile()?.SetValue(ExifTag.Orientation, (ushort)0);
                }
                catch { }
            }
            return default;
        }

        public ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            foreach (var img in _native)
            {
                if (img.Format == MagickFormat.Pdf)
                {
                    img.Alpha(AlphaOption.Off);
                }
                img.Resize(size.Width, size.Height);
            }
            return default;
        }

        #region disposable

        private void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (disposing)
                {
                    _native.Dispose();
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        #endregion
    }
}