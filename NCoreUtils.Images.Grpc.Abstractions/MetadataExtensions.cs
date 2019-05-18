using System;
using System.Text;
using System.Text.RegularExpressions;
using Grpc.Core;

namespace NCoreUtils.Images.Proto
{
    public static class MetadataExtensions
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        static readonly byte[] _true = new byte[] { 1 };

        static readonly byte[] _false = new byte[] { 0 };

        static readonly Regex _regexNotAllowed = new Regex("[^a-z0-9A-Z._-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Metadata AddNotEmpty(this Metadata metadata, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (_regexNotAllowed.IsMatch(value))
                {
                    metadata.Add(key, _utf8.GetBytes(value));
                }
                else
                {
                    metadata.Add(key, value);
                }
            }
            return metadata;
        }

        public static Metadata AddNotEmpty(this Metadata metadata, string key, int? value)
        {
            if (value.HasValue)
            {
                metadata.Add(key, BitConverter.GetBytes(value.Value));
            }
            return metadata;
        }

        public static Metadata AddNotEmpty(this Metadata metadata, string key, bool? value)
        {
            if (value.HasValue)
            {
                metadata.Add(key, value.Value ? _true : _false);
            }
            return metadata;
        }

        public static string GetStringValue(this Metadata.Entry entry)
        {
            return entry.IsBinary ? _utf8.GetString(entry.ValueBytes) : entry.Value;
        }

        public static int GetInt32Value(this Metadata.Entry entry)
        {
            if (!entry.IsBinary)
            {
                throw new FormatException($"{entry.Key} has string value, binary expected.");
            }
            var data = entry.ValueBytes;
            if (sizeof(int) != data.Length)
            {
                throw new FormatException($"{entry.Key} has invalid length, {sizeof(int)} bytes expected.");
            }
            return BitConverter.ToInt32(data, 0);
        }

        public static bool GetBooleanValue(this Metadata.Entry entry)
        {
            if (!entry.IsBinary)
            {
                throw new FormatException($"{entry.Key} has string value, binary expected.");
            }
            var data = entry.ValueBytes;
            if (1 != data.Length)
            {
                throw new FormatException($"{entry.Key} has invalid length, 1 byte expected.");
            }
            return 0 != data[0];
        }
    }
}