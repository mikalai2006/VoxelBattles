//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;

//public struct ChunkBakingState : ICleanupComponentData
//{
//    public JobHandle BakingJobHandle;

//    // Персональные C++ буферы геометрии чанка
//    public NativeArray<VoxelVertex> Vertices;
//    public NativeArray<int> Indices;
//    public NativeArray<int2> Counter;

//    // ИСПРАВЛЕНИЕ: Поле для прямого безопасного чтения long-указателя!
//    public BlobAssetReference<Unity.Physics.Collider> ColliderBlob;

//    // СКРЫТОЕ ПОЛЕ: Храним сам NativeArray кадра для Burst-джобы,
//    // чтобы чисто деаллоцировать его на следующем кадре симуляции!
//    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> _internalArrayRef;
//}


using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;


[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
public struct ChunkGraphicsFlushTag : IComponentData, IEnableableComponent
{
    // ЖЕЛЕЗОБЕТОННЫЙ ПАРАЛЛЕЛЬНЫЙ ПАЙПЛАЙН:
    // Храним чистые, изолированные C++ массивы геометрии ТОЛЬКО этого чанка!
    public NativeArray<VoxelVertex> SafeVertices;
    public NativeArray<int> SafeIndices;
    public NativeArray<int2> SafeCounter;
    public NativeArray<BlobAssetReference<Collider>> SafeColliderBlob;
    //public NativeList<BakedBoxData> BakedBoxes;
    //public BlobAssetReference<Collider> SafeColliderBlob;

    // Ссылка на хэндл джобы для точечной проверки готовности
    public JobHandle LastBakingJobHandle;

    public Entity RootVehicleEntity;
    public float3 LocalOffsetWithPivot;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;
    public int3 index;

}


// Буфер для хранения вершин чанка прямо на его Entity
[InternalBufferCapacity(0)] // Capacity 0, так как вокселей много и память выделится в куче
public struct ChunkVertexElement : IBufferElementData
{
    public VoxelVertex Value;
}

// Буфер для хранения индексов чанка прямо на его Entity
[InternalBufferCapacity(0)]
public struct ChunkIndexElement : IBufferElementData
{
    public int Value;
}