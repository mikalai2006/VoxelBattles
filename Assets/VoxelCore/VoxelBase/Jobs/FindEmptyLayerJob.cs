using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Mikalai2006.VoxelBase
{
    [BurstCompile]
    public struct FindEmptyLayerJob : IJob
    {
        [ReadOnly] public NativeArray<Voxel> voxels;
        public int sizeX;
        public int sizeY;
        public int sizeZ;

        // Сюда запишем Y-индекс первого полностью пустого слоя
        // Если пустых слоев нет, внутри останется значение по умолчанию (например, -1)
        public NativeArray<int> outCollapseY;

        public void Execute()
        {
            outCollapseY[0] = -1; // Сброс результата

            // Идем снизу вверх по уровням Y
            for (int y = 0; y < sizeY; y++)
            {
                bool layerHasVoxels = false;

                // Сканируем всю горизонтальную плоскость XZ на текущем уровне Y
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int index = x + (y * sizeX) + (z * sizeX * sizeY);
                        Voxel voxel = voxels[index];

                        // Если нашли хотя бы один существующий блок, слой НЕ пустой
                        if (voxel.type != VoxelType.Air && voxel.type != VoxelType.Destroyed)
                        {
                            layerHasVoxels = true;
                            break;
                        }
                    }
                    if (layerHasVoxels) break;
                }

                // Если мы проверили весь слой XZ и не нашли ни одного вокселя...
                if (!layerHasVoxels)
                {
                    // Запоминаем этот уровень Y: всё, что выше него, должно осыпаться!
                    outCollapseY[0] = y;
                    return;
                }
            }
        }
    }
}
