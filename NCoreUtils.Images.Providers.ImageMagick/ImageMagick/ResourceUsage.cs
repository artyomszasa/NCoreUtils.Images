namespace NCoreUtils.Images.ImageMagick;

internal class ResourceUsage
{
    private BucketUsage _memory;

    private BucketUsage _area;

    private BucketUsage _disk;

    private BucketUsage _map;

    private BucketUsage _file;

    public ref BucketUsage Memory
        => ref _memory;

    public ref BucketUsage Area
        => ref _area;

    public ref BucketUsage Disk
        => ref _disk;

    public ref BucketUsage Map
        => ref _map;

    public ref BucketUsage File
        => ref _file;

    public ResourceUsageData Snapshot() => new(
        Memory.Snapshot(),
        Area.Snapshot(),
        Disk.Snapshot(),
        Map.Snapshot(),
        File.Snapshot()
    );
}