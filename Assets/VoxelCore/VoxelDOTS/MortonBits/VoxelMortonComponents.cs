using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;


public struct VisualsReplyMaskTag : IComponentData { }
public struct ChunkMeshNeedCreate : IComponentData, IEnableableComponent { }
public struct ChunkMeshNeedApply : IComponentData, IEnableableComponent { }

// Хэш модели, чтобы заглянуть в GlobalVoxelModelCache
public struct VoxelModelHeader : IComponentData
{
    // Этот атрибут говорит Netcode: "Отправь это поле клиенту ровно ОДИН РАЗ при спавне!"
    // Так как значение не меняется, в игровых кадрах трафик будет равен строго 0 байт.
    [GhostField] public uint ConfigHashName;
}

// Трехмерный индекс этого чанка (например: 0, 1, 0)
public struct ChunkIndexComponent : IComponentData
{
    [GhostField] public int3 Value;
}

// Этот компонент теперь корректно сериализуется Netcode
public struct NetworkParent : IComponentData
{
    // Netcode автоматически переведет локальный Entity сервера в сетевой ID,
    // а на клиенте превратит его в локальный Entity клиента.
    //[GhostField] public GhostInstance Value;

    // Будем передавать только чистый сетевой ID родителя
    [GhostField] public uint ParentGhostId;
}

// Вешается на Узел (Sub-Root). Хранит локальную Entity Корня.
public struct PendingNodeToRoot : IComponentData
{
    public Entity LocalRootEntity;
}

// Вешается на Чанк. Хранит локальную Entity Узла.
public struct PendingChunkToNode : IComponentData
{
    public Entity LocalNodeEntity;
}

// Тег корневой сущности воксельного объекта
public struct VoxelModelRootTag : IComponentData { }
//public struct NeedsMeshRebuildTag : IComponentData { }
public struct VoxelChunkLinkedTag : IComponentData { }

// Настройки модели на корневом объекте
public struct VoxelModelRootData : IComponentData
{
    // ААА-ФИКС: Помечаем поле для Netcode. 
    // Сервер отправит его клиенту ОДИН РАЗ при спавне. Трафик в рантайме = 0!
    [GhostField] public uint ConfigHashName;
}

// ПЕРЕИМЕНОВАНО: Чисто локальный unmanaged-буфер маски целостности чанка. 
// Он НЕ реплицируется по сети, обеспечивая нулевой оверхед на сетевой спавн!
[InternalBufferCapacity(0)]
public struct LocalChunkDestructionMask : IBufferElementData
{
    //[GhostField(Quantization = 0)]
    public ulong Value;
}
// Трекер прогресса загрузки маски чанка
public struct ChunkSyncTracker : IComponentData
{
    // Каждый бит отвечает за свой кусок: 
    // кусок 0 = 1 (0001), кусок 1 = 2 (0010), кусок 2 = 4 (0100), кусок 3 = 8 (1000)
    public byte ReceivedChunksBitmask;
}


// Рантайм-метаданные чанка для Presentation-конвейера
//public struct ChunkIndexComponent : IComponentData { public int3 Value; }
//public struct VoxelModelHeader : IComponentData { public uint ConfigHashName; }

//// Серверный маркер для Фазы 2 спавна
//public struct ServerAwaitingChunksSpawnTag : IComponentData { }

//// 100% Unmanaged синглтон для Burst-систем клиента
//public struct ClientMaterialCacheComponent : IComponentData
//{
//    public BatchMaterialID OpaqueMaterialID;
//}

//// Локальный компонент для отслеживания состояния рендеринга на клиенте
//public struct ClientRenderState : IComponentData
//{
//    public uint LastProcessedVersion;
//    public bool NeedsMeshRebuild;

//    // Кэшируем постоянную unmanaged-ссылку на Mesh-болванку, 
//    // чтобы просто перезаписывать её через WritableMeshData без аллокаций
//    public int CachedRenderMeshIndex;
//    public bool IsMeshInitialized;
//}

// Маркерный компонент (должен быть ICleanupComponentData, чтобы не удаляться вместе с сущностью!)
//[UnityEngine.HideInInspector]
public struct VoxelColliderCleanupMarker //: IComponentData //, ICleanupComponentData
{
    // Обязательно ICleanupComponentData! 
    // Когда вы вызовите DestroyEntity(vehicle), все обычные компоненты удалятся,
    // но сущность ОСТАНЕТСЯ в памяти как "призрак" только с этим компонентом,
    // пока мы вручную её не задиспозим и не снимем маркер.
    public BlobAssetReference<Unity.Physics.Collider> ColliderBlob;
    //public NativeArray<int3> SafeStatus;
    // Безопасный ECS-список для хранения дочерних коллайдеров воксельных чанков.
    // Вмещает до 15 чанков на одну машину (если чанков больше, используйте FixedList512Bytes)
    //public FixedList128Bytes<BlobAssetReference<Unity.Physics.Collider>> ChildBlobs;
}


//[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
public struct ChunkColliderData //: IComponentData //, IEnableableComponent
{
    public NativeArray<int3> SafeStatus;
    //public NativeArray<BlobAssetReference<Unity.Physics.Collider>> SafeColliderBlob;
    public NativeArray<BoxGeometry> GeometryArray;

    public Entity RootVehicleEntity;
    public float3 LocalOffsetWithPivot;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;
    //public int3 index;
}

public struct VoxelChildColliderRegistrySingleton : IComponentData
{
    // Синглтон просто хранит ссылки на нативные контейнеры, 
    // которые выделены в неуправляемой куче (Persistent)
    public NativeParallelHashMap<Entity, ChunkColliderData> Registry;
    public NativeList<BlobAssetReference<Collider>> DisposeList;
}

//[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
public struct ChunkMeshData //: IComponentData // , IEnableableComponent
{
    // Храним чистые, изолированные C++ массивы геометрии ТОЛЬКО этого чанка!
    public NativeArray<VoxelVertex> SafeVertices;
    public NativeArray<int> SafeIndices;
    public NativeArray<int3> SafeStatus;

    //// Ссылка на хэндл джобы для точечной проверки готовности
    //public JobHandle LastBakingJobHandle;
    public Entity RootVehicleEntity;
    public float3 LocalOffsetWithPivot;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;
    public int3 index;
}

public struct VoxelMeshDataRegistrySingleton : IComponentData
{
    public NativeParallelHashMap<Entity, ChunkMeshData> Registry;
}

///// <summary>
///// Тег пометки сущностей, которые прошли 
///// </summary>
//public struct TagIsHierarchyCompleted : IComponentData { }