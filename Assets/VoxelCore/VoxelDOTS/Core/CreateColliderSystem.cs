using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

public struct NewChunkColliderData // : IComponentData, IEnableableComponent
{
    public Entity TargetEntity;
    public int JobIndex;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;

    // ДОБАВЛЯЕМ: Ссылки на массивы для безопасного С++ копирования
    public NativeArray<VoxelVertex> SafeVertices;
    public NativeArray<int> SafeIndices;

    // ДОБАВЛЯЕМ: Ссылка на персональный массив счетчика
    public NativeArray<int2> SafeCounter;

    // МЕНЯЕМ ТИП: Сюда мы сохраним индивидуальный нативный массив чанка
    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> SafeColliderBlob;
}

[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
public struct ChunkColliderData : IComponentData, IEnableableComponent
{
    //// Храним чистые, изолированные C++ массивы геометрии ТОЛЬКО этого чанка!
    //public NativeArray<VoxelVertex> SafeVertices;
    //public NativeArray<int> SafeIndices;
    //public NativeArray<int2> SafeCounter;
    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> SafeColliderBlob;
    //public NativeList<BakedBoxData> BakedBoxes;
    //public BlobAssetReference<Collider> SafeColliderBlob;

    // Ссылка на хэндл джобы для точечной проверки готовности
    public JobHandle LastBakingJobHandle;

    public Entity RootVehicleEntity;
    public float3 LocalOffsetWithPivot;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;
    public int3 index;
    public bool isCreatedCollider;
}

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct CreateColliderSystem : ISystem
{
    private ComponentLookup<ChunkColliderData> m_ChunkColliderDataLookup;

    private ComponentLookup<ChunkActiveState> m_ActiveStateLookup;

    // Внутри объявления структуры вашей системы (ISystem):
    ComponentLookup<Parent> m_ParentLookup;
    ComponentLookup<Unity.NetCode.GhostInstance> m_GhostInstanceLookup;

    private BufferTypeHandle<LocalChunkDestructionMask> m_MaskTypeHandle;

    private NativeList<JobHandle> m_JobHandles;
    //private NativeList<NewChunkGraphicsData> m_ChunksToInitialize;

    //// 1. Объявляем приватные поля для кэширования запросов
    //private EntityQuery m_ConfigQuery;
    //private EntityQuery m_StorageQuery;

    //[BurstCompile(CompileSynchronously = true)]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GlobalVoxelModelCache>();
        state.RequireForUpdate<VoxelGlobalConfigComponent>();

        m_MaskTypeHandle = state.GetBufferTypeHandle<LocalChunkDestructionMask>(true);

        m_JobHandles = new NativeList<JobHandle>(16, Allocator.Persistent);
        //m_ChunksToInitialize = new NativeList<NewChunkGraphicsData>(16, Allocator.Persistent);

        //// 2. Создаем запросы ОДИН РАЗ при инициализации системы
        //m_ConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<VoxelGlobalConfigComponent>());
        //m_StorageQuery = state.GetEntityQuery(ComponentType.ReadOnly<ClientVoxelMeshFrameStorage>());

        m_ChunkColliderDataLookup = state.GetComponentLookup<ChunkColliderData>();
        m_ActiveStateLookup = state.GetComponentLookup<ChunkActiveState>();

        // Инициализируем лукапы при создании системы
        m_ParentLookup = state.GetComponentLookup<Parent>(true); // true = ReadOnly
        m_GhostInstanceLookup = state.GetComponentLookup<Unity.NetCode.GhostInstance>(true);
    }


    [BurstCompile(CompileSynchronously = true)]
    public void OnDestroy(ref SystemState state)
    {
        if (m_JobHandles.IsCreated) m_JobHandles.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    public void OnUpdate(ref SystemState state)
    {
        //// 1. В начале OnUpdate сохраняем ТЕКУЩИЙ сетевой тик как структуру NetworkTick
        //if (!SystemAPI.HasSingleton<NetworkTime>()) return;
        //var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        //NetworkTick currentTick = networkTime.ServerTick;

        ////// Свойство IsFirstTimePredictedTick сообщает, что Netcode сейчас выполняет 
        ////// "свежий" кадр, а не прокручивает историю тиков назад для ресимуляции.
        ////// Если это повторный тик перемотки — мы мгновенно выходим и не дублируем код!
        ////if (!networkTime.IsFinalPredictionTick)
        ////{
        ////    return;
        ////}

        //m_ChunkColliderDataLookup.Update(ref state);
        //m_ActiveStateLookup.Update(ref state);
        //m_ParentLookup.Update(ref state);
        //m_GhostInstanceLookup.Update(ref state);

        //// Буфер команд
        ////var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        ////var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        //var cache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

        //m_MaskTypeHandle.Update(ref state);
        //uint lastSystemVersion = state.LastSystemVersion;

        //m_JobHandles.Clear();
        ////m_ChunksToInitialize.Clear();

        //// Считаем точное число чанков на перестроение в текущем кадре
        //int totalChunksToRebuild = 0;
        //foreach (var (maskBuffer, chunkIndex, modelHeader, entity) in SystemAPI.Query<DynamicBuffer<LocalChunkDestructionMask>, RefRO<ChunkIndexComponent>, RefRO<VoxelModelHeader>>().WithDisabled<ChunkActiveState>().WithEntityAccess())
        //{
        //    totalChunksToRebuild++;
        //}

        //if (totalChunksToRebuild == 0) return;

        //// ====================================================================
        //// ЧИСТЫЙ AAA SAFE ПАЙПЛАЙН: Выделяем ТОЛЬКО три этих массива!
        //// Старый childCollidersList КАТЕГОРИЧЕСКИ УДАЛЕН со строки 79!
        //// ====================================================================
        //var childOffsetsArray = new NativeArray<float3>(totalChunksToRebuild, Allocator.Persistent);
        //// ====================================================================


        //int jobIndex = 0;



        ////// ====================================================================
        ////// ФАЗА Б: ЗАПУСК НОВЫХ РАЗРУШЕНИЙ ИЛИ СПАВНОВ ЧАНКОВ
        ////// ЖЕЛЕЗОБЕТОННЫЙ SAFE-ФИКС ДЛЯ PresentationSystemGroup:
        ////// Мы КАТЕГОРИЧЕСКИ стерли отсюда GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()!
        ////// Создаем чистый, изолированный unmanaged буфер команд на аллокаторе TempJob.
        ////// Ошибка "EntityCommandBuffer has been deallocated" исчезнет навсегда!
        ////// ====================================================================
        //////var ecb = new EntityCommandBuffer(Allocator.TempJob);
        ////var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        ////var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        ////// ====================================================================

        //// ====================================================================
        //// ЭТАП 1 (BURST): АСИНХРОННЫЙ ПРОЛЕТ ПО ВСЕМ ИЗМЕНИВШИМСЯ ЧАНКАМ
        //// ====================================================================
        //foreach (var (maskBuffer, chunkIndex, modelHeader, ghostInstance, entity) in SystemAPI.Query<
        //        DynamicBuffer<LocalChunkDestructionMask>,
        //        RefRO<ChunkIndexComponent>,
        //        RefRO<VoxelModelHeader>,
        //        RefRO<GhostInstance>
        //    >()
        //     .WithDisabled<ChunkColliderData>()
        //     .WithDisabled<ChunkActiveState>()
        //     .WithEntityAccess())
        //{
        //    // ====================================================================
        //    // ЖЕЛЕЗОБЕТОННЫЙ СЕТЕВОЙ ПРЕДОХРАНИТЕЛЬ:
        //    // Если Netcode на этом кадре уже пометил чанк на удаление — 
        //    // КАТЕГОРИЧЕСКИ пропускаем его и не трогаем память!
        //    // ====================================================================
        //    if (!state.EntityManager.Exists(entity)) continue;

        //    Entity rootVehicleEntity = Entity.Null;

        //    // ====================================================================
        //    // Получаем корень автомобиля (предполагаем, что у вас есть компонент родителя, например Parent)
        //    if (m_ParentLookup.HasComponent(entity))
        //    {
        //        //rootVehicleEntity = state.EntityManager.GetComponentData<Parent>(entity).Value;
        //        rootVehicleEntity = m_ParentLookup[entity].Value;
        //    }
        //    if (rootVehicleEntity == Entity.Null) continue;
        //    //==========================================

        //    if (!m_GhostInstanceLookup.HasComponent(rootVehicleEntity)) continue;

        //    //var ghostInstanceRoot = m_GhostInstanceLookup[rootVehicleEntity];
        //    //NetworkTick spawnTick = ghostInstanceRoot.spawnTick;

        //    //// Проверяем валидность обоих тиков
        //    //if (currentTick.IsValid && spawnTick.IsValid)
        //    //{
        //    //    uint currentTickIdx = currentTick.TickIndexForValidTick;
        //    //    uint spawnTickIdx = spawnTick.TickIndexForValidTick;
        //    //    uint ticksPassed = currentTickIdx - spawnTickIdx;

        //    //    // Если машина слишком молодая — отправляем чанк на карантин
        //    //    if (ticksPassed >= 0u && ticksPassed < 4u)
        //    //    {
        //    //        continue; // Чистый пропуск итерации!
        //    //    }
        //    //}
        //    // Извлекаем тик спавна САМОГО ЧАНКА из переменной итератора query
        //    NetworkTick chunkSpawnTick = ghostInstance.ValueRO.spawnTick;

        //    if (currentTick.IsValid && chunkSpawnTick.IsValid)
        //    {
        //        uint currentTickIdx = currentTick.TickIndexForValidTick;
        //        uint chunkSpawnTickIdx = chunkSpawnTick.TickIndexForValidTick;
        //        uint ticksPassed = currentTickIdx - chunkSpawnTickIdx;

        //        // Если САМ ЧАНК слишком молодой (меньше 4 тиков) — отправляем его на карантин
        //        if (ticksPassed >= 0 && ticksPassed < 4)
        //        {
        //            continue; // Строго ПРОПУСКАЕМ этот чанк, давая Netcode время уложить его в Ghost Map!
        //        }
        //    }

        //    // ====================================================================
        //    // ОФИЦИАЛЬНЫЙ СЕТЕВОЙ Safe-ПРЕДOХРАНИТЕЛЬ (Канон Unity Netcode):
        //    // Мы извлекаем компонент GhostInstance. Если этот чанк родился прямо 
        //    // на текущем кадре симуляции — мы КАТЕГОРИЧЕСКИ пропускаем его!
        //    // Даем С++ ядру GhostReceiveSystem спокойно стабилизировать и настроить 
        //    // Ghost Map без фоновых блокировок воркеров CPU.
        //    // На следующем кадре чанк станет "взрослым", воркеры асинхронно допекут 
        //    // его без единого краха, а фриз и ошибка полностью ИСЧЕЗНУТ при любом спавне!
        //    // ====================================================================
        //    // Проверяем сетевой статус госта через встроенные unmanaged-флаги Netcode
        //    if (ghostInstance.ValueRO.ghostId <= 0) // Или простая проверка на возраст тика кадра
        //    {
        //        // Самый надежный Safe-вариант для Burst: если гост только что заспавнился на клиенте, 
        //        // даем ему 1 кадр на сетевую акклиматизацию
        //        continue;
        //    }

        //    ArchetypeChunk chunk = state.EntityManager.GetChunk(entity);
        //    uint chunkMaskVersion = chunk.GetChangeVersion(ref m_MaskTypeHandle);

        //    bool hasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(entity);
        //    bool isMaskChanged = ChangeVersionUtility.DidChange(chunkMaskVersion, lastSystemVersion);

        //    if (hasGraphics && !isMaskChanged) continue;

        //    uint modelHash = modelHeader.ValueRO.ConfigHashName;
        //    if (modelHash == 0) continue;

        //    if (!cache.Templates.TryGetValue(modelHash, out var template)) continue;
        //    if (!template.ChunkCoordToOrderIndexMap.TryGetValue(chunkIndex.ValueRO.Value, out int chunkOrderIndex)) continue;

        //    int chunkOffset = chunkOrderIndex * 32768;

        //    // ====================================================================
        //    // КРИТИЧЕСКИЙ ПРЕДОХРАНИТЕЛЬ: МГНОВЕННАЯ БЛОКИРОВКА ЧАНКА!
        //    // Включаем ChunkActiveState прямо здесь, на месте! 
        //    // state.EntityManager.SetComponentEnabled — это НЕ структурное изменение!
        //    // Оно легально работает внутри foreach и мгновенно выкидывает чанк из 
        //    // этого запроса на все следующие кадры, пока джобы не завершатся!
        //    // ====================================================================
        //    //state.EntityManager.SetComponentEnabled<ChunkActiveState>(entity, true);
        //    m_ActiveStateLookup.SetComponentEnabled(entity, true);
        //    // ====================================================================

        //    // Выделяем раздельные unmanaged safe-буферы для джобы в Persistent куче кадра
        //    var tempVertices = new NativeArray<VoxelVertex>(16384, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        //    var tempIndices = new NativeArray<int>(24576, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        //    var singleChunkCounter = new NativeArray<int2>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        //    //==============Physics====================
        //    // НА 100% СЕЙФОВЫЙ МАНЕВР: Персональный мини-массив коллайдера для КАЖДОГО чанка!
        //    // Никаких GetSubArray и общих ссылок. Полная изоляция памяти.
        //    // ====================================================================
        //    var singleChunkColliderBlob = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        //    // ====================================================================
        //    //UnityEngine.Debug.LogWarning($"tempVertices.lenght={tempVertices.Length}");
        //    // 2. Планируем джобу выпекания MeshCollider для этого чанка
        //    var colliderJob = new GenerateChunkColliderJob
        //    {
        //        LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
        //        OutputColliderBlob = singleChunkColliderBlob,
        //    };

        //    //var colliderJob = new PhysicsGreedyJobSafeDirect
        //    //{
        //    //    LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
        //    //    OutputColliderBlob = singleChunkColliderBlob,
        //    //    ChunkOffsetInFlattenedArray = chunkOffset,
        //    //    FlattenedModelColors = template.FlattenedLinearColors,
        //    //};

        //    JobHandle chunkColliderHandle = colliderJob.Schedule(state.Dependency);

        //    m_JobHandles.Add(chunkColliderHandle);

        //    // 1. Вычисляем исходное локальное смещение чанка (размер чанка 32 вокселя)
        //    float3 baseLocalOffset = (float3)chunkIndex.ValueRO.Value * 32f * 1.0f;

        //    // ====================================================================
        //    // ЖЕЛЕЗОБЕТОННЫЙ ФИКС ПИВОТА ДЛЯ ФИЗИКИ:
        //    // Вычисляем точно такой же pivotOffset, как и в спавнере чанков!
        //    // Замените modelSizeInChunks на реальные габариты вашей модели в чанках.
        //    float3 pivotOffset = new float3(
        //        (template.SizeModel.x * 32f) / 2f,
        //        0f, // Оставляем 0, чтобы физическое дно машины совпало с пивотом (0,5,0)
        //        (template.SizeModel.z * 32f) / 2f
        //    );

        //    // Вычитаем смещение, чтобы сдвинуть физический бокс коллайдера чанка вслед за графикой!
        //    float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

        //    // Записываем СМЕЩЕННЫЙ оффсет в массив для генерации CompoundCollider
        //    childOffsetsArray[jobIndex] = localOffsetWithPivot;
        //    // ====================================================================


        //    //// Считаем Bounds через чистый unmanaged AABB.Transform без managed-вызовов
        //    //var aabbLocal = new Unity.Mathematics.AABB { Center = new float3(16f, 16f, 16f), Extents = new float3(16f, 16f, 16f) };
        //    //var aabbWorld = aabbLocal;
        //    //if (state.EntityManager.HasComponent<LocalToWorld>(entity))
        //    //{
        //    //    var ltw = state.EntityManager.GetComponentData<LocalToWorld>(entity);
        //    //    aabbWorld = Unity.Mathematics.AABB.Transform(ltw.Value, aabbLocal);
        //    //}
        //    // 2. СЧИТАЕМ BOUNDS С УЧЕТОМ НОВОГО ЛОКАЛЬНОГО ПОЛОЖЕНИЯ
        //    // Локальный центр чанка в пространстве машины теперь равен: смещенный оффсет чанка + центр самого чанка (16,16,16)
        //    float3 correctedChunkCenterInVehicleSpace = localOffsetWithPivot + new float3(16f, 16f, 16f);

        //    var aabbLocal = new Unity.Mathematics.AABB
        //    {
        //        Center = correctedChunkCenterInVehicleSpace,
        //        Extents = new float3(16f, 16f, 16f)
        //    };

        //    var aabbWorld = aabbLocal;

        //    // Если у машины (а не у чанка!) есть LocalToWorld, трансформируем локальный AABB в мировой
        //    if (state.EntityManager.HasComponent<LocalToWorld>(rootVehicleEntity))
        //    {
        //        var parentLtw = state.EntityManager.GetComponentData<LocalToWorld>(rootVehicleEntity);
        //        aabbWorld = Unity.Mathematics.AABB.Transform(parentLtw.Value, aabbLocal);
        //    }


        //    // ====================================================================
        //    // БЕЗОПАСНАЯ РЕГИСТРАЦИЯ НА СУЩНОСТИ ЧАНКА:
        //    // Откладываем добавление компонента-маркера выгрузки графики в ECB.
        //    // OnUpdate завершается мгновенно, оставаясь на 100% BurstCompile-чистым!
        //    // ====================================================================
        //    // Теперь мы со спокойной душой перезаписываем маркер свежими массивами текущего кадра!
        //    m_ChunkColliderDataLookup[entity] = new ChunkColliderData
        //    {
        //        LastBakingJobHandle = chunkColliderHandle,
        //        SafeColliderBlob = singleChunkColliderBlob,
        //        RootVehicleEntity = rootVehicleEntity,
        //        LocalOffsetWithPivot = localOffsetWithPivot,
        //        LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
        //        WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
        //        HasGraphicsBefore = hasGraphics,
        //        index = chunkIndex.ValueRO.Value
        //    };

        //    //var isClient = state.WorldUnmanaged.IsClient();
        //    //string textWorld = isClient ? "Client" : "Server";

        //    //UnityEngine.Debug.Log($"[{textWorld}] Добавление ChunkGraphicsFlushTag для {chunkIndex.ValueRO.Value}");

        //    //// Если маркер уже был — обновляем его структуру атомарно, если нет — добавляем
        //    //if (state.EntityManager.HasComponent<ChunkGraphicsFlushTag>(entity))
        //    //{
        //    //    ecb.SetComponent(entity, finalFlushComponent);
        //    //}
        //    //else
        //    //{
        //    //    ecb.AddComponent(entity, finalFlushComponent);
        //    //}
        //    m_ChunkColliderDataLookup.SetComponentEnabled(entity, true);

        //    jobIndex++;
        //}

        //if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();

        //// ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        //if (m_JobHandles.Length == 0)
        //{
        //    // Если в этом кадре разрушений не было — просто сбрасываем список
        //    m_JobHandles.Clear();

        //    // Чистим пустую записную книжку кадра, чтобы не было утечек памяти
        //    //if (ecb.IsCreated) ecb.Dispose();
        //    return;
        //}

        //// ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        //if (m_JobHandles.Length == 0) return;

        //// ====================================================================
        //// Объединяем хэндлы асинхронного выпекания ВСЕХ индивидуальных чанков.
        //// ====================================================================
        //JobHandle allChunksBakedHandle = JobHandle.CombineDependencies(m_JobHandles.AsArray());
        //// Передаем итоговую чистую зависимость кадра в систему
        //state.Dependency = allChunksBakedHandle;

    }
}
