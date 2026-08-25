//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//public class VehicleSpawnTest : MonoBehaviour
//{
//    public float3 spawnPosition;

//    // Ссылки на воксельные ассеты ваших моделей
//    [Tooltip("Пресет машины, который будет запечен как шаблон для спавна")]
//    public VehiclePresetAsset VehiclePreset;

//    public void RequestTankSpawn()
//    {
//        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

//        // 1. Создаем unmanaged Билдер для Blob-ассета параметров
//        using var builder = new BlobBuilder(Allocator.Temp);
//        ref var rootArray = ref builder.ConstructRoot<BlobArray<VehiclePartSpawnParams>>();

//        // Выделяем память под 3 детали в С++ куче
//        var arrayBuilder = builder.Allocate(ref rootArray, 3);

//        // Настраиваем Деталь 1: Шасси (Корпус)
//        arrayBuilder[0] = new VehiclePartSpawnParams
//        {
//            PartType = VehiclePartType.Chassis,
//            LocalOffset = float3.zero,
//            Bounds = VehiclePreset.chassis.,
//            IsWheelRotatable = false
//        };

//        // Настраиваем Деталь 2: Башня (Смещение на 0.8 метра вверх на крышу)
//        arrayBuilder[1] = new VehiclePartSpawnParams
//        {
//            PartType = VehiclePartType.Tower,
//            LocalOffset = new float3(0f, 0.8f, 0f),
//            Bounds = new int3(8, 6, 12),
//            IsWheelRotatable = false
//        };

//        // Настраиваем Деталь 3: Орудие (Смещение вперед относительно башни)
//        arrayBuilder[2] = new VehiclePartSpawnParams
//        {
//            PartType = VehiclePartType.Muzzle,
//            LocalOffset = new float3(0f, 0.8f, 1.2f),
//            Bounds = new int3(2, 2, 16),
//            IsWheelRotatable = false
//        };

//        // Запекаем BlobAssetReference
//        BlobAssetReference<BlobArray<VehiclePartSpawnParams>> blobRef = builder.CreateBlobAssetReference<BlobArray<VehiclePartSpawnParams>>(Allocator.Persistent);

//        // 2. Создаем сущность-запрос в ECS-мире
//        Entity requestEntity = entityManager.CreateEntity();

//        entityManager.AddComponentData(requestEntity, new SpawnVehicleRequest
//        {
//            SpawnTransform = LocalTransform.FromPosition(spawnPosition),
//            MoveSpeed = 12f,
//            RotationSpeed = 45f,
//            TowerRotationSpeed = 60f,
//            VehiclePartsBlob = blobRef // Передаем запеченный параметрический массив
//        });
//    }
//}
