using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Thrown if the supplied image is either unsupported or unprocessable.
    /// </summary>
    [Serializable]
    public class InvalidImageException : ImageException
    {
        protected InvalidImageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public InvalidImageException(string description)
            : base(ErrorCodes.InvalidImage, description)
        { }

        public InvalidImageException(string description, Exception innerException)
            : base(ErrorCodes.InvalidImage, description, innerException)
        { }
    }
}