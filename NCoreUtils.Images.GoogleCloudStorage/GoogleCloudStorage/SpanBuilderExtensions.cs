using System;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    static class SpanBuilderExtensions
    {
        public static ref SpanBuilder AppendQ(this ref SpanBuilder builder, ref bool first, string name, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ref builder;
            }
            var escapedValue = Uri.EscapeDataString(value);
            if (first)
            {
                first = false;
                builder.Append('?');
            }
            else
            {
                builder.Append('&');
            }
            builder.Append(name);
            builder.Append('=');
            builder.Append(escapedValue);
            return ref builder;
        }

        public static ref SpanBuilder AppendQ(this ref SpanBuilder builder, ref bool first, string name, int? value)
        {
            if (!value.HasValue)
            {
                return ref builder;
            }
            var escapedValue = value.Value.ToString();
            if (first)
            {
                first = false;
                builder.Append('?');
            }
            else
            {
                builder.Append('&');
            }
            builder.Append(name);
            builder.Append('=');
            builder.Append(escapedValue);
            return ref builder;
        }

        public static ref SpanBuilder AppendQ(this ref SpanBuilder builder, ref bool first, string name, bool? value)
        {
            if (!value.HasValue)
            {
                return ref builder;
            }
            var escapedValue = value.Value ? "true" : "false";
            if (first)
            {
                first = false;
                builder.Append('?');
            }
            else
            {
                builder.Append('&');
            }
            builder.Append(name);
            builder.Append('=');
            builder.Append(escapedValue);
            return ref builder;
        }
    }
}