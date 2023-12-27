using System;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal;

public class WaterMark(IReadableResource waterMarkSource) : IFilter, ISpanExactEmplaceable
{
    public static IEmplacer<WaterMark> Emplacer { get; } = new SpanEmplaceableEmplacer<WaterMark>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetEmplaceBufferSize()
        => 10
            + 8192 // Source
            + 1
            + 21 // Gravity
            + 1
            + 21 // X
            + 1
            + 21 // Y
            + 1;

    public IReadableResource WaterMarkSource { get; } = waterMarkSource;

    public WaterMarkGravity Gravity { get; } = WaterMarkGravity.Northeast;

    public int? X { get; }

    public int? Y { get; }

    public WaterMark(IReadableResource waterMarkSource, WaterMarkGravity gravity)
        : this(waterMarkSource)
    {
        Gravity = gravity;
    }

    public WaterMark(IReadableResource waterMarkSource, WaterMarkGravity gravity, int x, int y)
        : this(waterMarkSource, gravity)
    {
        X = x;
        Y = y;
    }

    int IFilter.GetEmplaceBufferSize() => GetEmplaceBufferSize();

    int ISpanExactEmplaceable.GetEmplaceBufferSize() => GetEmplaceBufferSize();

    bool ISpanEmplaceable.TryGetEmplaceBufferSize(out int minimumBufferSize)
    {
        minimumBufferSize = GetEmplaceBufferSize();
        return true;
    }

#if NET6_0_OR_GREATER
    string IFormattable.ToString(string? format, System.IFormatProvider? formatProvider)
        => ToString();
#else
    public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
        => TryEmplace(destination, out charsWritten);

    public int Emplace(Span<char> span)
    {
        if (TryEmplace(span, out var used))
        {
            return used;
        }
        throw new InsufficientBufferSizeException(span);
    }
#endif

    public bool TryEmplace(Span<char> span, out int used)
    {
        var builder = new SpanBuilder(span);
        if (builder.TryAppend("watermark(")
            && builder.TryAppend(WaterMarkSource.ToString() ?? string.Empty)
            && builder.TryAppend(",")
            && builder.TryAppend((int)Gravity)
            && builder.TryAppend(",")
            && builder.TryAppend(X ?? 0)
            && builder.TryAppend(",")
            && builder.TryAppend(Y ?? 0)
            && builder.TryAppend(')'))
        {
            used = builder.Length;
            return true;
        }
        used = default;
        return false;
    }

    public override string ToString()
        => NCoreUtils.Emplacer.ToStringUsingArrayPool(this);
}