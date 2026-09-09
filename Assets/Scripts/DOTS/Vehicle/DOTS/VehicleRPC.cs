using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// RPC команда от Клиента к Серверу
//public struct RequestVehicleSpawnRpc : IRpcCommand
//{
//    public uint PresetId;         // ID вашего ScriptableObject пресета
//    public float3 SpawnPosition;  // Координаты спавна
//    public bool IsAddMove;       // Флаг движения
//    public bool IsDynamic;       // Флаг динамики
//}

public struct WheelPresetData
{
    public uint HashName;
    public float3 Offset;
    public bool IsRotatable;
}
public struct WheelsData
{
    public uint HashName;
    public FixedList128Bytes<WheelPresetData> WheelsSlots;
    public float MoveSpeed;
    public float RotationSpeed;
}
public struct MuzzleData
{
    public uint HashName;
    public float3 Offset;
}

public struct TowerData
{
    public uint HashName;
    public float3 Offset;
    //public FixedList64Bytes<float3> MuzzlesSlots;
    public FixedList128Bytes<MuzzleData> muzzlesData;
}

public struct BodyData
{
    public uint HashName;
    public float3 Offset;
    public uint Mass;
}


// Легковесный unmanaged компонент для передачи данных из UI в ECS-систему клиента
public struct SpawnVehicleIntent : IComponentData
{
    public BodyData bodyData;
    public TowerData towerData;
    public WheelsData wheelsData;
    public float3 SpawnPosition;
    public quaternion SpawnRotation; // Поворот объекта
    public bool IsAddMove;
    public bool IsDynamic;
}

public struct RequestSpawnVehicleRpc : IRpcCommand
{
    public BodyData bodyData;
    public TowerData towerData;
    public WheelsData wheelsData;
    public float3 SpawnPosition; // Где заспавнить в мире
    public quaternion SpawnRotation; // Поворот объекта
}

/// <summary>
/// Данные спавна чанков модели.
/// </summary>
public struct ChunkData
{
    public byte ParentNodeId;
    public uint HashName;
    public float3 Offset;
}

/// <summary>
/// Данные спавна узла модели.
/// </summary>
public struct NodeData
{
    public float3 Offset;
    public byte NodeId;
    public byte TypeCollider; // 1 - динамика, 0 - статика
    public uint ParentTargetEntity;
}

/// <summary>
/// Данные для спавна модели.
/// </summary>
public struct RequestServerSpawnModelRpc1 : IRpcCommand
{
    public FixedList512Bytes<NodeData> nodes;
    public FixedList512Bytes<ChunkData> chunks;
    public float3 SpawnPosition; // Где заспавнить в мире
    public quaternion SpawnRotation; // Поворот объекта
}