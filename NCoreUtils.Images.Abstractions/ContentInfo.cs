using System;

namespace NCoreUtils.Images
{
    [Serializable]
    public struct ContentInfo : IEquatable<ContentInfo>
    {
        public string? Type { get; }

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

        public override bool Equals(object obj)
            => obj is ContentInfo other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Type, Length);
    }
}