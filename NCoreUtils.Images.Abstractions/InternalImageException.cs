using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Represents error that has occured in image implementation.
    /// </summary>
    [Serializable]
    public class InternalImageException : ImageException
    {
        public string InternalCode { get; }

        protected InternalImageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            => InternalCode = info.GetString(nameof(InternalCode));

        public InternalImageException(string internalCode, string description)
            : base(ErrorCodes.InternalError, description)
            => InternalCode = internalCode;

        public InternalImageException(string internalCode, string description, Exception innerException)
            : base(ErrorCodes.InternalError, description, innerException)
            => InternalCode = internalCode;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(InternalCode), InternalCode);
        }
    }
}