using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Images.Internal
{
    [Serializable]
    public readonly struct Rectangle : IEquatable<Rectangle>
    {
        public static bool operator==(Rectangle a, Rectangle b)
            => a.Equals(b);

        public static bool operator!=(Rectangle a, Rectangle b)
            => !a.Equals(b);

        public Point Point { get; }

        public Size Size { get; }

        public int X
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Point.X;
        }

        public int Y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Point.Y;
        }

        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size.Width;
        }

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size.Height;
        }

        public Rectangle(Point point, Size size)
        {
            Point = point;
            Size = size;
        }

        public Rectangle(int x, int y, int width, int height)
            : this(new Point(x, y), new Size(width, height))
        { }

        public bool Equals(Rectangle other)
            => Point == other.Point
                && Size == other.Size;

        public override bool Equals(object? obj)
            => obj is Rectangle other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Point, Size);
    }
}