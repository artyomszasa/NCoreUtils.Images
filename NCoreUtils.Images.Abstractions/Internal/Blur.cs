using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal
{
    public class Blur : IFilter, IEmplaceable<Blur>
    {
        public double Sigma { get; }

        public Blur(double sigma = 1.0)
        {
            Sigma = sigma;
        }

        public override string ToString()
        {
            Span<char> buffer = stackalloc char[256];
            var size = Emplace(buffer);
            return buffer[..size].ToString();
        }

        public int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var used))
            {
                return used;
            }
            throw new ArgumentException("Insufficient buffer size.", nameof(span));
        }

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