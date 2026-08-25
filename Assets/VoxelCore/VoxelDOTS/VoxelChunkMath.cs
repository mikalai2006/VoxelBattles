using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public static class VoxelChunkMath
{
    public const int Size = 32;
    public const int Volume = Size * Size * Size; // 32768

    // ИСПРАВЛЕНО ДЛЯ BURST: Добавлен модификатор 'in' (передача структуры по ссылке)
    [BurstCompile]
    public static int GetLinearIndex(in int3 localCoord)
    {
        // Побитовые сдвиги выполняются за 1 такт процессора
        return localCoord.x + (localCoord.y << 5) + (localCoord.z << 10);
    }

    // ИСПРАВЛЕНО ДЛЯ BURST: Вместо возврата структуры используем модификатор out
    [BurstCompile]
    public static void Get3DCoords(int linearIndex, out int3 coords)
    {
        coords = new int3(
            linearIndex & 31,
            (linearIndex >> 5) & 31,
            (linearIndex >> 10) & 31
        );
    }
}
