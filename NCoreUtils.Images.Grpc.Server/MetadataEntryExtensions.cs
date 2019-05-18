using System;
using System.Globalization;
using System.Text;
using Grpc.Core;

namespace NCoreUtils.Images.Grpc
{
    static class MetadataEntryExtensions
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        public static string GetStringValue(this Metadata.Entry entry)
        {
            return entry.IsBinary ? _utf8.GetString(entry.ValueBytes) : entry.Value;
        }

        public static int GetInt32Value(this Metadata.Entry entry)
        {
            if (int.TryParse(entry.GetStringValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }
            throw new FormatException($"{entry.Key} has value \"{entry.GetStringValue()}\" which is not valid integer value.");
        }

        public static bool GetBooleanValue(this Metadata.Entry entry)
        {
            var value = entry.GetStringValue();
            if ("true" == value)
            {
                return true;
            }
            if ("false" == value)
            {
                return false;
            }
            throw new FormatException($"{entry.Key} has value \"{entry.GetStringValue()}\" which is not valid boolean value.");
        }
    }
}