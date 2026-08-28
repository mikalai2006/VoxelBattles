using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

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
    // Безопасный ECS-список для хранения дочерних коллайдеров воксельных чанков.
    // Вмещает до 15 чанков на одну машину (если чанков больше, используйте FixedList512Bytes)
    //public FixedList128Bytes<BlobAssetReference<Unity.Physics.Collider>> ChildBlobs;
}

public struct VoxelChildColliderRegistrySingleton : IComponentData
{
    // Синглтон просто хранит ссылки на нативные контейнеры, 
    // которые выделены в неуправляемой куче (Persistent)
    public NativeParallelHashMap<Entity, NativeArray<BlobAssetReference<Unity.Physics.Collider>>> Registry;
    public NativeList<BlobAssetReference<Collider>> DisposeList;
}

///// <summary>
///// Тег пометки сущностей, которые прошли 
///// </summary>
//public struct TagIsHierarchyCompleted : IComponentData { }