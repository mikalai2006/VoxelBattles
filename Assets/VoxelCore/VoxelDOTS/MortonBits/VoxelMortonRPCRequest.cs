using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// RPC-запрос от клиента к серверу: "Хочу создать модель X в точке Y"
public struct RequestSpawnModelRpc : IRpcCommand
{
    public uint ConfigHashNameBody; // Хэш модели (какую модель спавнить)
    public uint ConfigHashNameTower;
    public uint ConfigHashNameMuzzle;
    public uint ConfigHashNameWheels;
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
