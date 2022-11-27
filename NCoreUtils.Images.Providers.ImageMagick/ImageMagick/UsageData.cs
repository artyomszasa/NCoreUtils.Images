using System.Numerics;

namespace NCoreUtils.Images.ImageMagick;

public readonly record struct UsageData(BigInteger Total, BigInteger Used);