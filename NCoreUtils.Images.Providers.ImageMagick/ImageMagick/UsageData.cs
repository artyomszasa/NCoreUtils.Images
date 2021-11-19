using System.Numerics;

namespace NCoreUtils.Images.ImageMagick
{
#if NETSTANDARD2_1
    public struct UsageData
    {
        public BigInteger Total { get; }

        public BigInteger Used { get; }

        public UsageData(BigInteger Total, BigInteger Used)
        {
            this.Total = Total;
            this.Used = Used;
        }
    }

#else
    public readonly record struct UsageData(BigInteger Total, BigInteger Used);
#endif
}