
// Сама структура Job для Unity 6
using Unity.Jobs;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [Unity.Burst.BurstCompile]
    public struct BakeMeshJob : IJob
    {
        public int meshId;
        public MeshColliderCookingOptions cookingOptions;

        public void Execute()
        {
            // Этот метод вызывается внутри рабочего потока (Worker Thread).
            // Он подготавливает данные PhysX в фоне.
            //Physics.BakeMesh(meshId, false, cookingOptions);
        }
    }
}