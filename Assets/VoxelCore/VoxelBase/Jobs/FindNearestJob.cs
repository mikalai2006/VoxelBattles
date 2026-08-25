using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [BurstCompile]
    struct FindNearestJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> points;
        [ReadOnly] public float3 targetPosition;
        public NativeArray<float> distances;

        public void Execute(int index)
        {
            // Вычисляем квадрат расстояния (быстрее, чем Vector3.Distance)
            distances[index] = Vector3.SqrMagnitude(points[index] - targetPosition);
        }
    }
}