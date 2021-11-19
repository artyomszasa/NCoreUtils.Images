using ImageMagick;

namespace NCoreUtils.Images.ImageMagick
{
    public static class LogCategories
    {
        public const string Accelerate = nameof(MagickNET) + "." + nameof(LogEvents.Accelerate);
        public const string Annotate = nameof(MagickNET) + "." + nameof(LogEvents.Annotate);
        public const string Blob = nameof(MagickNET) + "." + nameof(LogEvents.Blob);
        public const string Cache = nameof(MagickNET) + "." + nameof(LogEvents.Cache);
        public const string Coder = nameof(MagickNET) + "." + nameof(LogEvents.Coder);
        public const string Configure = nameof(MagickNET) + "." + nameof(LogEvents.Configure);
        public const string Draw = nameof(MagickNET) + "." + nameof(LogEvents.Draw);
        public const string Image = nameof(MagickNET) + "." + nameof(LogEvents.Image);
        public const string Locale = nameof(MagickNET) + "." + nameof(LogEvents.Locale);
        public const string Module = nameof(MagickNET) + "." + nameof(LogEvents.Module);
        public const string Pixel = nameof(MagickNET) + "." + nameof(LogEvents.Pixel);
        public const string Policy = nameof(MagickNET) + "." + nameof(LogEvents.Policy);
        public const string Resource = nameof(MagickNET) + "." + nameof(LogEvents.Resource);
        public const string Transform = nameof(MagickNET) + "." + nameof(LogEvents.Transform);
        public const string User = nameof(MagickNET) + "." + nameof(LogEvents.User);
        public const string Wand = nameof(MagickNET) + "." + nameof(LogEvents.Wand);
    }
}