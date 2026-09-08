using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;


// Данные о конкретной пуле внутри сетевого пакета
public struct BulletSpawnInfo
{
    public float3 Position;
    public float3 Direction;
}

// Сетевой RPC-пакет выстрела
public struct ShootEventRPC : IRpcCommand
{
    public uint ConfigHashName; // Хэш ScriptableObject конфигурации
    public FixedList64Bytes<BulletSpawnInfo> SpawnedBullets; // Пачка выстрелов за тик
    public Entity ShooterEntity; // Кто выстрелил
}

// Компонент данных летящей пули (используется и на сервере, и на клиенте)
public struct BulletData : IComponentData
{
    public uint ConfigHashName;
    public float3 Direction;
    public float Speed;
    public Entity Shooter;
    public int RadiusExplode;
}

public struct Lifetime : IComponentData
{
    public float Value;
}

// Теги-маркеры для систем
public struct DestroyTag : IComponentData { }