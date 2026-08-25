namespace Mikalai2006.VoxelMap
{
    [System.Flags]
    public enum TypeGround
    {
        None = 1 << 0,
        Dirt = 1 << 1,
        Grass = 1 << 2,
        Sand = 1 << 3,
        Water = 1 << 4,
    }
}