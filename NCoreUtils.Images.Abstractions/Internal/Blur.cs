using System;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal
{
    public class Blur : IFilter, IEmplaceable<Blur>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEmplaceBufferSize() => 5 + 36 + 1;

        public double Sigma { get; }

        public Blur(double sigma = 1.0)
            => Sigma = sigma;

        int IFilter.GetEmplaceBufferSize() => GetEmplaceBufferSize();

        bool IEmplaceable<Blur>.TryGetEmplaceBufferSize(out int minimumBufferSize)
        {
            minimumBufferSize = GetEmplaceBufferSize();
            return true;
        }

#if NET6_0_OR_GREATER
        string IFormattable.ToString(string? format, System.IFormatProvider? formatProvider)
            => ToString();
#else
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
            => Emplacer.ToStringUsingArrayPool(this);

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
}