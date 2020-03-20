using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Represents generic image processing error.
    /// </summary>
    [Serializable]
    public class ImageException : Exception
    {
        public string ErrorCode { get; }

        protected ImageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            => ErrorCode = info.GetString(nameof(ErrorCode));

        public ImageException(string errorCode, string description, Exception innerException)
            : base(description, innerException)
            => ErrorCode = errorCode;

        public ImageException(string errorCode, string description)
            : base(description)
            => ErrorCode = errorCode;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
        }
    }
}