using System;

namespace NCoreUtils.Images.Internal
{
    [Serializable]
    public readonly struct Point : IEquatable<Point>
    {
        public static bool operator==(Point a, Point b)
            => a.Equals(b);

        public static bool operator!=(Point a, Point b)
            => !a.Equals(b);

        public int X { get; }

        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Point other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object? obj)
            => obj is Point other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(X, Y);
    }
}