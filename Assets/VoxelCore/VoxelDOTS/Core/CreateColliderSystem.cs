using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

public struct ChunkColliderNeedCreate : IComponentData, IEnableableComponent { }
public struct ChunkColliderNeedApply : IComponentData, IEnableableComponent { }

//[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
//public struct ChunkColliderData : IComponentData, IEnableableComponent
//{
//    public NativeArray<int3> SafeCounter;
//    //public NativeArray<BlobAssetReference<Unity.Physics.Collider>> SafeColliderBlob;

//    // Ссылка на хэндл джобы для точечной проверки готовности
//    //public JobHandle LastBakingJobHandle;

//    public Entity RootVehicleEntity;
//    public float3 LocalOffsetWithPivot;
//    public MinMaxAABB LocalBounds;
//    public MinMaxAABB WorldBounds;
//    public int3 index;
//    public bool isCreatedCollider;
//}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial struct CreateColliderSystem : ISystem
{
    // Внутри объявления структуры вашей системы (ISystem):
    //private ComponentLookup<ChunkActiveState> m_ActiveStateLookup;
    //private ComponentLookup<ChunkColliderData> m_ChunkColliderDataLookup;
    private BufferLookup<LocalChunkDestructionMask> m_MaskBufferLookup;
    private ComponentLookup<Parent> m_ParentLookup;
    private ComponentLookup<ChunkIndexComponent> m_ChunkIndexLookup;
    private ComponentLookup<VoxelModelHeader> m_ModelHeaderLookup;
    private ComponentLookup<GhostInstance> m_GhostInstanceLookup;
    private ComponentLookup<LocalToWorld> m_LocalToWorldLookup;
    private ComponentLookup<ChunkColliderNeedCreate> m_ChunkColliderNeedCreate;

    EntityQuery m_RebuildQuery;

    private NativeList<JobHandle> m_JobHandles;

    //[BurstCompile(CompileSynchronously = true)]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GlobalVoxelModelCache>();
#if !UNITY_SERVER
        state.RequireForUpdate<VoxelGlobalConfigComponent>();
#endif
        // Создаем сущность-синглтон и записываем туда ссылки
        if (state.World.IsClient() || state.World.IsServer())
        {
            // Выделяем память под контейнеры
            var registry = new NativeParallelHashMap<Entity, ChunkColliderData>(100000, Allocator.Persistent);
            var disposeList = new NativeList<BlobAssetReference<Collider>>(Allocator.Persistent);

            Entity singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new VoxelChildColliderRegistrySingleton
            {
                Registry = registry,
                DisposeList = disposeList
            });
        }

        m_JobHandles = new NativeList<JobHandle>(16, Allocator.Persistent);

        // Инициализируем лукапы при создании системы // true = ReadOnly
        //m_ActiveStateLookup = state.GetComponentLookup<ChunkActiveState>();
        //m_ChunkColliderDataLookup = state.GetComponentLookup<ChunkColliderData>();
        m_MaskBufferLookup = state.GetBufferLookup<LocalChunkDestructionMask>(true);
        m_ParentLookup = state.GetComponentLookup<Parent>(true);
        m_GhostInstanceLookup = state.GetComponentLookup<GhostInstance>(true);
        m_ChunkIndexLookup = state.GetComponentLookup<ChunkIndexComponent>(true);
        m_ModelHeaderLookup = state.GetComponentLookup<VoxelModelHeader>(true);
        m_GhostInstanceLookup = state.GetComponentLookup<GhostInstance>(true);
        m_LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        m_ChunkColliderNeedCreate = state.GetComponentLookup<ChunkColliderNeedCreate>(false);

        // ====================================================================
        // ИСПРАВЛЕНИЕ 1: Использование кэшированного EntityQuery (O(1) скорость)
        // Предполагается, что m_RebuildQuery инициализирован в OnCreate:
        using (var builder = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<LocalChunkDestructionMask>() // Билдер сам определит, что это буфер!
        .WithAll<ChunkIndexComponent>()
        .WithAll<VoxelModelHeader>()
        .WithAll<GhostInstance>()
        .WithAll<ChunkColliderNeedCreate>())
        {
            m_RebuildQuery = state.GetEntityQuery(builder);
        }
        // ====================================================================
    }

    [BurstCompile]//(CompileSynchronously = true)
    public void OnDestroy(ref SystemState state)
    {
        // Уничтожать контейнеры нужно строго там же, где создавали, чтобы избежать утечек
        if (SystemAPI.TryGetSingleton<VoxelChildColliderRegistrySingleton>(out var singleton))
        {
            // 1. Очищаем коллайдеры, оставшиеся в реестре
            if (singleton.Registry.IsCreated)
            {
                // Получаем массив всех значений (маркеров) из хэш-мапы
                var markers = singleton.Registry.GetValueArray(Allocator.Temp);

                for (int i = 0; i < markers.Length; i++)
                {
                    var marker = markers[i];
                    if (marker.SafeColliderBlob.IsCreated)
                    {
                        for (int y = 0; y < marker.SafeColliderBlob.Length; y++)
                        {
                            if (marker.SafeColliderBlob[y].IsCreated)
                            {
                                marker.SafeColliderBlob[y].Dispose();
                            }
                        }
                    }
                    marker.SafeColliderBlob.Dispose();
                    if (marker.SafeStatus.IsCreated) marker.SafeStatus.Dispose();
                }
                markers.Dispose();
                // Теперь безопасно удаляем саму хэш-мапу
                singleton.Registry.Dispose();
            }

            // 2. Очищаем коллайдеры, которые ожидали утилизации в списке мусора
            if (singleton.DisposeList.IsCreated)
            {
                for (int i = 0; i < singleton.DisposeList.Length; i++)
                {
                    var blob = singleton.DisposeList[i];
                    if (blob.IsCreated)
                    {
                        blob.Dispose();
                    }
                }

                // Теперь безопасно удаляем сам список
                singleton.DisposeList.Dispose();
            }
        }

        if (m_JobHandles.IsCreated) m_JobHandles.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    public void OnUpdate(ref SystemState state)
    {
        // 1. В начале OnUpdate сохраняем ТЕКУЩИЙ сетевой тик как структуру NetworkTick
        if (!SystemAPI.HasSingleton<NetworkTime>()) return;
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;

        var voxelChildColliderRegistrySingleton = SystemAPI.GetSingleton<VoxelChildColliderRegistrySingleton>();
        var cache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

        // Лукапы состояний обновляем
        //m_ActiveStateLookup.Update(ref state);
        //m_ChunkColliderDataLookup.Update(ref state);
        m_MaskBufferLookup.Update(ref state);
        m_ParentLookup.Update(ref state);
        m_GhostInstanceLookup.Update(ref state);
        m_ChunkIndexLookup.Update(ref state);
        m_ModelHeaderLookup.Update(ref state);
        m_GhostInstanceLookup.Update(ref state);
        m_LocalToWorldLookup.Update(ref state);
        m_ChunkColliderNeedCreate.Update(ref state);

        // Высокоскоростной Burst-заменитель для EntityManager.Exists
        var entityStorageInfoLookup = state.GetEntityStorageInfoLookup();

        m_JobHandles.Clear();

        // Считаем точное число чанков на перестроение в текущем кадре
        //int totalChunksToRebuild = 0;
        //foreach (var (maskBuffer, chunkIndex, modelHeader, entity) in SystemAPI.Query<
        //    DynamicBuffer<LocalChunkDestructionMask>,
        //    RefRO<ChunkIndexComponent>,
        //    RefRO<VoxelModelHeader>
        //  >()
        //    .WithAll<ChunkColliderNeedCreate>()
        //    .WithEntityAccess())
        //{
        //    totalChunksToRebuild++;
        //}
        //if (totalChunksToRebuild == 0) return;
        //var childOffsetsArray = new NativeArray<float3>(totalChunksToRebuild, Allocator.Persistent);

        //int jobIndex = 0;

        int totalChunksToRebuild = m_RebuildQuery.CalculateEntityCount();
        if (totalChunksToRebuild == 0) return;

        // Безопасно собираем сущности в плоский нативный массив.
        // Никакие переключения JobHandle и деструкции больше не сломают итерацию!
        var entitiesToProcess = m_RebuildQuery.ToEntityArray(Allocator.TempJob);

        var childOffsetsArray = new NativeArray<float3>(totalChunksToRebuild, Allocator.Persistent);
        int jobIndex = 0;
        JobHandle loopDependency = state.Dependency;

        // ====================================================================
        // ЭТАП 1 (BURST): АСИНХРОННЫЙ ПРОЛЕТ ПО ВСЕМ ИЗМЕНИВШИМСЯ ЧАНКАМ
        // ====================================================================
        //foreach (var (maskBuffer, chunkIndex, modelHeader, ghostInstance, entity) in SystemAPI.Query<
        //        DynamicBuffer<LocalChunkDestructionMask>,
        //        RefRO<ChunkIndexComponent>,
        //        RefRO<VoxelModelHeader>,
        //        RefRO<GhostInstance>
        //    >()
        //    .WithAll<ChunkColliderNeedCreate>()
        //     //.WithDisabled<ChunkColliderData>()
        //     //.WithDisabled<ChunkActiveState>()
        //     .WithEntityAccess())
        //{
        for (int i = 0; i < entitiesToProcess.Length; i++)
        {
            Entity entity = entitiesToProcess[i];

            //if (!state.EntityManager.Exists(entity)) continue;
            if (!entityStorageInfoLookup.Exists(entity)) continue;

            Entity rootVehicleEntity = Entity.Null;

            // Получаем корень автомобиля (предполагаем, что у вас есть компонент родителя, например Parent)
            if (m_ParentLookup.HasComponent(entity))
            {
                rootVehicleEntity = m_ParentLookup[entity].Value;
            }
            if (rootVehicleEntity == Entity.Null) continue;

            if (!m_GhostInstanceLookup.HasComponent(rootVehicleEntity)) continue;

            // Извлекаем данные чанка напрямую через высокоскоростной Lookup
            var maskBuffer = m_MaskBufferLookup[entity];
            var chunkIndex = m_ChunkIndexLookup[entity];
            var modelHeader = m_ModelHeaderLookup[entity];
            var ghostInstance = m_GhostInstanceLookup[rootVehicleEntity];

            // Извлекаем тик спавна САМОГО ЧАНКА из переменной итератора query
            NetworkTick chunkSpawnTick = ghostInstance.spawnTick;

            if (currentTick.IsValid && chunkSpawnTick.IsValid)
            {
                uint currentTickIdx = currentTick.TickIndexForValidTick;
                uint chunkSpawnTickIdx = chunkSpawnTick.TickIndexForValidTick;
                uint ticksPassed = currentTickIdx - chunkSpawnTickIdx;

                // Если САМ ЧАНК слишком молодой (меньше 4 тиков) — отправляем его на карантин
                if (ticksPassed >= 0 && ticksPassed < 4)
                {
                    continue; // Строго ПРОПУСКАЕМ этот чанк, давая Netcode время уложить его в Ghost Map!
                }
            }

            // Проверяем сетевой статус госта через встроенные unmanaged-флаги Netcode
            if (ghostInstance.ghostId <= 0) // Или простая проверка на возраст тика кадра
            {
                // Самый надежный Safe-вариант для Burst: если гост только что заспавнился на клиенте, 
                // даем ему 1 кадр на сетевую акклиматизацию
                continue;
            }

            uint modelHash = modelHeader.ConfigHashName;
            if (modelHash == 0) continue;

            if (!cache.Templates.TryGetValue(modelHash, out var template)) continue;
            if (!template.ChunkCoordToOrderIndexMap.TryGetValue(chunkIndex.Value, out int chunkOrderIndex)) continue;

            int chunkOffset = chunkOrderIndex * 32768;

            // МГНОВЕННАЯ БЛОКИРОВКА ЧАНКА!

            //m_ChunkColliderNeedCreate.SetComponentEnabled(entity, false);
            SystemAPI.SetComponentEnabled<ChunkColliderNeedCreate>(entity, false);

            var singleChunkStatus = new NativeArray<int3>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // Выделяем массив для блоба коллайдера
            var singleChunkColliderBlob = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // 2. Планируем джобу выпекания MeshCollider для этого чанка
            var colliderJob = new GenerateChunkColliderJob
            {
                LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
                OutputColliderBlob = singleChunkColliderBlob,
                JobCountersRef = singleChunkStatus,
                FlattenedModelColors = template.FlattenedLinearColors,
                ChunkOffsetInFlattenedArray = chunkOffset,
            };

            JobHandle chunkColliderHandle = colliderJob.Schedule(state.Dependency);

            m_JobHandles.Add(chunkColliderHandle);

            // 1. Вычисляем исходное локальное смещение чанка (размер чанка 32 вокселя)
            float3 baseLocalOffset = (float3)chunkIndex.Value * 32f * 1.0f;

            // Пивот
            float3 pivotOffset = new float3(
                (template.SizeModel.x * 32f) / 2f,
                0f, // Оставляем 0, чтобы физическое дно машины совпало с пивотом (0,5,0)
                (template.SizeModel.z * 32f) / 2f
            );

            // Вычитаем смещение, чтобы сдвинуть физический бокс коллайдера чанка вслед за графикой!
            float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

            // Записываем СМЕЩЕННЫЙ оффсет в массив для генерации CompoundCollider
            childOffsetsArray[jobIndex] = localOffsetWithPivot;

            // 2. СЧИТАЕМ BOUNDS С УЧЕТОМ НОВОГО ЛОКАЛЬНОГО ПОЛОЖЕНИЯ
            float3 correctedChunkCenterInVehicleSpace = localOffsetWithPivot + new float3(16f, 16f, 16f);

            var aabbLocal = new Unity.Mathematics.AABB
            {
                Center = correctedChunkCenterInVehicleSpace,
                Extents = new float3(16f, 16f, 16f)
            };

            var aabbWorld = aabbLocal;

            // Если у машины (а не у чанка!) есть LocalToWorld, трансформируем локальный AABB в мировой
            if (m_LocalToWorldLookup.HasComponent(rootVehicleEntity))
            {
                var parentLtw = m_LocalToWorldLookup[rootVehicleEntity];// state.EntityManager.GetComponentData<LocalToWorld>(rootVehicleEntity);
                aabbWorld = Unity.Mathematics.AABB.Transform(parentLtw.Value, aabbLocal);
            }

            // БЕЗОПАСНАЯ РЕГИСТРАЦИЯ НА СУЩНОСТИ ЧАНКА:
            //m_ChunkColliderDataLookup[entity] = new ChunkColliderData
            //{
            //    //LastBakingJobHandle = chunkColliderHandle,
            //    //SafeColliderBlob = singleChunkColliderBlob,
            //    SafeCounter = singleChunkCounter,
            //    RootVehicleEntity = rootVehicleEntity,
            //    LocalOffsetWithPivot = localOffsetWithPivot,
            //    LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
            //    WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
            //    //HasGraphicsBefore = hasGraphics,
            //    index = chunkIndex.Value
            //};

            if (voxelChildColliderRegistrySingleton.Registry.TryGetValue(entity, out var oldBlob))
            {
                if (oldBlob.SafeColliderBlob.IsCreated)
                {
                    //voxelChildColliderRegistrySingleton.DisposeList.AddRange(oldBlob);
                    for (int x = 0; x < oldBlob.SafeColliderBlob.Length; x++)
                    {
                        if (oldBlob.SafeColliderBlob[x].IsCreated)
                        {
                            oldBlob.SafeColliderBlob[x].Dispose();
                        }
                    }
                    oldBlob.SafeColliderBlob.Dispose();
                }

                if (oldBlob.SafeStatus.IsCreated)
                {
                    oldBlob.SafeStatus.Dispose();
                }

            }
            // добавляем коллайдер чанка
            voxelChildColliderRegistrySingleton.Registry[entity] = new ChunkColliderData
            {
                SafeColliderBlob = singleChunkColliderBlob,
                SafeStatus = singleChunkStatus,
                RootVehicleEntity = rootVehicleEntity,

                LocalOffsetWithPivot = localOffsetWithPivot,
                LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
                WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
                //HasGraphicsBefore = hasGraphics,
                index = chunkIndex.Value
            };

            //m_ChunkColliderDataLookup.SetComponentEnabled(entity, true);

            SystemAPI.SetComponentEnabled<ChunkColliderNeedApply>(entity, true);

            jobIndex++;
        }

        entitiesToProcess.Dispose();

        if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();

        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        if (m_JobHandles.Length == 0)
        {
            // Если в этом кадре разрушений не было — просто сбрасываем список
            m_JobHandles.Clear();
            return;
        }

        // ====================================================================
        // Объединяем хэндлы асинхронного выпекания ВСЕХ индивидуальных чанков.
        // ====================================================================
        JobHandle allChunksBakedHandle = JobHandle.CombineDependencies(m_JobHandles.AsArray());

        // Передаем итоговую чистую зависимость кадра в систему
        state.Dependency = allChunksBakedHandle;

        // Пинаем планировщик Unity: воркеры мгновенно и параллельно разберут все чанки!
        JobHandle.ScheduleBatchedJobs();
    }
}