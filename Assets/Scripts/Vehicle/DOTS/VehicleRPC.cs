using Unity.Entities;
using Unity.Mathematics;

// RPC команда от Клиента к Серверу
//public struct RequestVehicleSpawnRpc : IRpcCommand
//{
//    public uint PresetId;         // ID вашего ScriptableObject пресета
//    public float3 SpawnPosition;  // Координаты спавна
//    public bool IsAddMove;       // Флаг движения
//    public bool IsDynamic;       // Флаг динамики
//}

// Легковесный unmanaged компонент для передачи данных из UI в ECS-систему клиента
public struct SpawnVehicleIntent : IComponentData
{
    public uint PresetId;
    public float3 SpawnPosition;
    public quaternion SpawnRotation; // Поворот объекта
    public bool IsAddMove;
    public bool IsDynamic;
}