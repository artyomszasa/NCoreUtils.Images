using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images
{
    static class SpanBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAppendOption(this ref SpanBuilder builder, ref bool first, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (first)
                {
                    first = false;
                    if (!builder.TryAppend(", ")) { return false; }
                }
                if (!builder.TryAppend(key)) { return false; }
                if (!builder.TryAppend(" = ")) { return false; }
                if (!builder.TryAppend(value!)) { return false; }
            }
            return true;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, string? value)
        // {
        //     if (!string.IsNullOrEmpty(value))
        //     {
        //         if (first)
        //         {
        //             first = false;
        //             builder.Append(", ");
        //         }
        //         builder.Append(key);
        //         builder.Append(" = ");
        //         builder.Append(value!);
        //     }
        //     return ref builder;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAppendOption(this ref SpanBuilder builder, ref bool first, string key, int? value)
        {
            if (value.HasValue)
            {
                if (first)
                {
                    first = false;
                    if (!builder.TryAppend(", ")) { return false; }
                }
                if (!builder.TryAppend(key)) { return false; }
                if (!builder.TryAppend(" = ")) { return false; }
                if (!builder.TryAppend(value.Value)) { return false; }
            }
            return true;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, int? value)
        // {
        //     if (value.HasValue)
        //     {
        //         if (first)
        //         {
        //             first = false;
        //             builder.Append(", ");
        //         }
        //         builder.Append(key);
        //         builder.Append(" = ");
        //         builder.Append(value);
        //     }
        //     return ref builder;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAppendOption(this ref SpanBuilder builder, ref bool first, string key, bool? value)
        {
            if (value.HasValue)
            {
                if (first)
                {
                    first = false;
                    if (!builder.TryAppend(", ")) { return false; }
                }
                if (!builder.TryAppend(key)) { return false; }
                if (!builder.TryAppend(" = ")) { return false; }
                if (!builder.TryAppend(value.Value)) { return false; }
            }
            return true;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ref SpanBuilder AppendOption(this ref SpanBuilder builder, ref bool first, string key, bool? value)
        // {
        //     if (value.HasValue)
        //     {
        //         if (first)
        //         {
        //             first = false;
        //             builder.Append(", ");
        //         }
        //         builder.Append(key);
        //         builder.Append(" = ");
        //         builder.Append(value);
        //     }
        //     return ref builder;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAppendOption<T>(this ref SpanBuilder builder, ref bool first, string key, T? value)
            where T : class
        {
            if (!(value is null))
            {
                if (first)
                {
                    first = false;
                    if (!builder.TryAppend(", ")) { return false; }
                }
                if (!builder.TryAppend(key)) { return false; }
                if (!builder.TryAppend(" = ")) { return false; }
                if (!builder.TryAppend(value)) { return false; }
            }
            return true;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ref SpanBuilder AppendOption<T>(this ref SpanBuilder builder, ref bool first, string key, T? value)
        //     where T : class
        // {
        //     if (!(value is null))
        //     {
        //         if (first)
        //         {
        //             first = false;
        //             builder.Append(", ");
        //         }
        //         builder.Append(key);
        //         builder.Append(" = ");
        //         builder.Append(value!);
        //     }
        //     return ref builder;
        // }

        public static bool TryAppendFilters(this ref SpanBuilder builder, ref bool first, IEnumerable<IFilter> filters)
        {
            foreach (var filter in filters)
            {
                if (!builder.TryAppendOption(ref first, "Filter", filter))
                {
                    return false;
                }
            }
            return true;
        }
    }
}