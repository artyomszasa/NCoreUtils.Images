using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    [Serializable]
    public class GoogleCloudStorageUploadException : Exception
    {
        protected GoogleCloudStorageUploadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public GoogleCloudStorageUploadException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public GoogleCloudStorageUploadException(string message)
            : base(message)
        { }
    }
}