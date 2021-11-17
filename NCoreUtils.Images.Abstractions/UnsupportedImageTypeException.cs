using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Thrown if the requested output image type is not supported.
    /// </summary>
    [Serializable]
    public class UnsupportedImageTypeException : ImageException
    {
        public string ImageType { get; }

        protected UnsupportedImageTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            => ImageType = info.GetString(nameof(ImageType)) ?? string.Empty;

        public UnsupportedImageTypeException(string imageType, string description, Exception innerException)
            : base(ErrorCodes.UnsupportedImageType, description, innerException)
            => ImageType = imageType;

        public UnsupportedImageTypeException(string imageType, string description)
            : base(ErrorCodes.UnsupportedImageType, description)
            => ImageType = imageType;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ImageType), ImageType);
        }
    }
}