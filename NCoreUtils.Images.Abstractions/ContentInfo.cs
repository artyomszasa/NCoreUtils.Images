using System;

namespace NCoreUtils.Images
{
    /// <summary>
    /// Represents optional minimal information about the content.
    /// </summary>
    [Serializable]
    public struct ContentInfo : IEquatable<ContentInfo>
    {
        public static bool operator==(ContentInfo a, ContentInfo b)
            => a.Equals(b);

        public static bool operator!=(ContentInfo a, ContentInfo b)
            => !a.Equals(b);

        /// <summary>
        /// Media type if any.
        /// </summary>
        public string? Type { get; }

        /// <summary>
        /// Content length if available.
        /// </summary>
        public long? Length { get; }

        public ContentInfo(string type, long? length = default)
        {
            Type = type;
            Length = length;
        }

        public ContentInfo(long length)
        {
            Type = default;
            Length = length;
        }

        public bool Equals(ContentInfo other)
            => Type == other.Type
                && Length == other.Length;

        public override bool Equals(object? obj)
            => obj is ContentInfo other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Type, Length);
    }
}