using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Logging;

public readonly struct InitializingDestinationEntry(string contentType) : ISpanExactEmplaceable
{
    public static Func<InitializingDestinationEntry, Exception?, string> Formatter { get; } =
        (entry, _) => entry.ToString();

    public string ContentType { get; } = contentType ?? string.Empty;

    private int GetEmplaceBufferSize()
        => 50 + ContentType.Length;

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
        throw new InsufficientBufferSizeException(span);
    }
#endif

    public bool TryEmplace(Span<char> span, out int used)
    {
        var builder = new SpanBuilder(span);
        if (builder.TryAppend("Initializing image destination with content type ")
            && builder.TryAppend(ContentType)
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