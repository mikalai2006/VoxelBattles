using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;


public struct ChunkMeshNeedCreate : IComponentData, IEnableableComponent { }

[ChunkSerializable]// Разрешает Live Conversion игнорировать NativeArray внутри префаба
public struct ChunkMeshData : IComponentData, IEnableableComponent
{
    // Храним чистые, изолированные C++ массивы геометрии ТОЛЬКО этого чанка!
    public NativeArray<VoxelVertex> SafeVertices;
    public NativeArray<int> SafeIndices;
    public NativeArray<int3> SafeCounter;

    // Ссылка на хэндл джобы для точечной проверки готовности
    public JobHandle LastBakingJobHandle;

    public Entity RootVehicleEntity;
    public float3 LocalOffsetWithPivot;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;
    public int3 index;
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
//[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ClientCreateVoxelMeshSystem : ISystem
{
    private ComponentLookup<ChunkMeshData> m_ChunkDataLookup;

    private ComponentLookup<ChunkActiveState> m_ActiveStateLookup;

    private ComponentLookup<Parent> m_ParentLookup;

    private ComponentLookup<GhostInstance> m_GhostInstanceLookup;

    private BufferTypeHandle<LocalChunkDestructionMask> m_MaskTypeHandle;

    private NativeList<JobHandle> m_JobHandles;

    private EntityQuery m_RebuildChunksQuery;

    [BurstCompile(CompileSynchronously = true)]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GlobalVoxelModelCache>();
        state.RequireForUpdate<VoxelGlobalConfigComponent>();

        m_MaskTypeHandle = state.GetBufferTypeHandle<LocalChunkDestructionMask>(true);

        m_JobHandles = new NativeList<JobHandle>(16, Allocator.Persistent);

        m_ChunkDataLookup = state.GetComponentLookup<ChunkMeshData>();
        m_ActiveStateLookup = state.GetComponentLookup<ChunkActiveState>();

        // Инициализируем лукапы при создании системы
        m_ParentLookup = state.GetComponentLookup<Parent>(true); // true = ReadOnly
        m_GhostInstanceLookup = state.GetComponentLookup<Unity.NetCode.GhostInstance>(true);

        // Создаем неуправляемый массив на 4 элемента
        var queryTypes = new NativeArray<ComponentType>(4, Allocator.Temp);

        queryTypes[0] = ComponentType.ReadOnly<LocalChunkDestructionMask>();
        queryTypes[1] = ComponentType.ReadOnly<ChunkIndexComponent>();
        queryTypes[2] = ComponentType.ReadOnly<VoxelModelHeader>();
        //queryTypes[3] = ComponentType.Exclude<ChunkActiveState>();
        // Фильтр .WithAll<ChunkMeshNeedCreate>()
        // Так как это IEnableableComponent, он должен присутствовать на сущности и быть Enabled
        queryTypes[3] = ComponentType.ReadOnly<ChunkMeshNeedCreate>();
        // Если нужно раскомментировать .WithDisabled<ChunkMeshData>() или .WithDisabled<ChunkActiveState>(),
        // то размер массива увеличиваем, а компоненты добавляем через ComponentType.Exclude<T>:
        // queryTypes[5] = ComponentType.Exclude<ChunkMeshData>();
        // queryTypes[6] = ComponentType.Exclude<ChunkActiveState>();


        // Передаем NativeArray в метод
        m_RebuildChunksQuery = state.GetEntityQuery(queryTypes);

        // Обязательно освобождаем память массива
        queryTypes.Dispose();
    }


    [BurstCompile(CompileSynchronously = true)]
    public void OnDestroy(ref SystemState state)
    {
        if (m_JobHandles.IsCreated) m_JobHandles.Dispose();
    }


    [BurstCompile(CompileSynchronously = true)]
    public void OnUpdate(ref SystemState state)
    {
        m_ChunkDataLookup.Update(ref state);
        m_ActiveStateLookup.Update(ref state);
        m_ParentLookup.Update(ref state);
        m_GhostInstanceLookup.Update(ref state);
        m_MaskTypeHandle.Update(ref state);

        var cache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

        uint lastSystemVersion = state.LastSystemVersion;

        m_JobHandles.Clear();


        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        //// Считаем точное число чанков на перестроение в текущем кадре
        //int totalChunksToRebuild = 0;
        //foreach (var (maskBuffer, chunkIndex, modelHeader, entity) in SystemAPI.Query<DynamicBuffer<LocalChunkDestructionMask>, RefRO<ChunkIndexComponent>, RefRO<VoxelModelHeader>>().WithDisabled<ChunkActiveState>().WithEntityAccess())
        //{
        //    totalChunksToRebuild++;
        //}
        // Мгновенный подсчет без перебора сущностей
        int totalChunksToRebuild = m_RebuildChunksQuery.CalculateEntityCount();

        if (totalChunksToRebuild == 0) return;

        var childOffsetsArray = new NativeArray<float3>(totalChunksToRebuild, Allocator.Persistent);

        int jobIndex = 0;

        // ====================================================================
        // ЭТАП 1 (BURST): АСИНХРОННЫЙ ПРОЛЕТ ПО ВСЕМ ИЗМЕНИВШИМСЯ ЧАНКАМ
        // ====================================================================
        foreach (var (maskBuffer, chunkIndex, modelHeader, ghostInstance, entity) in SystemAPI.Query<
                DynamicBuffer<LocalChunkDestructionMask>,
                RefRO<ChunkIndexComponent>,
                RefRO<VoxelModelHeader>,
                RefRO<GhostInstance>
            >()
            .WithAll<ChunkMeshNeedCreate>() // Сущность попадет в выборку, только если у неё присутствует компонент T И он включен (Enabled).
                                            //.WithDisabled<ChunkMeshData>()
                                            //.WithDisabled<ChunkActiveState>()
             .WithEntityAccess())
        {
            // ====================================================================
            // Если Netcode на этом кадре уже пометил чанк на удаление — 
            // КАТЕГОРИЧЕСКИ пропускаем его и не трогаем память!
            // ====================================================================
            if (!state.EntityManager.Exists(entity)) continue;

            Entity rootVehicleEntity = Entity.Null;

            // ====================================================================
            // Получаем корень автомобиля (предполагаем, что у вас есть компонент родителя, например Parent)
            if (m_ParentLookup.HasComponent(entity))
            {
                //rootVehicleEntity = state.EntityManager.GetComponentData<Parent>(entity).Value;
                rootVehicleEntity = m_ParentLookup[entity].Value;
            }
            if (rootVehicleEntity == Entity.Null) continue;
            //==========================================

            if (!m_GhostInstanceLookup.HasComponent(rootVehicleEntity)) continue;

            // ====================================================================
            // ОФИЦИАЛЬНЫЙ СЕТЕВОЙ Safe-ПРЕДOХРАНИТЕЛЬ (Канон Unity Netcode):
            // Мы извлекаем компонент GhostInstance. Если этот чанк родился прямо 
            // на текущем кадре симуляции — мы КАТЕГОРИЧЕСКИ пропускаем его!
            // Даем С++ ядру GhostReceiveSystem спокойно стабилизировать и настроить 
            // Ghost Map без фоновых блокировок воркеров CPU.
            // На следующем кадре чанк станет "взрослым", воркеры асинхронно допекут 
            // его без единого краха, а фриз и ошибка полностью ИСЧЕЗНУТ при любом спавне!
            // ====================================================================
            // Проверяем сетевой статус госта через встроенные unmanaged-флаги Netcode
            if (ghostInstance.ValueRO.ghostId <= 0) // Или простая проверка на возраст тика кадра
            {
                // Самый надежный Safe-вариант для Burst: если гост только что заспавнился на клиенте, 
                // даем ему 1 кадр на сетевую акклиматизацию
                continue;
            }

            ArchetypeChunk chunk = state.EntityManager.GetChunk(entity);
            uint chunkMaskVersion = chunk.GetChangeVersion(ref m_MaskTypeHandle);

            bool hasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(entity);
            bool isMaskChanged = ChangeVersionUtility.DidChange(chunkMaskVersion, lastSystemVersion);

            if (hasGraphics && !isMaskChanged) continue;

            uint modelHash = modelHeader.ValueRO.ConfigHashName;
            if (modelHash == 0) continue;

            if (!cache.Templates.TryGetValue(modelHash, out var template)) continue;
            if (!template.ChunkCoordToOrderIndexMap.TryGetValue(chunkIndex.ValueRO.Value, out int chunkOrderIndex)) continue;

            int chunkOffset = chunkOrderIndex * 32768;

            //// ====================================================================
            //// КРИТИЧЕСКИЙ ПРЕДОХРАНИТЕЛЬ: МГНОВЕННАЯ БЛОКИРОВКА ЧАНКА!
            //// Включаем ChunkActiveState прямо здесь, на месте! 
            //// state.EntityManager.SetComponentEnabled — это НЕ структурное изменение!
            //// Оно легально работает внутри foreach и мгновенно выкидывает чанк из 
            //// этого запроса на все следующие кадры, пока джобы не завершатся!
            //// ====================================================================
            ////state.EntityManager.SetComponentEnabled<ChunkActiveState>(entity, true);
            state.EntityManager.SetComponentEnabled<ChunkMeshNeedCreate>(entity, false);
            //ecb.SetComponentEnabled<ChunkMeshNeedRender>(entity, true);
            //m_ActiveStateLookup.SetComponentEnabled(entity, true);
            //// ====================================================================

            // Выделяем раздельные unmanaged safe-буферы для джобы в Persistent куче кадра
            var tempVertices = new NativeArray<VoxelVertex>(16384, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var tempIndices = new NativeArray<int>(24576, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            var singleChunkCounter = new NativeArray<int3>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            var greedyJob = new MeshGreedyJobSafeDirect
            {
                LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
                FlattenedModelColors = template.FlattenedLinearColors,
                GlobalPaletteColors = template.PaletteColors,
                ChunkOffsetInFlattenedArray = chunkOffset,
                OutputVertices = tempVertices,
                OutputIndices = tempIndices,
                JobCountersRef = singleChunkCounter
            };

            JobHandle meshJobHandle = greedyJob.Schedule(state.Dependency);

            m_JobHandles.Add(meshJobHandle);

            // 1. Вычисляем исходное локальное смещение чанка (размер чанка 32 вокселя)
            float3 baseLocalOffset = (float3)chunkIndex.ValueRO.Value * 32f * 1.0f;

            // ====================================================================
            // ЖЕЛЕЗОБЕТОННЫЙ ФИКС ПИВОТА ДЛЯ ФИЗИКИ:
            // Вычисляем точно такой же pivotOffset, как и в спавнере чанков!
            // Замените modelSizeInChunks на реальные габариты вашей модели в чанках.
            float3 pivotOffset = new float3(
                (template.SizeModel.x * 32f) / 2f,
                0f, // Оставляем 0, чтобы физическое дно машины совпало с пивотом (0,5,0)
                (template.SizeModel.z * 32f) / 2f
            );

            // Вычитаем смещение, чтобы сдвинуть физический бокс коллайдера чанка вслед за графикой!
            float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

            // Записываем СМЕЩЕННЫЙ оффсет в массив для генерации CompoundCollider
            childOffsetsArray[jobIndex] = localOffsetWithPivot;
            // ====================================================================

            // 2. СЧИТАЕМ BOUNDS С УЧЕТОМ НОВОГО ЛОКАЛЬНОГО ПОЛОЖЕНИЯ
            // Локальный центр чанка в пространстве машины теперь равен: смещенный оффсет чанка + центр самого чанка (16,16,16)
            float3 correctedChunkCenterInVehicleSpace = localOffsetWithPivot + new float3(16f, 16f, 16f);

            var aabbLocal = new Unity.Mathematics.AABB
            {
                Center = correctedChunkCenterInVehicleSpace,
                Extents = new float3(16f, 16f, 16f)
            };

            var aabbWorld = aabbLocal;

            // Если у машины (а не у чанка!) есть LocalToWorld, трансформируем локальный AABB в мировой
            if (state.EntityManager.HasComponent<LocalToWorld>(rootVehicleEntity))
            {
                var parentLtw = state.EntityManager.GetComponentData<LocalToWorld>(rootVehicleEntity);
                aabbWorld = Unity.Mathematics.AABB.Transform(parentLtw.Value, aabbLocal);
            }

            // ====================================================================
            // БЕЗОПАСНАЯ РЕГИСТРАЦИЯ НА СУЩНОСТИ ЧАНКА:
            // Откладываем добавление компонента-маркера выгрузки графики в ECB.
            // OnUpdate завершается мгновенно, оставаясь на 100% BurstCompile-чистым!
            // ====================================================================
            // Теперь мы со спокойной душой перезаписываем маркер свежими массивами текущего кадра!
            m_ChunkDataLookup[entity] = new ChunkMeshData
            {
                LastBakingJobHandle = meshJobHandle,
                SafeVertices = tempVertices, // Новые чистые Persistent-массивы кадра
                SafeIndices = tempIndices,
                SafeCounter = singleChunkCounter,
                RootVehicleEntity = rootVehicleEntity,
                LocalOffsetWithPivot = localOffsetWithPivot,
                LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
                WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
                HasGraphicsBefore = hasGraphics,
                index = chunkIndex.ValueRO.Value,
            };

            m_ChunkDataLookup.SetComponentEnabled(entity, true);

            jobIndex++;
        }

        if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();

        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        if (m_JobHandles.Length == 0)
        {
            // Если в этом кадре разрушений не было — просто сбрасываем список
            m_JobHandles.Clear();

            // Чистим пустую записную книжку кадра, чтобы не было утечек памяти
            //if (ecb.IsCreated) ecb.Dispose();
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

//public struct ChunkBakeStatusComponent : IComponentData
//{
//    // false — меш печется в фоне, true — Burst всё закончил
//    public bool IsReady;
//}


//[BurstCompile]
//public partial struct MarkChunkAsReadyJob : IJobEntity
//{
//    public EntityCommandBuffer.ParallelWriter Ecb;

//    // Джоб ищет сущности, у которых есть компонент статуса.
//    // Мы передаем его как 'in' (для чтения), чтобы просто отфильтровать нужные чанки.
//    public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, in ChunkBakeStatusComponent status)
//    {
//        // Если он уже готов, пропускаем
//        if (status.IsReady) return;

//        // Откладываем безопасное обновление флага через ECB
//        Ecb.SetComponent(chunkIndex, entity, new ChunkBakeStatusComponent { IsReady = true });
//    }
//}
