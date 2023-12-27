using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Images.Internal;
using NCoreUtils.Memory;

namespace NCoreUtils.Images;

internal static class SpanBuilderExtensions
{
    public static bool TryAppend(this ref SpanBuilder builder, bool value)
        => builder.TryAppend(value ? "true" : "false");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAppendOption(this ref SpanBuilder builder, scoped ref bool first, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(key)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value!);
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAppendOption(this ref SpanBuilder builder, scoped ref bool first, string key, int? value)
    {
        if (value.HasValue)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(key)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value.Value);
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAppendOption(this ref SpanBuilder builder, scoped ref bool first, string key, bool? value)
    {
        if (value.HasValue)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(key)
                && builder.TryAppend(" = ")
                && TryAppend(ref builder, value.Value);
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAppendOption<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this ref SpanBuilder builder,
        scoped ref bool first,
        string key,
        T? value,
        IEmplacer<T> emplacer)
        where T : class
    {
        if (value is not null)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(key)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value, emplacer);
        }
        return true;
    }

    public static bool TryAppendFilters(this ref SpanBuilder builder, scoped ref bool first, IEnumerable<IFilter> filters)
    {
        foreach (var filter in filters)
        {
            var success = filter switch
            {
                Blur blur => builder.TryAppendOption(ref first, "Filter", blur, Blur.Emplacer),
                WaterMark waterMark => builder.TryAppendOption(ref first, "Filter", waterMark, WaterMark.Emplacer),
                _ => builder.TryAppendOption(ref first, "Filter", filter.ToString())
            };
            if (!success)
            {
                return false;
            }
        }
        return true;
    }
}