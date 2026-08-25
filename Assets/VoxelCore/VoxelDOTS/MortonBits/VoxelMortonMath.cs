using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public static class VoxelMortonMath
{
    [BurstCompile]
    public static uint ExpandBits(uint v)
    {
        v = (v | (v << 16)) & 0x030000FF;
        v = (v | (v << 8)) & 0x0300F00F;
        v = (v | (v << 4)) & 0x030C30C3;
        v = (v | (v << 2)) & 0x09249249;
        return v;
    }

    // ФИКС: Добавлен модификатор 'in' для безопасной Burst-компиляции вектора int3
    [BurstCompile]
    public static int GetMortonIndex(in int3 localPos)
    {
        uint x = ExpandBits((uint)localPos.x);
        uint y = ExpandBits((uint)localPos.y);
        uint z = ExpandBits((uint)localPos.z);

        return (int)(x | (y << 1) | (z << 2));
    }
}
