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
            var builder = new SpanBuilder(buffer);
            builder.Append("blur(");
            builder.Append(Sigma, 2);
            builder.Append(')');
            return builder.ToString();
        }

        public int Emplace(Span<char> span)
        {
            var builder = new SpanBuilder(span);
            builder.Append("blur(");
            builder.Append(Sigma, 2);
            builder.Append(')');
            return builder.Length;
        }
    }
}