using System;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal;

public class Blur(double sigma = 1.0) : IFilter, ISpanExactEmplaceable
{
    public static IEmplacer<Blur> Emplacer { get; } = new SpanEmplaceableEmplacer<Blur>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetEmplaceBufferSize() => 5 + 36 + 1;

    public double Sigma { get; } = sigma;

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

    public override string ToString()
        => NCoreUtils.Emplacer.ToStringUsingArrayPool(this);

    public bool TryEmplace(Span<char> span, out int used)
    {
        var builder = new SpanBuilder(span);
        if (builder.TryAppend("blur(")
            && builder.TryAppend(Sigma, 2)
            && builder.TryAppend(')'))
        {
            used = builder.Length;
            return true;
        }
        used = default;
        return false;
    }
}