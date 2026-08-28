//using System.Collections.Generic; // Важно: подключаем стандартные C# коллекции
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Transforms;
//using UnityEngine;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial class VehicleAssemblySystem : SystemBase
//{
//    private VoxelBlobCacheManager _cacheManager;
//    private PhysicsVoxelPoolSystem _poolPhysicsSystem;

//    private struct AssemblyJobData
//    {
//        public VehiclePresetAsset Preset;
//        public float3 SpawnPosition;
//        public quaternion SpawnRotation;
//        public bool IsDynamic;
//        public bool isAddMoved;
//    }

//    protected override void OnCreate()
//    {
//        _poolPhysicsSystem = World.GetOrCreateSystemManaged<PhysicsVoxelPoolSystem>();

//        // Система заблокирует свое выполнение (OnUpdate), 
//        // пока SubScene с компонентом PhysicsStep полностью не загрузится в память
//        // Запрещаем системе обновляться, пока PhysicsStep с подсцены не загружен в память
//        RequireForUpdate<PhysicsStep>();
//    }

//    protected override void OnUpdate()
//    {
//        if (_cacheManager == null)
//        {
//            _cacheManager = Object.FindAnyObjectByType<VoxelBlobCacheManager>();
//            if (_cacheManager == null) return;
//        }

//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        // ИСПРАВЛЕНО: Используем стандартный C# List вместо NativeList. 
//        // Теперь хранение ScriptableObject внутри легально и не вызывает ошибок типов.
//        var gatheredRequests = new List<AssemblyJobData>();

//        // ---------------------------------------------------------
//        // ФАЗА 1: Чтение и фиксация данных (Архетипы заблокированы)
//        // ---------------------------------------------------------
//        foreach (var (request, requestEntity) in SystemAPI.Query<RequestVehicleAssembly>().WithEntityAccess())
//        {
//            if (request.Preset == null) continue;

//            gatheredRequests.Add(new AssemblyJobData
//            {
//                Preset = request.Preset,
//                SpawnPosition = request.SpawnPosition,
//                SpawnRotation = request.SpawnRotation,
//                IsDynamic = request.IsDynamic,
//                isAddMoved = request.isAddMove,
//            });

//            ecb.DestroyEntity(requestEntity);
//        }

//        // Выполняем удаление запросов, освобождая итератор Query
//        ecb.Playback(EntityManager);
//        ecb.Dispose();

//        // Если запросов на сборку нет — выходим
//        if (gatheredRequests.Count == 0) return;

//        var em = EntityManager;

//        // ---------------------------------------------------------
//        // ФАЗА 2: Безопасный спавн деталей (Архетипы свободны)
//        // ---------------------------------------------------------
//        CollisionFilter collisionFilter = new CollisionFilter
//        {
//            BelongsTo = 1 << 1,
//            // Луч реагирует ТОЛЬКО на 0-ю категорию (Земля). Он физически "не заметит" колесо!
//            CollidesWith = (1u << 0) | (1u << 1),
//            GroupIndex = 0
//        };

//        for (int i = 0; i < gatheredRequests.Count; i++)
//        {
//            AssemblyJobData req = gatheredRequests[i];
//            VehiclePresetAsset preset = req.Preset;

//            // 1. СПАВН ШАССИ
//            SparseBakedModelResult chassisBlob = _cacheManager.GetOrCreateSparseModel(preset.chassis.meshConfig.sOVoxelData);
//            PhysicChunkMode chassisMode = req.IsDynamic ? PhysicChunkMode.Dynamic : PhysicChunkMode.Static;
//            float3 localPositionChassis = req.SpawnPosition + new float3(0, preset.chassis.meshConfig.sOVoxelData.Pivot.y, 0);

//            Entity chassisEntity = _poolPhysicsSystem.SpawnEntity(
//                localPositionChassis,
//                chassisBlob.ChunkCoords,
//                chassisBlob.ChunkBlobs,
//                new VoxelChunkStateComponent()
//                {
//                    PhysicsMode = chassisMode,
//                    CollisionFilter = collisionFilter,
//                    Mass = 2000f
//                }
//            );

//            if (req.isAddMoved)
//            {
//                em.AddComponent<IsControlledTag>(chassisEntity);
//            }
//            em.AddComponentData(chassisEntity, new AAA_MovementComponent
//            {
//                Acceleration = 10f,
//                CurrentVelocity = 0f,
//                Deceleration = 10f,
//                MaxSpeed = preset.wheelsPreset.moveSpeed,
//            });

//            ////===================Настройка массы========================
//            //// 1. Получаем коллайдер тела для расчета свойств инерции
//            //var collider = EntityManager.GetComponentData<PhysicsCollider>(chassisEntity);

//            //// 2. Устанавливаем желаемое значение массы (2000 кг для шасси)
//            //float newMass = 2000f;

//            //// 3. Создаем новую структуру массы с правильным тензором инерции 
//            //PhysicsMass updatedMass = PhysicsMass.CreateDynamic(collider.MassProperties, newMass);

//            //// 4. Применяем обновленные данные к Entity через SetComponentData
//            //EntityManager.SetComponentData(chassisEntity, updatedMass);
//            //===========================================================


//            em.AddComponent<VehicleRootTag>(chassisEntity);
//            em.SetComponentData(chassisEntity, LocalTransform.FromPositionRotation(localPositionChassis, req.SpawnRotation));

//            float4x4 chassisMatrix = float4x4.TRS(localPositionChassis, req.SpawnRotation, new float3(1f));

//            Entity towerEntity = Entity.Null;
//            float4x4 towerMatrix = chassisMatrix;

//            // 2. СПАВН БАШНИ (Уровень 0)
//            if (preset.tower != null)
//            {
//                float3 towerLocalPos = (float3)preset.tower.baseOffset + new float3(0, preset.chassis.meshConfig.sOVoxelData.Bounds.y, 0);
//                float3 towerWorldPos = math.transform(chassisMatrix, towerLocalPos);

//                SparseBakedModelResult towerBlob = _cacheManager.GetOrCreateSparseModel(preset.tower.meshConfig.sOVoxelData);
//                towerEntity = _poolPhysicsSystem.SpawnEntity(
//                    towerWorldPos,
//                    towerBlob.ChunkCoords,
//                    towerBlob.ChunkBlobs,
//                    new VoxelChunkStateComponent
//                    {
//                        PhysicsMode = PhysicChunkMode.Trigger,
//                        CollisionFilter = collisionFilter,
//                    }
//                );

//                SetupFlatChildDirect(em, chassisEntity, towerEntity, towerLocalPos, towerWorldPos, 0);

//                towerMatrix = float4x4.TRS(towerWorldPos, req.SpawnRotation, new float3(1f));
//            }

//            // 3. СПАВН ДУЛА (Уровень 1)
//            if (preset.muzzle != null)
//            {
//                Entity muzzleParent = (towerEntity != Entity.Null) ? towerEntity : chassisEntity;
//                int hierarchyLevel = (towerEntity != Entity.Null) ? 1 : 0;

//                float3 muzzleLocalPos = (float3)preset.muzzle.baseOffset;
//                float3 muzzleWorldPos = math.transform(towerMatrix, muzzleLocalPos);

//                SparseBakedModelResult muzzleBlob = _cacheManager.GetOrCreateSparseModel(preset.muzzle.meshConfig.sOVoxelData);
//                Entity muzzleEntity = _poolPhysicsSystem.SpawnEntity(
//                    muzzleWorldPos,
//                    muzzleBlob.ChunkCoords,
//                    muzzleBlob.ChunkBlobs,
//                    new VoxelChunkStateComponent
//                    {
//                        PhysicsMode = PhysicChunkMode.Trigger,
//                        CollisionFilter = collisionFilter,
//                    }
//                );

//                SetupFlatChildDirect(em, muzzleParent, muzzleEntity, muzzleLocalPos, muzzleWorldPos, hierarchyLevel);
//            }

//            // 4. СПАВН КОЛЕС (Уровень 0)
//            if (preset.wheelsPreset != null)
//            {
//                foreach (var wheelSlot in preset.wheelsPreset.wheelSlots)
//                {
//                    if (wheelSlot.wheelPartAsset == null) continue;

//                    float3 wheelLocalPos = new float3(wheelSlot.offsetInVoxels.x > 0 ? 9 : -9, 0, 0) - new float3(0, preset.chassis.meshConfig.sOVoxelData.Pivot.y, 0) + (float3)wheelSlot.offsetInVoxels + (float3)wheelSlot.wheelPartAsset.baseOffset;
//                    float3 wheelWorldPos = math.transform(chassisMatrix, wheelLocalPos);

//                    SparseBakedModelResult wheelBlob = _cacheManager.GetOrCreateSparseModel(wheelSlot.wheelPartAsset.meshConfig.sOVoxelData);
//                    Entity wheelEntity = _poolPhysicsSystem.SpawnEntity(
//                        wheelWorldPos,
//                        wheelBlob.ChunkCoords,
//                        wheelBlob.ChunkBlobs,
//                        new VoxelChunkStateComponent
//                        {
//                            PhysicsMode = PhysicChunkMode.Trigger,
//                            CollisionFilter = collisionFilter,
//                        }
//                    );

//                    SetupFlatChildDirect(em, chassisEntity, wheelEntity, wheelLocalPos, wheelWorldPos, 0);

//                    // Добавляем настройки подвески для каждого колеса
//                    em.AddComponentData(wheelEntity, new VehicleSuspensionComponent
//                    {
//                        RideHeight = wheelSlot.wheelPartAsset.meshConfig.sOVoxelData.Pivot.y + 2f,      // ИСПРАВЛЕНО: Радиус колеса 5.5м + 0.7м на ход подвески корпуса
//                        Frequency = 1.0f,       // Частота колебаний (для огромного танка 2.0f — отличная физика)
//                        DampingRatio = 5.0f,    // Амортизатор, полностью исключающий прыжки
//                        WheelRadius = wheelSlot.wheelPartAsset.meshConfig.sOVoxelData.Pivot.y
//                    });

//                    // ДОБАВЛЯЕМ НОВЫЙ КОМПОНЕНТ ДЛЯ ВРАЩЕНИЯ:
//                    // Определяем, переднее это колесо или заднее (например, по локальной координате Z)
//                    bool isFrontWheel = wheelLocalPos.z > 0f;

//                    if (wheelSlot.isRotatable)
//                    {
//                        em.AddComponentData(wheelEntity, new WheelVisualRotation
//                        {
//                            SpinAngle = 0f,
//                            SteerAngle = 0f,
//                            IsSteerable = isFrontWheel // Передние колёса будут рулить, задние — нет
//                        });
//                    }

//                    em.AddComponent<SuspensionForceComponent>(wheelEntity);
//                }
//            }
//        }
//    }

//    private static void SetupFlatChildDirect(EntityManager em, Entity parent, Entity child, float3 localOffset, float3 initialWorldPos, int level)
//    {
//        em.AddComponentData(child, new VehicleFlatChildData
//        {
//            ParentEntity = parent,
//            LocalOffset = localOffset,
//            HierarchyLevel = level
//        });

//        em.SetComponentData(child, LocalTransform.FromPosition(initialWorldPos));
//    }
//}
