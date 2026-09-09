using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

public struct RootColliderNeedApply : IComponentData, IEnableableComponent { }
public struct NewChunkColliderData
{
    public Entity TargetEntity;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // WorldSystemFilterFlags.ClientSimulation | 
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(CreateColliderSystem))]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class ApplyColliderSystem : SystemBase
{
    // Нативный реестр: Сетевая Сущность -> Её текущий Blob-коллайдер
    private NativeParallelHashMap<Entity, VoxelColliderCleanupMarker> m_ColliderRegistry;
    protected override void OnCreate()
    {
        // Выделяем память один раз при старте игры. Настройки сети не затрагиваются.
        m_ColliderRegistry = new NativeParallelHashMap<Entity, VoxelColliderCleanupMarker>(128, Allocator.Persistent);
    }


    protected override void OnDestroy()
    {
        // ГАРАНТИРОВАННАЯ ОЧИСТКА ПРИ ВЫХОДЕ ИЗ ИГРЫ (PLAY MODE)
        // Этот метод вызывается ВСЕГДА, до полной деаллокации памяти мира.
        if (m_ColliderRegistry.IsCreated)
        {
            // Извлекаем все оставшиеся в игре коллайдеры напрямую из нативной памяти
            using var values = m_ColliderRegistry.GetValueArray(Allocator.Temp);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].ColliderBlob.IsCreated)
                {
                    values[i].ColliderBlob.Dispose(); // Принудительно выгружаем каждый компаунд
                }
            }

            // Уничтожаем саму карту
            m_ColliderRegistry.Dispose();
        }

        // Обязательно вызываем базовый метод уничтожения SystemBase!
        base.OnDestroy();
        // ====================================================================
    }


    protected override void OnUpdate()
    {
        // Проверяем, валидна ли наша карта в памяти (защита от Domain Reload в редакторе)
        if (!m_ColliderRegistry.IsCreated)
        {
            UnityEngine.Debug.LogWarning($"m_ColliderRegistry IsCreated= NO!");
            return;
        }

        // ====================================================================
        // ФАЗА 1: УЛЬТРА-БЫСТРАЯ ПРОВЕРКА НАЛИЧИЯ ДАННЫХ В КАДРЕ
        // Если ни один чанк вокселей в мире не находится в статусе выпекания графики,
        // система мгновенно выходит, затрачивая ровно 0.00 мс времени Main Thread!
        // ====================================================================
        var flushQuery = SystemAPI.QueryBuilder().WithAll<RootColliderNeedApply>().Build();
        if (flushQuery.IsEmpty)
        {
            return;
        }

        // Получаем синглтон коллайдеров
        var voxelChildColliderRegistrySingleton = SystemAPI.GetSingleton<VoxelChildColliderRegistrySingleton>();


        // ====================================================================
        // ФАЗА 2: СБОРКА ЧАНКОВ, КОТОРЫЕ ПОЛНОСТЬЮ ДОПЕКЛИСЬ НА ЯДРАХ CPU
        // ====================================================================
        foreach (var (status, rootEntity) in SystemAPI.Query<
            RefRO<RootColliderNeedApply>
            >()
            .WithAll<RootColliderNeedApply>()  // Сущность попадет в выборку, только если у неё присутствует компонент и он включен (Enabled).
            .WithEntityAccess())
        {
            EntityManager.SetComponentEnabled<RootColliderNeedApply>(rootEntity, false);

            // Проверяем, есть ли у корня встроенный буфер иерархии
            if (SystemAPI.HasBuffer<LinkedEntityGroup>(rootEntity))
            {
                // 1. Получаем доступ к буферу связей
                DynamicBuffer<LinkedEntityGroup> linkedGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(rootEntity);

                // Выделяем временные контейнеры Mono-кадра для передачи в метод ExecuteManagedMeshAllocation
                var chunksDataArray = new NativeList<NewChunkColliderData>(16, Allocator.Temp);
                //var childOffsetsList = new NativeList<float3>(16, Allocator.Temp);
                //Entity rootVehicleEntity = Entity.Null;

                //// 2. Создаем лукапы для проверки компонентов на дочерних сущностях
                //var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                //var chunkDataLookup = SystemAPI.GetComponentLookup<AAA_VoxelModelHeader>(true); // Ваш компонент-маркер чанка

                // 3. Обходим буфер. Начинаем с i = 1, так как под индексом 0 лежит сам Root!
                for (int i = 1; i < linkedGroup.Length; i++)
                {
                    Entity childEntity = linkedGroup[i].Value;

                    // Нам нужно отсеять промежуточные Узлы (Sub-Roots) и взять только Чанки.
                    // Для этого проверяем наличие вашего специфичного компонента чанка:
                    if (SystemAPI.HasComponent<AAA_VoxelModelHeader>(childEntity))
                    {
                        // Ура! Это гарантированно дочерний чанк именно этой машины
                        if (SystemAPI.HasComponent<LocalTransform>(childEntity))
                        {
                            //LocalTransform chunkTransform = SystemAPI.GetComponent<LocalTransform>(childEntity);

                            //// Достаем локальную позицию для запекания в CompoundCollider
                            //float3 localPos = chunkTransform.Position;

                            //var chunkColliderData = voxelChildColliderRegistrySingleton.Registry[childEntity];

                            chunksDataArray.Add(new NewChunkColliderData
                            {
                                TargetEntity = childEntity,
                            });
                        }
                    }
                }

                int totalReadyCount = chunksDataArray.Length;
                int validCollidersCount = 0;

                for (int i = 0; i < totalReadyCount; i++)
                {
                    var dataFromSingleton = voxelChildColliderRegistrySingleton.Registry[chunksDataArray[i].TargetEntity];
                    //int3 finalCounts = dataFromSingleton.SafeStatus[0];
                    //if (finalCounts.x > 0 && dataFromSingleton.SafeColliderBlob[0].IsCreated) // && chunksDataArray[i].SafeColliderBlob.IsCreated
                    if (dataFromSingleton.GeometryArray.IsCreated)
                    {
                        validCollidersCount++;
                    }
                }

                BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

                if (validCollidersCount > 0)
                {
                    var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
                    int currentInstanceIdx = 0;

                    for (int i = 0; i < totalReadyCount; i++)
                    {
                        var dataFromSingleton = voxelChildColliderRegistrySingleton.Registry[chunksDataArray[i].TargetEntity];
                        //int3 finalCounts = dataFromSingleton.SafeStatus[0];
                        //if (finalCounts.x > 0 && dataFromSingleton.SafeColliderBlob[0].IsCreated)// && chunksDataArray[i].SafeColliderBlob.IsCreated
                        if (dataFromSingleton.GeometryArray.IsCreated)
                        {
                            // Создаем BlobAssetReference на главном потоке (быстрая операция)
                            BlobAssetReference<Unity.Physics.Collider> newColliderBlob = Unity.Physics.BoxCollider.Create(dataFromSingleton.GeometryArray[0]);

                            compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
                            {
                                Collider = newColliderBlob, // dataFromSingleton.SafeColliderBlob[0], //chunksDataArray[i].SafeColliderBlob[0],
                                CompoundFromChild = new RigidTransform(quaternion.identity, dataFromSingleton.LocalOffsetWithPivot),//childOffsetsList[i]
                                Entity = chunksDataArray[i].TargetEntity
                            };

                            currentInstanceIdx++;
                        }
                    }

                    if (compoundInstances.Length > 0)
                    {
                        finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
                    }

                    // 3. ИСПРАВЛЕНИЕ УТЕЧКИ: Проходим по массиву и уничтожаем оригинальные временные Blob-активы чанков
                    for (int i = 0; i < compoundInstances.Length; i++)
                    {
                        if (compoundInstances[i].Collider.IsCreated)
                        {
                            // Освобождаем неуправляемую память каждого дочернего кубика
                            compoundInstances[i].Collider.Dispose();
                        }
                    }
                    compoundInstances.Dispose();
                }

                try
                {
                    ExecuteManagedCollider(
                        ref this.CheckedStateRef,
                        chunksDataArray,
                        rootEntity,
                        finalVehicleCompoundCollider
                    );
                }
                finally
                {
                    for (int i = 0; i < totalReadyCount; i++)
                    {
                        Entity chunkEntity = chunksDataArray[i].TargetEntity;

                        if (EntityManager.Exists(chunkEntity))
                        {
                            EntityManager.SetComponentEnabled<ChunkColliderNeedApply>(chunkEntity, false); // Выключили до следующего взрыва!
                        }

                        // очищаем данные о статусах
                        if (voxelChildColliderRegistrySingleton.Registry[chunkEntity].SafeStatus.Length > 0)
                        {
                            for (int j = 0; j < voxelChildColliderRegistrySingleton.Registry[chunkEntity].SafeStatus.Length; j++)
                            {
                                var safeStatusItem = voxelChildColliderRegistrySingleton.Registry[chunkEntity];
                                if (safeStatusItem.SafeStatus.IsCreated)
                                {
                                    //safeStatusItem.SafeStatus.Dispose();
                                    //safeStatusItem.SafeStatus = default;
                                    // сбрасываем статус джобы.
                                    safeStatusItem.SafeStatus[0] = new int3(0, 0, 0);
                                }
                                voxelChildColliderRegistrySingleton.Registry[chunkEntity] = safeStatusItem;
                            }
                        }
                    }

                    chunksDataArray.Dispose();
                }

            }



            //var chunkColliderData = voxelChildColliderRegistrySingleton.Registry[chunkEntity];
            //// АТОМАРНЫЙ БЕЗОПАСНЫЙ ДЕТЕКТОР:
            //// Если фоновый воркер процессора ВСЁ ЕЩЕ ПИШЕТ данные в Persistent-массивы этого чанка —
            //// мы КАТЕГОРИЧЕСКИ пропускаем его в текущем кадре симуляции!
            //// Вызов .IsCompleted занимает 0.00 мс и НИКОГДА не фризит главный поток игры.
            //if (!chunkColliderData.SafeStatus.IsCreated || chunkColliderData.SafeStatus[0].z != 1)
            //{
            //    continue;
            //}

            //rootVehicleEntity = chunkColliderData.RootVehicleEntity; //chunkColliderData.ValueRO.RootVehicleEntity;

            //// Заполняем плоскую структуру NewChunkGraphicsData прямыми С++ ссылками на Persistent массивы
            //chunksDataArray.Add(new NewChunkColliderData
            //{
            //    TargetEntity = chunkEntity,
            //    //LocalBounds = chunkColliderData.ValueRO.LocalBounds,
            //    //WorldBounds = chunkColliderData.ValueRO.WorldBounds,
            //    //SafeStatus = chunkColliderData.ValueRO.SafeCounter,
            //    //SafeColliderBlob = chunkColliderData.ValueRO.SafeColliderBlob,

            //});

            ////var isClient = World.IsClient();
            ////string textWorld = isClient ? "Client" : "Server";

            ////UnityEngine.Debug.Log($"[{textWorld}] Добавление NewChunkGraphicsData для {flushTag.ValueRO.index}");


            ////childOffsetsList.Add(chunkColliderData.LocalOffsetWithPivot);
        }

        //// Если в текущем кадре ни один из чанков еще до конца не завершил фоновые вычисления —
        //// мгновенно закрываем контекст кадра, не нагружая процессор вхолостую!
        //if (chunksDataArray.Length == 0)
        //{
        //    chunksDataArray.Dispose();
        //    //childOffsetsList.Dispose();
        //    return;
        //}

        // ====================================================================
        // ФАЗА 3: СБОРКА МОНОЛИТНОГО COMPOUND COLLIDER АВТОМОБИЛЯ ИЗ ГОТОВЫХ ЧАНКОВ
        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        // ====================================================================
        //int totalReadyCount = chunksDataArray.Length;
        //int validCollidersCount = 0;

        //for (int i = 0; i < totalReadyCount; i++)
        //{
        //    var dataFromSingleton = voxelChildColliderRegistrySingleton.Registry[chunksDataArray[i].TargetEntity];
        //    //int3 finalCounts = dataFromSingleton.SafeStatus[0];
        //    //if (finalCounts.x > 0 && dataFromSingleton.SafeColliderBlob[0].IsCreated) // && chunksDataArray[i].SafeColliderBlob.IsCreated
        //    if (dataFromSingleton.GeometryArray.IsCreated)
        //    {
        //        validCollidersCount++;
        //    }
        //}

        //BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

        //if (validCollidersCount > 0)
        //{
        //    var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
        //    int currentInstanceIdx = 0;

        //    for (int i = 0; i < totalReadyCount; i++)
        //    {
        //        var dataFromSingleton = voxelChildColliderRegistrySingleton.Registry[chunksDataArray[i].TargetEntity];
        //        //int3 finalCounts = dataFromSingleton.SafeStatus[0];
        //        //if (finalCounts.x > 0 && dataFromSingleton.SafeColliderBlob[0].IsCreated)// && chunksDataArray[i].SafeColliderBlob.IsCreated
        //        if (dataFromSingleton.GeometryArray.IsCreated)
        //        {
        //            // Создаем BlobAssetReference на главном потоке (быстрая операция)
        //            BlobAssetReference<Unity.Physics.Collider> newColliderBlob = Unity.Physics.BoxCollider.Create(dataFromSingleton.GeometryArray[0]);

        //            compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
        //            {
        //                Collider = newColliderBlob, // dataFromSingleton.SafeColliderBlob[0], //chunksDataArray[i].SafeColliderBlob[0],
        //                CompoundFromChild = new RigidTransform(quaternion.identity, dataFromSingleton.LocalOffsetWithPivot),//childOffsetsList[i]
        //                Entity = chunksDataArray[i].TargetEntity
        //            };

        //            currentInstanceIdx++;
        //        }
        //    }

        //    if (compoundInstances.Length > 0)
        //    {
        //        finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
        //    }

        //    // 3. ИСПРАВЛЕНИЕ УТЕЧКИ: Проходим по массиву и уничтожаем оригинальные временные Blob-активы чанков
        //    for (int i = 0; i < compoundInstances.Length; i++)
        //    {
        //        if (compoundInstances[i].Collider.IsCreated)
        //        {
        //            // Освобождаем неуправляемую память каждого дочернего кубика
        //            compoundInstances[i].Collider.Dispose();
        //        }
        //    }
        //    compoundInstances.Dispose();
        //}

        //childOffsetsList.Dispose();

        //try
        //{
        //    ExecuteManagedCollider(
        //        ref this.CheckedStateRef,
        //        chunksDataArray,
        //        rootVehicleEntity,
        //        finalVehicleCompoundCollider
        //    );
        //}
        //finally
        //{
        //    for (int i = 0; i < totalReadyCount; i++)
        //    {
        //        Entity chunkEntity = chunksDataArray[i].TargetEntity;

        //        if (EntityManager.Exists(chunkEntity))
        //        {
        //            EntityManager.SetComponentEnabled<ChunkColliderNeedApply>(chunkEntity, false); // Выключили до следующего взрыва!
        //        }

        //        // очищаем данные о статусах
        //        if (voxelChildColliderRegistrySingleton.Registry[chunkEntity].SafeStatus.Length > 0)
        //        {
        //            for (int j = 0; j < voxelChildColliderRegistrySingleton.Registry[chunkEntity].SafeStatus.Length; j++)
        //            {
        //                var safeStatusItem = voxelChildColliderRegistrySingleton.Registry[chunkEntity];
        //                if (safeStatusItem.SafeStatus.IsCreated)
        //                {
        //                    //safeStatusItem.SafeStatus.Dispose();
        //                    //safeStatusItem.SafeStatus = default;
        //                    // сбрасываем статус джобы.
        //                    safeStatusItem.SafeStatus[0] = new int3(0, 0, 0);
        //                }
        //                voxelChildColliderRegistrySingleton.Registry[chunkEntity] = safeStatusItem;
        //            }
        //        }
        //    }

        //    chunksDataArray.Dispose();
        //}

        // Гарантируем, что джобы завершились перед тем, как мы начнем чистить память на главном потоке
        this.CheckedStateRef.Dependency.Complete();

        // 5. БЕЗОПАСНАЯ ОЧИСТКА НА ГЛАВНОМ ПОТОКЕ (Без структурных изменений!)
        if (voxelChildColliderRegistrySingleton.DisposeList.Length > 0)
        {
            for (int i = 0; i < voxelChildColliderRegistrySingleton.DisposeList.Length; i++)
            {
                if (voxelChildColliderRegistrySingleton.DisposeList[i].IsCreated)
                {
                    voxelChildColliderRegistrySingleton.DisposeList[i].Dispose(); // Здесь Unity Physics уже отпустила ссылку, краша не будет
                }
            }
            voxelChildColliderRegistrySingleton.DisposeList.Clear(); // Обнуляем длину списка для следующего кадра
        }
    }


    // ЭТОТ МЕТОД ВЫПОЛНЯЕТСЯ В MANAGED РЕЖИМЕ БЕЗ КОНФЛИКТОВ С BURST
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteManagedCollider(
            ref SystemState state,
            NativeList<NewChunkColliderData> chunksData,
            Entity rootVehicleEntity,
            BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider)
    {
        var isClient = state.WorldUnmanaged.IsClient();
        string textWorld = isClient ? "Client" : "Server";

        // Получаем синглтон коллайдеров
        var voxelChildColliderRegistrySingleton = SystemAPI.GetSingleton<VoxelChildColliderRegistrySingleton>();

        //// Буфер команд
        ////var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        ////var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        //var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        bool isColliderAssignedToEntity = false;
        try
        {
            // Проверка: если маркер есть, но физики (скорости или самого коллайдера) уже нет — 
            // значит машина деспавнится прямо сейчас! Игнорируем её и сразу уничтожаем свежевыпеченный блоб.
            //bool isEntityDying = state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity)
            //                     && !state.EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity);
            // Легальный и Burst-безопасный способ понять, что Netcode утилизирует машину:
            bool isEntityDying = (!state.EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity)
                                     || !state.EntityManager.HasComponent<Unity.NetCode.GhostInstance>(rootVehicleEntity));
            //state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity)
            //                     &&

            if (rootVehicleEntity != Entity.Null && state.EntityManager.Exists(rootVehicleEntity) && finalVehicleCompoundCollider.IsCreated && !isEntityDying)
            {
                // Если для этой сетевой сущности УЖЕ был коллайдер — выгружаем его из RAM
                // Это происходит мгновенно на С++ уровне без структурных изменений!
                if (m_ColliderRegistry.TryGetValue(rootVehicleEntity, out var oldGroup))
                {
                    //if (oldGroup.ColliderBlob.IsCreated)
                    //{
                    //    oldGroup.ColliderBlob.Dispose();
                    //}

                    // Потокобезопасно скидываем старый блоб в мусорку
                    voxelChildColliderRegistrySingleton.DisposeList.Add(oldGroup.ColliderBlob);
                }

                // Записываем новый сгенерированный коллайдер
                var newGroup = new VoxelColliderCleanupMarker
                {
                    ColliderBlob = finalVehicleCompoundCollider
                };
                m_ColliderRegistry[rootVehicleEntity] = newGroup;

                // 3. ЗАПИСЫВАЕМ НОВЫЕ ВАЛИДНЫЕ ДАННЫЕ
                state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

                byte typeCollider = 0;
                if (SystemAPI.HasComponent<AAA_RootData>(rootVehicleEntity))
                {
                    typeCollider = SystemAPI.GetComponentRO<AAA_RootData>(rootVehicleEntity).ValueRO.TypeCollider;
                }

                if (typeCollider == 0)
                {
                    // Вручную забиваем структуру нулями
                    var kinematicMass = new PhysicsMass
                    {
                        Transform = RigidTransform.identity,
                        InverseMass = 0f,                // 1 / бесконечность = 0 кг инверсного веса
                        InverseInertia = float3.zero,    // Нулевая инверсная инерция (тело нельзя повернуть ударом)
                        CenterOfMass = float3.zero
                    };
                    kinematicMass.CenterOfMass = float3.zero;

                    state.EntityManager.SetComponentData(rootVehicleEntity, kinematicMass);

                    state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsVelocity());
                }
                else
                {
                    // 4. ЗАЩИТА МАССЫ ОТ NaN (Деления на ноль)
                    var massProperties = finalVehicleCompoundCollider.Value.MassProperties;

                    // ИСПРАВЛЕНИЕ: Извлекаем тензор инерции через InertiaTensorWithOrientation
                    float3 inertia = massProperties.MassDistribution.InertiaTensor;

                    // Проверяем тензор инерции, если он сломан/нулевой (при пустом или недостроенном воксельном меше)
                    if (inertia.x <= 0f || inertia.y <= 0f || inertia.z <= 0f)
                    {
                        // Подставляем безопасную единичную сферу, чтобы физика Unity не выдала NaN
                        massProperties = MassProperties.UnitSphere;
                    }

                    var dynamicMass = PhysicsMass.CreateDynamic(massProperties, 1000.0f);
                    dynamicMass.CenterOfMass = float3.zero;
                    state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);

                }


                state.EntityManager.SetComponentData(rootVehicleEntity, new AAA_MovementComponent
                {
                    MaxSpeed = 45f,
                    Acceleration = 25f,
                    Deceleration = 18f
                });


                //// 2. РАЗДЕЛЯЕМ ЛОГИКУ: Перепекание (существующая машина) ИЛИ Новый спавн
                //if (state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
                //{
                //    // ЕСЛИ МАШИНА УЖЕ СУЩЕСТВУЕТ (Перепекание):
                //    // Структура сущности НЕ меняется. Изменение данных через SetComponentData БЕЗОПАСНО делать прямо на месте.
                //    var oldCleanupData = state.EntityManager.GetComponentData<VoxelColliderCleanupMarker>(rootVehicleEntity);

                //    if (oldCleanupData.ColliderBlob.IsCreated) oldCleanupData.ColliderBlob.Dispose();
                //    for (int c = 0; c < oldCleanupData.ChildBlobs.Length; c++)
                //    {
                //        if (oldCleanupData.ChildBlobs[c].IsCreated) oldCleanupData.ChildBlobs[c].Dispose();
                //    }

                //    state.EntityManager.SetComponentData(rootVehicleEntity, newCleanupData);
                //    state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

                //    // Массу тоже обновляем на месте, если компонент уже есть
                //    var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
                //    dynamicMass.CenterOfMass = float3.zero;
                //    state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);
                //}
                //else
                //{
                //    // ЕСЛИ МАШИНА ТОЛЬКО ЧТО ЗАСПАВНИЛАСЬ (Новая):
                //    // Все операции AddComponent делаем СТРОГО через ecb! Прямой EntityManager здесь ЗАПРЕЩЕН.
                //    ecb.AddComponent(rootVehicleEntity, new VoxelColliderCleanupMarker { ColliderBlob = finalVehicleCompoundCollider });
                //    ecb.AddComponent(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

                //    var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
                //    dynamicMass.CenterOfMass = float3.zero;
                //    ecb.AddComponent(rootVehicleEntity, dynamicMass);
                //    ecb.AddComponent(rootVehicleEntity, new PhysicsVelocity());

                //    // Главный триггер краша Ghost Map (Shared-компонент) уходит в буфер команд
                //    ecb.AddSharedComponent(rootVehicleEntity, new PhysicsWorldIndex { Value = 0 });

                //    ecb.AddComponent(rootVehicleEntity, new AAA_MovementComponent
                //    {
                //        MaxSpeed = 45f,
                //        Acceleration = 25f,
                //        Deceleration = 18f
                //    });
                //}

                isColliderAssignedToEntity = true;

            }
            else
            {
                // Сущности нет — просто удаляем только что созданный коллайдер
                if (finalVehicleCompoundCollider.IsCreated)
                {
                    finalVehicleCompoundCollider.Dispose();
                }
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning($"[{textWorld}] Не найдена корневая сущность или она уничтожена! Свежий коллайдер удален.");
#endif
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[{textWorld}] Error: {ex.Message}"); ;

            // Если что-то пошло не так во время AddComponent/SetComponent (например, OutOfMemory или Burst-abort)
            if (!isColliderAssignedToEntity && finalVehicleCompoundCollider.IsCreated)
            {
                finalVehicleCompoundCollider.Dispose();
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning($"[{textWorld}] Критический сбой! Коллайдер принудительно стерт в блоке finally.");
#endif
            }
        }
        // ====================================================================

        //// ====================================================================
        //// ЭТАП 2: БЕЗОПАСНАЯ ФИКСАЦИЯ НА GPU И СБОРКА КОМПОНЕНТОВ BRG
        //// ====================================================================
        //for (int i = 0; i < chunksData.Length; i++)
        //{
        //    var chunkData = chunksData[i];

        //    if (chunkData.SafeStatus.IsCreated) chunkData.SafeStatus.Dispose();

        //}
    }
}