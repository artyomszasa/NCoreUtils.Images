namespace NCoreUtils.Images
{
    /// <summary>
    /// Defines default resize modes.
    /// </summary>
    public static class ResizeModes
    {
        /// <summary>
        /// No resizing is performed. My be used to convert or optimize images wihtout resizing.
        /// </summary>
        public static string None { get; } = "none";

        /// <summary>
        /// Represents resizing to the exact size.
        /// </summary>
        public static string Exact { get; } = "exact";

        /// <summary>
        /// Represents best-fit resizing to the specified box.
        /// </summary>
        public static string Inbox { get; } = "inbox";
    }
}