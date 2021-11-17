using System;

namespace NCoreUtils.Images.Internal
{
    [Serializable]
    public struct Size : IEquatable<Size>
    {
        public static bool operator==(Size a, Size b)
            => a.Equals(b);

        public static bool operator!=(Size a, Size b)
            => !a.Equals(b);

        public int Width { get; }

        public int Height { get; }

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(Size other)
            => Width == other.Width
                && Height == other.Height;

        public override bool Equals(object? obj)
            => obj is Size other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Width, Height);
    }
}