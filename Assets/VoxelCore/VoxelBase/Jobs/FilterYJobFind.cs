using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Mikalai2006.VoxelBase
{
    [BurstCompile]
    public struct FilterYJobFind : IJob
    {
        [ReadOnly] public NativeArray<Voxel> data; // The array you want to filter based on
        public int y; // Your filtering condition
        public NativeArray<bool> found; // To store the result

        public void Execute()
        {
            found[0] = false; // Initialize to not found

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].position.y == y && data[i].type != VoxelType.Destroyed && data[i].type != VoxelType.Air)
                {
                    found[0] = true;
                    break; // Found the value, no need to continue
                }
            }
        }

        // public bool Execute(int index)
        // {
        //     Voxel voxel = dataToFilter[index];
        //     // Return true if the element at 'index' should be kept, false otherwise
        //     return voxel.position.y == y && voxel.type != VoxelType.Destroyed && voxel.type != VoxelType.Air;
        // }
    }
}