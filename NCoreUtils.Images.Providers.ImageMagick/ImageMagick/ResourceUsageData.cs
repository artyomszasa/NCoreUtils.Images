namespace NCoreUtils.Images.ImageMagick
{
#if NETSTANDARD2_1
    public struct ResourceUsageData
    {
        public UsageData Memory { get; }

        public UsageData Area { get; }

        public UsageData Disk { get; }

        public UsageData Map { get; }

        public UsageData File { get; }

        public ResourceUsageData(
            UsageData Memory,
            UsageData Area,
            UsageData Disk,
            UsageData Map,
            UsageData File)
        {
            this.Memory = Memory;
            this.Area = Area;
            this.Disk = Disk;
            this.Map = Map;
            this.File = File;
        }
    }
#else
    public readonly record struct ResourceUsageData(
        UsageData Memory,
        UsageData Area,
        UsageData Disk,
        UsageData Map,
        UsageData File
    );
#endif
}