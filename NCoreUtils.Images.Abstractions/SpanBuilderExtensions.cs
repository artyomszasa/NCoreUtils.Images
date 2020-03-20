using System.Runtime.CompilerServices;

namespace NCoreUtils.Images
{
    static class SpanBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (first)
                {
                    first = false;
                    builder.Append(", ");
                }
                builder.Append(key);
                builder.Append(" = ");
                builder.Append(value!);
            }
            return ref builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, int? value)
        {
            if (value.HasValue)
            {
                if (first)
                {
                    first = false;
                    builder.Append(", ");
                }
                builder.Append(key);
                builder.Append(" = ");
                builder.Append(value);
            }
            return ref builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, bool? value)
        {
            if (value.HasValue)
            {
                if (first)
                {
                    first = false;
                    builder.Append(", ");
                }
                builder.Append(key);
                builder.Append(" = ");
                builder.Append(value);
            }
            return ref builder;
        }
    }
}