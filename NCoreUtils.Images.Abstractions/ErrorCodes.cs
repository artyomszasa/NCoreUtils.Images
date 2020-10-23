namespace NCoreUtils.Images
{
    /// Contains predefined error codes.
    public static class ErrorCodes
    {
        /// Resize method not supported.
        public const string UnsupportedResizeMode = "unsupported_resize_mode";

        /// Requested output image type not supported.
        public const string UnsupportedImageType = "unsupported_image_type";

        /// Input image is invalid or not supported.
        public const string InvalidImage = "invalid_image";

        /// Implementation specific error occured while preforming resize or get-info operation.
        public const string InternalError = "internal_error";

        /// Generic error.
        public const string GenericError = "generic_error";
    }
}