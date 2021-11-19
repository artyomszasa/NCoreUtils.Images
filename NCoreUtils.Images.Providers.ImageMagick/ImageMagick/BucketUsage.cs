using System.Numerics;

namespace NCoreUtils.Images.ImageMagick
{
    internal struct BucketUsage
    {
        public BigInteger Total { get; set; }

        public BigInteger Used { get; set; }

        public UsageData Snapshot() => new(Total, Used);

    }
}