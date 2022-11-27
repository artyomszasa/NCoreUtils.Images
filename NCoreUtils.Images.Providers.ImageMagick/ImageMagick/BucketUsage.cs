using System.Numerics;

namespace NCoreUtils.Images.ImageMagick;

internal partial record struct BucketUsage(BigInteger Total, BigInteger Used);

internal partial record struct BucketUsage
{
    public UsageData Snapshot() => new(Total, Used);
}