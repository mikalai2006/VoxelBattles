using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Mikalai2006.VoxelBase
{
    
[BurstCompile]
    struct CheckCollisionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> points;
        [ReadOnly] public float3 _pointCollision;
        [ReadOnly] public float _radiusExplode;
        [WriteOnly]public NativeArray<float3> _needCreateElements;
        [WriteOnly]public NativeArray<float3> needRemoveElements;
        public int maxRadius;

        public void Execute(int index)
        {
            float3 point = points[index];
            if (HelperVoxel.IsInsideSphere(point, _pointCollision, _radiusExplode))
            {
                // list.Add(posx, data.ElementAt(j).Value);
                // // data[posx] = new Voxel()
                // // {
                // //     ID = 0,
                // // };
                // data.Remove(posx);
                needRemoveElements[index] = point;

                if (HelperVoxel.IsInsideSphere(point, _pointCollision, Math.Max(4, Math.Min(_radiusExplode / 2, maxRadius))))
                {
                    _needCreateElements[index] = point;
                }
            }
            else
            {
                _needCreateElements[index] = float3.zero;
                needRemoveElements[index] = float3.zero;
            }
        }
    }
}