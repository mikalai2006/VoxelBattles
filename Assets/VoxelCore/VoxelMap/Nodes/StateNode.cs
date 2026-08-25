namespace Mikalai2006.VoxelMap
{
    [System.Flags]
    public enum StateNode
    {
        Disable = 1 << 0,
        Empty = 1 << 1,
        Occupied = 1 << 2,
        Tiled = 1 << 3,
        TiledInner = 1 << 4, // red point
        Tree = 1 << 5, // green point
        House = 1 << 6, // blue point
        TiledTop = 1 << 7,
        TiledInnerTop = 1 << 8
    }
}