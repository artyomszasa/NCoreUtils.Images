using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    [Serializable]
    public class GoogleCloudStorageAccessException : Exception
    {
        protected GoogleCloudStorageAccessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public GoogleCloudStorageAccessException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public GoogleCloudStorageAccessException(string message)
            : base(message)
        { }
    }
}