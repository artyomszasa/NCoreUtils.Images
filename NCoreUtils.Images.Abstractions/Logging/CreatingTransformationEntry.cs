using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Logging;

public readonly struct CreatingTransformationEntry(ResizeOptions options) : ISpanExactEmplaceable
{
    public static Func<CreatingTransformationEntry, Exception?, string> Formatter { get; } =
        (entry, _) => entry.ToString();

    public ResizeOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    private int GetEmplaceBufferSize()
        => 38 + Options.GetEmplaceBufferSize();

    bool ISpanEmplaceable.TryGetEmplaceBufferSize(out int minimumBufferSize)
    {
        minimumBufferSize = GetEmplaceBufferSize();
        return true;
    }

    int ISpanExactEmplaceable.GetEmplaceBufferSize()
        => GetEmplaceBufferSize();

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
        throw new ArgumentException("Insufficient buffer size.", nameof(span));
    }
#endif

    public bool TryEmplace(Span<char> span, out int used)
    {
        var builder = new SpanBuilder(span);
        if (builder.TryAppend("Creating transformation with options ")
            && builder.TryAppend(Options, ResizeOptions.Emplacer)
            && builder.TryAppend('.'))
        {
            used = builder.Length;
            return true;
        }
        used = default;
        return false;
    }

    public override string ToString()
        => Emplacer.ToStringUsingArrayPool(this);
}