using System.Numerics;

namespace NCoreUtils.Images.ImageMagick;

internal readonly partial record struct BucketUsage(BigInteger Total, BigInteger Used)
{
    public UsageData Snapshot() => new(Total, Used);
}