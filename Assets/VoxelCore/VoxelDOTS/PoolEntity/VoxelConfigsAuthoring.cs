using Unity.Entities;
using UnityEngine;

// Синглтон ссылок на префабы
public struct VoxelPrefabConfig : IComponentData
{
    public Entity BulletPrefab;
}


// Синглтон ссылок на сетевые префабы
public struct VoxelGhostPrefabConfig : IComponentData
{
    public Entity RootGhostPrefab;
    public Entity ChunkGhostPrefab;
}

// Настройки для расчетов физики на сервере и клиенте
public struct VoxelServerConfigComponent : IComponentData
{
    public float SizeVoxel;
    public BlobAssetReference<Unity.Physics.Collider> EmptyCollider;
    public Unity.Physics.Material PhysicsMaterial;
    public Unity.Physics.Material TriggerMaterial;
}

#if !UNITY_SERVER
// Managed-класс для графики (Компилируется ТОЛЬКО на клиенте и в редакторе)
public class VoxelGlobalConfigComponent : IComponentData
{
    public UnityEngine.Material OpaqueMaterial;
    public UnityEngine.Material TransparentMaterial;

    public UnityEngine.Rendering.BatchMaterialID OpaqueMaterialRuntimeID;
    public UnityEngine.Rendering.BatchMaterialID TransparentMaterialRuntimeID;
    public UnityEngine.Rendering.BatchMeshID EmptyMeshID;

    public BlobAssetReference<Unity.Physics.Collider> EmptyCollider;
    public Unity.Physics.Material PhysicsMaterial;
    public Unity.Physics.Material TriggerMaterial;

    public bool PoolIsPrewarmed;
    public float SizeVoxel = 1.0f;
}
#endif

public class VoxelConfigsAuthoring : MonoBehaviour
{
    [Header("Графика и Рендеринг (Только Клиент)")]
    public Material opaqueMaterial;
    public Material transparentMaterial;

    [Header("Физика и Масштаб (Сервер и Клиент)")]
    public float sizeVoxel = 1.0f;

    [Header("Префабы")]
    public GameObject prefabBullet;

    [Header("Новые Сетевые Префабы (Архитектура Масок)")]
    public GameObject rootGhostPrefab;
    public GameObject chunkPrefab;

    public class Baker : Baker<VoxelConfigsAuthoring>
    {
        public override void Bake(VoxelConfigsAuthoring authoring)
        {
            // Создаем сущность-синглтон глобального контекста настроек вокселей
            Entity entity = GetEntity(TransformUsageFlags.None);

#if !UNITY_SERVER
            // 1. Запекаем managed-класс графики ТОЛЬКО для клиента
            AddComponentObject(entity, new VoxelGlobalConfigComponent
            {
                OpaqueMaterial = authoring.opaqueMaterial,
                TransparentMaterial = authoring.transparentMaterial,
                SizeVoxel = authoring.sizeVoxel,
                PoolIsPrewarmed = false
            });
#endif

            // 2. Запекаем unmanaged-структуру для математики сервера и Burst-потоков
            AddComponent(entity, new VoxelServerConfigComponent
            {
                SizeVoxel = authoring.sizeVoxel,
                EmptyCollider = default,
                PhysicsMaterial = default,
                TriggerMaterial = default
            });

            // 3. Конвертируем префабы из инспектора в ECS-сущности префабов
            if (authoring.rootGhostPrefab != null && authoring.chunkPrefab != null)
            {
                Entity rootPrefabEntity = GetEntity(authoring.rootGhostPrefab, TransformUsageFlags.Dynamic);
                Entity chunkPrefabEntity = GetEntity(authoring.chunkPrefab, TransformUsageFlags.Dynamic);
                Entity bulletPrefabEntity = GetEntity(authoring.prefabBullet, TransformUsageFlags.Dynamic);

                // Записываем их в чистый синглтон VoxelGhostPrefabConfig
                AddComponent(entity, new VoxelGhostPrefabConfig
                {
                    RootGhostPrefab = rootPrefabEntity,
                    ChunkGhostPrefab = chunkPrefabEntity
                });
                // Записываем их в чистый синглтон VoxelGhostPrefabConfig
                AddComponent(entity, new VoxelPrefabConfig
                {
                    BulletPrefab = bulletPrefabEntity
                });
            }
            else
            {
                Debug.LogWarning("[Voxel System]: Ошибка запекания! Префаб корня или чанка не назначен в VoxelConfigsAuthoring.");
            }
        }
    }
}
