public readonly struct PoolHandle
{
    public readonly int Index;
    public readonly int Version;

    public bool IsValid => Index != -1;

    public PoolHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public static PoolHandle Invalid => new PoolHandle(-1, 0);
}
