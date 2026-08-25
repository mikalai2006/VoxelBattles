using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// RPC-запрос от клиента к серверу: "Хочу создать модель X в точке Y"
public struct RequestSpawnModelRpc : IRpcCommand
{
    public uint ConfigHashName; // Хэш модели (какую модель спавнить)
    public float3 SpawnPosition; // Где заспавнить в мире
    public quaternion SpawnRotation; // Поворот объекта
}

// Компонент-маркер на корневой сущности объекта (и на сервере, и на клиенте)
public struct VoxelModelRoot : IComponentData
{
    public uint ConfigHashName;
}

// Временный маркер сервера для Фазы 2 двухфазного спавна чанков
//public struct ServerAwaitingChunksSpawnTag : IComponentData { }
