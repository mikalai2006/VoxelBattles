using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

//// 1. Структура вершины (Остается без изменений)
//[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
//public struct VoxelVertex
//{
//    public float3 Position;
//    public uint Color;

//    public VoxelVertex(float3 position, uint color)
//    {
//        Position = position;
//        Color = color;
//    }
//}

// 2. Сетевой маркер Корня Модели (Root Entity)
// Этот компонент вешается на главный корневой объект модели.
[GhostComponent]
public struct GhostModelRootData : IComponentData
{
    [GhostField] public int RuntimeModelId; // ID модели для инспектора/кэша
    [GhostField] public float VoxelSize;
}

// 4. Локальный рантайм-компонент на Корне Модели (Хранилище карты чанков)
// Netcode его НЕ передает. Сервер заполняет его при спавне, а Клиент — по мере прилета чанков.
public struct VoxelModelRuntimeState : IComponentData
{
    // Ваша карта существующих чанков. Теперь она безопасно живет в локальном стейте корня!
    public NativeParallelHashMap<int3, ChunkSparseData> SparseChunks;
    public bool IsDirty;
}

// Данные ячейки вашей карты чанков
public struct ChunkSparseData
{
    public Entity ChunkEntity; // Ссылка на сущность конкретного чанка
    public int VertexOffset;
    public bool IsChunkDirty;
}

// 5. Локальный рантайм-компонент Физики Чанка (Ваши unmanaged-списки)
// Эти данные уникальны для каждого чанка и рассчитываются локально (не для сети!)
public struct VoxelChunkPhysicsState : IComponentData
{
    // Список боксов для CompoundCollider чанка 32^3
    public NativeList<BlobAssetReference<Unity.Physics.Collider>> ActiveChildColliders;
    public BlobAssetReference<Unity.Physics.Collider> CurrentRootCollider;

    public PhysicChunkMode PhysicsMode;
    public CollisionFilter CollisionFilter;
    public float Mass;
    public bool IsDirty;
}

// Компоненты-переключатели активности (Остаются без изменений, поддерживают IEnableableComponent)
public struct ChunkActiveState : IComponentData, IEnableableComponent { }
public struct ChunkPhysicsActiveState : IComponentData, IEnableableComponent { }

public enum PhysicChunkMode : byte
{
    Static,
    Dynamic,
    Trigger
}


//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Physics;

//// 1. Компактная структура вершины меша (16 байт)
//[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
//public struct VoxelVertex
//{
//    public float3 Position;
//    public uint Color; // Упакованный RGBA32 (0xRRGGBBAA)

//    public VoxelVertex(float3 position, uint color)
//    {
//        Position = position;
//        Color = color;
//    }
//}

//// 2. Управляемый контейнер для хранения ссылки на меш болванки
//public class ChunkMeshComponent : IComponentData
//{
//    public UnityEngine.Mesh Mesh;
//}

//// 3. Компоненты-переключатели активности (Битовые маски)
//public struct ChunkActiveState : IComponentData, IEnableableComponent { }
//public struct ChunkPhysicsActiveState : IComponentData, IEnableableComponent { }

//// 4. Настройки размеров и данных текущего состояния
//public struct VoxelChunkStateComponent0 : IComponentData
//{
//    public BlobAssetReference<VoxelModelBlob> ModelBlob;
//    public bool IsDirty;

//    // Хранилище ссылок на дочерние боксы текущего составного коллайдера
//    // Это unmanaged-список, который будет жить прямо внутри ECS-памяти компонента
//    public NativeList<BlobAssetReference<Unity.Physics.Collider>> ActiveChildColliders;
//}

////// 5. Вспомогательные структуры данных Блоба
////public struct VoxelCell
////{
////    public bool IsOccupied;
////    public Color Color;
////}

////public struct VoxelModelBlob
////{
////    public int3 Bounds;
////    public float SizeVoxel;
////    public float3 Pivot;
////    public BlobArray<VoxelCell> Grid;
////}

//public struct ChunkSparseData
//{
//    public BlobAssetReference<VoxelModelBlob> ModelBlob;
//    public int VertexOffset; // Используется системой мешей для сборки

//    // AAA: Индивидуальный флаг чанка для распределения расчетов по кадрам
//    public bool IsChunkDirty;
//}

//public struct VoxelChunkStateComponent : IComponentData
//{
//    // AAA-Хранилище: Карта существующих чанков. Ключ - int3 координата сетки (например, 0,1,0)
//    public NativeParallelHashMap<int3, ChunkSparseData> SparseChunks;

//    public bool IsDirty;


//    // Трекер unmanaged-боксов для CompoundCollider
//    public NativeList<BlobAssetReference<Unity.Physics.Collider>> ActiveChildColliders;

//    //// ДОБАВЛЯЕМ СЮДА: Ссылка на текущий монолитный коллайдер чанка
//    //public BlobAssetReference<Collider> CurrentRootCollider;

//    // Храним текущий режим физики для этого чанка
//    public PhysicChunkMode PhysicsMode;

//    // Filter collision
//    public CollisionFilter CollisionFilter;

//    // Масса сущности
//    public float Mass;
//}

//public enum PhysicChunkMode : byte
//{
//    Static,
//    Dynamic,
//    Trigger
//}