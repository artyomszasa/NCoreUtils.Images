using System.Runtime.CompilerServices;

namespace NCoreUtils.Images.Internal;

public readonly partial record struct Rectangle(Point Point, Size Size);

public readonly partial record struct Rectangle
{
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

    public uint Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Size.Width;
    }

    public uint Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Size.Height;
    }

    public Rectangle(int x, int y, uint width, uint height)
        : this(new Point(x, y), new Size(width, height))
    { }
}