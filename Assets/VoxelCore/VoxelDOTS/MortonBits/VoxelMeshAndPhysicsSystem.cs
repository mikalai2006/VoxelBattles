//using System.Collections.Generic;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Physics;
//using Unity.Rendering;
//using Unity.Transforms;
//using UnityEngine;

//public struct NewChunkGraphicsData
//{
//    public Entity TargetEntity;
//    public int JobIndex;
//    public MinMaxAABB LocalBounds;
//    public MinMaxAABB WorldBounds;
//    public bool HasGraphicsBefore;

//    // ДОБАВЛЯЕМ: Ссылки на массивы для безопасного С++ копирования
//    public NativeArray<VoxelVertex> SafeVertices;
//    public NativeArray<int> SafeIndices;

//    // ДОБАВЛЯЕМ: Ссылка на персональный массив счетчика
//    public NativeArray<int2> SafeCounter;

//    // МЕНЯЕМ ТИП: Сюда мы сохраним индивидуальный нативный массив чанка
//    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> SafeColliderBlob;
//}

//// 1. УПРАВЛЯЕМЫЙ КОМПОНЕНТ ДАННЫХ ДЛЯ ХРАНЕНИЯ ССЫЛОК В КЭШЕ КАДРА
//public class ClientVoxelMeshFrameStorage : IComponentData
//{
//    public List<Mesh> RuntimeMeshes = new List<Mesh>();
//    public List<Mesh.MeshDataArray> DataArrays = new List<Mesh.MeshDataArray>();
//    public List<Entity> TargetEntities = new List<Entity>();
//}

////[UpdateInGroup(typeof(SimulationSystemGroup))]
////[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
//[BurstCompile]
//public partial struct VoxelMeshAndPhysicsSystem : ISystem
//{
//    private ComponentLookup<ChunkGraphicsFlushTag> m_FlushTagLookup;

//    private ComponentLookup<ChunkActiveState> m_ActiveStateLookup;

//    // Внутри объявления структуры вашей системы (ISystem):
//    ComponentLookup<Parent> m_ParentLookup;
//    ComponentLookup<Unity.NetCode.GhostInstance> m_GhostInstanceLookup;

//    private BufferTypeHandle<LocalChunkDestructionMask> m_MaskTypeHandle;

//    private NativeList<JobHandle> m_JobHandles;
//    //private NativeList<NewChunkGraphicsData> m_ChunksToInitialize;

//    //// 1. Объявляем приватные поля для кэширования запросов
//    //private EntityQuery m_ConfigQuery;
//    //private EntityQuery m_StorageQuery;

//    //[BurstCompile(CompileSynchronously = true)]
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<GlobalVoxelModelCache>();
//        state.RequireForUpdate<VoxelGlobalConfigComponent>();

//        m_MaskTypeHandle = state.GetBufferTypeHandle<LocalChunkDestructionMask>(true);

//        m_JobHandles = new NativeList<JobHandle>(16, Allocator.Persistent);
//        //m_ChunksToInitialize = new NativeList<NewChunkGraphicsData>(16, Allocator.Persistent);

//        // Регистрируем managed-хранилище в ECS-мире один раз при старте
//        Entity storageEntity = state.EntityManager.CreateEntity();
//        state.EntityManager.AddComponentObject(storageEntity, new ClientVoxelMeshFrameStorage());

//        //// 2. Создаем запросы ОДИН РАЗ при инициализации системы
//        //m_ConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<VoxelGlobalConfigComponent>());
//        //m_StorageQuery = state.GetEntityQuery(ComponentType.ReadOnly<ClientVoxelMeshFrameStorage>());

//        m_FlushTagLookup = state.GetComponentLookup<ChunkGraphicsFlushTag>();
//        m_ActiveStateLookup = state.GetComponentLookup<ChunkActiveState>();

//        // Инициализируем лукапы при создании системы
//        m_ParentLookup = state.GetComponentLookup<Parent>(true); // true = ReadOnly
//        m_GhostInstanceLookup = state.GetComponentLookup<Unity.NetCode.GhostInstance>(true);
//    }


//    [BurstCompile(CompileSynchronously = true)]
//    public void OnDestroy(ref SystemState state)
//    {
//        if (m_JobHandles.IsCreated) m_JobHandles.Dispose();
//    }

//    [BurstCompile(CompileSynchronously = true)]
//    public void OnUpdate(ref SystemState state)
//    {
//        // 1. В начале OnUpdate сохраняем ТЕКУЩИЙ сетевой тик как структуру NetworkTick
//        if (!SystemAPI.HasSingleton<NetworkTime>()) return;
//        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
//        NetworkTick currentTick = networkTime.ServerTick;

//        //// Свойство IsFirstTimePredictedTick сообщает, что Netcode сейчас выполняет 
//        //// "свежий" кадр, а не прокручивает историю тиков назад для ресимуляции.
//        //// Если это повторный тик перемотки — мы мгновенно выходим и не дублируем код!
//        //if (!networkTime.IsFinalPredictionTick)
//        //{
//        //    return;
//        //}

//        m_FlushTagLookup.Update(ref state);
//        m_ActiveStateLookup.Update(ref state);
//        m_ParentLookup.Update(ref state);
//        m_GhostInstanceLookup.Update(ref state);

//        // Буфер команд
//        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
//        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//        var cache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

//        m_MaskTypeHandle.Update(ref state);
//        uint lastSystemVersion = state.LastSystemVersion;

//        m_JobHandles.Clear();
//        //m_ChunksToInitialize.Clear();

//        // Считаем точное число чанков на перестроение в текущем кадре
//        int totalChunksToRebuild = 0;
//        foreach (var (maskBuffer, chunkIndex, modelHeader, entity) in SystemAPI.Query<DynamicBuffer<LocalChunkDestructionMask>, RefRO<ChunkIndexComponent>, RefRO<VoxelModelHeader>>().WithDisabled<ChunkActiveState>().WithEntityAccess())
//        {
//            totalChunksToRebuild++;
//        }

//        if (totalChunksToRebuild == 0) return;

//        // ====================================================================
//        // ЧИСТЫЙ AAA SAFE ПАЙПЛАЙН: Выделяем ТОЛЬКО три этих массива!
//        // Старый childCollidersList КАТЕГОРИЧЕСКИ УДАЛЕН со строки 79!
//        // ====================================================================
//        var childOffsetsArray = new NativeArray<float3>(totalChunksToRebuild, Allocator.Persistent);
//        // ====================================================================


//        int jobIndex = 0;



//        //// ====================================================================
//        //// ФАЗА Б: ЗАПУСК НОВЫХ РАЗРУШЕНИЙ ИЛИ СПАВНОВ ЧАНКОВ
//        //// ЖЕЛЕЗОБЕТОННЫЙ SAFE-ФИКС ДЛЯ PresentationSystemGroup:
//        //// Мы КАТЕГОРИЧЕСКИ стерли отсюда GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()!
//        //// Создаем чистый, изолированный unmanaged буфер команд на аллокаторе TempJob.
//        //// Ошибка "EntityCommandBuffer has been deallocated" исчезнет навсегда!
//        //// ====================================================================
//        ////var ecb = new EntityCommandBuffer(Allocator.TempJob);
//        //var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
//        //// ====================================================================

//        // ====================================================================
//        // ЭТАП 1 (BURST): АСИНХРОННЫЙ ПРОЛЕТ ПО ВСЕМ ИЗМЕНИВШИМСЯ ЧАНКАМ
//        // ====================================================================
//        foreach (var (maskBuffer, chunkIndex, modelHeader, ghostInstance, entity) in SystemAPI.Query<
//                DynamicBuffer<LocalChunkDestructionMask>,
//                RefRO<ChunkIndexComponent>,
//                RefRO<VoxelModelHeader>,
//                RefRO<GhostInstance>
//            >()
//             .WithDisabled<ChunkGraphicsFlushTag>()
//             .WithDisabled<ChunkActiveState>()
//             .WithEntityAccess())
//        {
//            // ====================================================================
//            // ЖЕЛЕЗОБЕТОННЫЙ СЕТЕВОЙ ПРЕДОХРАНИТЕЛЬ:
//            // Если Netcode на этом кадре уже пометил чанк на удаление — 
//            // КАТЕГОРИЧЕСКИ пропускаем его и не трогаем память!
//            // ====================================================================
//            if (!state.EntityManager.Exists(entity)) continue;

//            Entity rootVehicleEntity = Entity.Null;

//            // ====================================================================
//            // Получаем корень автомобиля (предполагаем, что у вас есть компонент родителя, например Parent)
//            if (m_ParentLookup.HasComponent(entity))
//            {
//                //rootVehicleEntity = state.EntityManager.GetComponentData<Parent>(entity).Value;
//                rootVehicleEntity = m_ParentLookup[entity].Value;
//            }
//            if (rootVehicleEntity == Entity.Null) continue;
//            //==========================================

//            if (!m_GhostInstanceLookup.HasComponent(rootVehicleEntity)) continue;

//            //var ghostInstanceRoot = m_GhostInstanceLookup[rootVehicleEntity];
//            //NetworkTick spawnTick = ghostInstanceRoot.spawnTick;

//            //// Проверяем валидность обоих тиков
//            //if (currentTick.IsValid && spawnTick.IsValid)
//            //{
//            //    uint currentTickIdx = currentTick.TickIndexForValidTick;
//            //    uint spawnTickIdx = spawnTick.TickIndexForValidTick;
//            //    uint ticksPassed = currentTickIdx - spawnTickIdx;

//            //    // Если машина слишком молодая — отправляем чанк на карантин
//            //    if (ticksPassed >= 0u && ticksPassed < 4u)
//            //    {
//            //        continue; // Чистый пропуск итерации!
//            //    }
//            //}
//            // Извлекаем тик спавна САМОГО ЧАНКА из переменной итератора query
//            NetworkTick chunkSpawnTick = ghostInstance.ValueRO.spawnTick;

//            if (currentTick.IsValid && chunkSpawnTick.IsValid)
//            {
//                uint currentTickIdx = currentTick.TickIndexForValidTick;
//                uint chunkSpawnTickIdx = chunkSpawnTick.TickIndexForValidTick;
//                uint ticksPassed = currentTickIdx - chunkSpawnTickIdx;

//                // Если САМ ЧАНК слишком молодой (меньше 4 тиков) — отправляем его на карантин
//                if (ticksPassed >= 0 && ticksPassed < 4)
//                {
//                    continue; // Строго ПРОПУСКАЕМ этот чанк, давая Netcode время уложить его в Ghost Map!
//                }
//            }

//            // ====================================================================
//            // ОФИЦИАЛЬНЫЙ СЕТЕВОЙ Safe-ПРЕДOХРАНИТЕЛЬ (Канон Unity Netcode):
//            // Мы извлекаем компонент GhostInstance. Если этот чанк родился прямо 
//            // на текущем кадре симуляции — мы КАТЕГОРИЧЕСКИ пропускаем его!
//            // Даем С++ ядру GhostReceiveSystem спокойно стабилизировать и настроить 
//            // Ghost Map без фоновых блокировок воркеров CPU.
//            // На следующем кадре чанк станет "взрослым", воркеры асинхронно допекут 
//            // его без единого краха, а фриз и ошибка полностью ИСЧЕЗНУТ при любом спавне!
//            // ====================================================================
//            // Проверяем сетевой статус госта через встроенные unmanaged-флаги Netcode
//            if (ghostInstance.ValueRO.ghostId <= 0) // Или простая проверка на возраст тика кадра
//            {
//                // Самый надежный Safe-вариант для Burst: если гост только что заспавнился на клиенте, 
//                // даем ему 1 кадр на сетевую акклиматизацию
//                continue;
//            }

//            ArchetypeChunk chunk = state.EntityManager.GetChunk(entity);
//            uint chunkMaskVersion = chunk.GetChangeVersion(ref m_MaskTypeHandle);

//            bool hasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(entity);
//            bool isMaskChanged = ChangeVersionUtility.DidChange(chunkMaskVersion, lastSystemVersion);

//            if (hasGraphics && !isMaskChanged) continue;

//            uint modelHash = modelHeader.ValueRO.ConfigHashName;
//            if (modelHash == 0) continue;

//            if (!cache.Templates.TryGetValue(modelHash, out var template)) continue;
//            if (!template.ChunkCoordToOrderIndexMap.TryGetValue(chunkIndex.ValueRO.Value, out int chunkOrderIndex)) continue;

//            int chunkOffset = chunkOrderIndex * 32768;

//            // ====================================================================
//            // КРИТИЧЕСКИЙ ПРЕДОХРАНИТЕЛЬ: МГНОВЕННАЯ БЛОКИРОВКА ЧАНКА!
//            // Включаем ChunkActiveState прямо здесь, на месте! 
//            // state.EntityManager.SetComponentEnabled — это НЕ структурное изменение!
//            // Оно легально работает внутри foreach и мгновенно выкидывает чанк из 
//            // этого запроса на все следующие кадры, пока джобы не завершатся!
//            // ====================================================================
//            //state.EntityManager.SetComponentEnabled<ChunkActiveState>(entity, true);
//            m_ActiveStateLookup.SetComponentEnabled(entity, true);
//            // ====================================================================

//            // Выделяем раздельные unmanaged safe-буферы для джобы в Persistent куче кадра
//            var tempVertices = new NativeArray<VoxelVertex>(16384, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            var tempIndices = new NativeArray<int>(24576, Allocator.Persistent, NativeArrayOptions.ClearMemory);

//            var singleChunkCounter = new NativeArray<int2>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

//            var greedyJob = new MeshGreedyJobSafeDirect
//            {
//                LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
//                FlattenedModelColors = template.FlattenedLinearColors,
//                GlobalPaletteColors = template.PaletteColors,
//                ChunkOffsetInFlattenedArray = chunkOffset,
//                OutputVertices = tempVertices,
//                OutputIndices = tempIndices,
//                JobCountersRef = singleChunkCounter
//            };

//            JobHandle meshJobHandle = greedyJob.Schedule(state.Dependency);
//            //m_JobHandles.Add(meshJobHandle);

//            //==============Physics====================
//            // НА 100% СЕЙФОВЫЙ МАНЕВР: Персональный мини-массив коллайдера для КАЖДОГО чанка!
//            // Никаких GetSubArray и общих ссылок. Полная изоляция памяти.
//            // ====================================================================
//            var singleChunkColliderBlob = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            // ====================================================================
//            //UnityEngine.Debug.LogWarning($"tempVertices.lenght={tempVertices.Length}");
//            // 2. Планируем джобу выпекания MeshCollider для этого чанка
//            var colliderJob = new VoxelMeshColliderBakingJob
//            {
//                SourceVertices = tempVertices,
//                SourceIndices = tempIndices,
//                OutputColliderBlob = singleChunkColliderBlob,
//                VertexCount = 16384,
//                IndexCount = 24576
//            };

//            //var colliderJob = new PhysicsGreedyJobSafeDirect
//            //{
//            //    LiveMask = maskBuffer.AsNativeArray().AsReadOnly(),
//            //    OutputColliderBlob = singleChunkColliderBlob,
//            //    ChunkOffsetInFlattenedArray = chunkOffset,
//            //    FlattenedModelColors = template.FlattenedLinearColors,
//            //};

//            JobHandle chunkColliderHandle = colliderJob.Schedule(meshJobHandle);

//            // КРИТИЧЕСКИЙ ШАГ: Перезаписываем state.Dependency, чтобы СЛЕДУЮЩИЙ чанк в цикле 
//            // встал в очередь ПОСЛЕ этого, а не параллельно ломал потоки!
//            state.Dependency = chunkColliderHandle;

//            m_JobHandles.Add(chunkColliderHandle);

//            //// Рассчитываем локальное смещение чанка (размер чанка 32 вокселя * 1.0м)
//            //float3 localOffset = (float3)chunkIndex.ValueRO.Value * 32f * 1.0f;
//            //childOffsetsArray[jobIndex] = localOffset;

//            // 1. Вычисляем исходное локальное смещение чанка (размер чанка 32 вокселя)
//            float3 baseLocalOffset = (float3)chunkIndex.ValueRO.Value * 32f * 1.0f;

//            // ====================================================================
//            // ЖЕЛЕЗОБЕТОННЫЙ ФИКС ПИВОТА ДЛЯ ФИЗИКИ:
//            // Вычисляем точно такой же pivotOffset, как и в спавнере чанков!
//            // Замените modelSizeInChunks на реальные габариты вашей модели в чанках.
//            float3 pivotOffset = new float3(
//                (template.SizeModel.x * 32f) / 2f,
//                0f, // Оставляем 0, чтобы физическое дно машины совпало с пивотом (0,5,0)
//                (template.SizeModel.z * 32f) / 2f
//            );

//            // Вычитаем смещение, чтобы сдвинуть физический бокс коллайдера чанка вслед за графикой!
//            float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

//            // Записываем СМЕЩЕННЫЙ оффсет в массив для генерации CompoundCollider
//            childOffsetsArray[jobIndex] = localOffsetWithPivot;
//            // ====================================================================


//            //// Считаем Bounds через чистый unmanaged AABB.Transform без managed-вызовов
//            //var aabbLocal = new Unity.Mathematics.AABB { Center = new float3(16f, 16f, 16f), Extents = new float3(16f, 16f, 16f) };
//            //var aabbWorld = aabbLocal;
//            //if (state.EntityManager.HasComponent<LocalToWorld>(entity))
//            //{
//            //    var ltw = state.EntityManager.GetComponentData<LocalToWorld>(entity);
//            //    aabbWorld = Unity.Mathematics.AABB.Transform(ltw.Value, aabbLocal);
//            //}
//            // 2. СЧИТАЕМ BOUNDS С УЧЕТОМ НОВОГО ЛОКАЛЬНОГО ПОЛОЖЕНИЯ
//            // Локальный центр чанка в пространстве машины теперь равен: смещенный оффсет чанка + центр самого чанка (16,16,16)
//            float3 correctedChunkCenterInVehicleSpace = localOffsetWithPivot + new float3(16f, 16f, 16f);

//            var aabbLocal = new Unity.Mathematics.AABB
//            {
//                Center = correctedChunkCenterInVehicleSpace,
//                Extents = new float3(16f, 16f, 16f)
//            };

//            var aabbWorld = aabbLocal;

//            // Если у машины (а не у чанка!) есть LocalToWorld, трансформируем локальный AABB в мировой
//            if (state.EntityManager.HasComponent<LocalToWorld>(rootVehicleEntity))
//            {
//                var parentLtw = state.EntityManager.GetComponentData<LocalToWorld>(rootVehicleEntity);
//                aabbWorld = Unity.Mathematics.AABB.Transform(parentLtw.Value, aabbLocal);
//            }

//            //m_ChunksToInitialize.Add(new NewChunkGraphicsData
//            //{
//            //    TargetEntity = entity,
//            //    JobIndex = jobIndex,
//            //    LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
//            //    WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
//            //    HasGraphicsBefore = hasGraphics,
//            //    SafeVertices = tempVertices,
//            //    SafeIndices = tempIndices,
//            //    SafeCounter = singleChunkCounter,

//            //    // Прокидываем ссылку на персональный массив во второй этап кадра
//            //    SafeColliderBlob = singleChunkColliderBlob
//            //});

//            // ====================================================================
//            // ЖЕЛЕЗОБЕТОННЫЙ AAA ПРЕДОХРАНИТЕЛЬ ОТ ObjectDisposedException:
//            // Если этот чанк по сети дергается повторно, пока старые джобы еще не допеклись,
//            // мы обязаны принудительно очистить его старые Persistent массивы, 
//            // которые застряли в маркере, ПРЕДОТВРАЩАЯ дублирование и затирание ссылок!
//            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//            // ====================================================================
//            // if (state.EntityManager.HasComponent<ChunkGraphicsFlushTag>(entity))
//            if (m_FlushTagLookup.HasComponent(entity))
//            {
//                var oldTag = state.EntityManager.GetComponentData<ChunkGraphicsFlushTag>(entity);

//                // Так как старые джобы еще могли крутиться, мы обязаны жестко дождаться их 
//                // завершения наносекундным Complete(), чтобы легально освободить С++ кучу!
//                //oldTag.LastBakingJobHandle.Complete();

//                // Чисто утилизируем старые брошенные массивы прошлого прохода симуляции!
//                if (oldTag.SafeVertices.IsCreated) oldTag.SafeVertices.Dispose(oldTag.LastBakingJobHandle);
//                if (oldTag.SafeIndices.IsCreated) oldTag.SafeIndices.Dispose(oldTag.LastBakingJobHandle);
//                if (oldTag.SafeCounter.IsCreated) oldTag.SafeCounter.Dispose(oldTag.LastBakingJobHandle);
//                // КЛЮЧЕВОЙ ШАГ: Безопасно уничтожаем С++ Блоб коллизий прошлого тика из базы данных движка!
//                if (oldTag.SafeColliderBlob.IsCreated)
//                {
//                    if (oldTag.SafeColliderBlob[0].IsCreated)
//                    {
//                        //oldTag.SafeColliderBlob[0].Dispose(); // Утилизировали BlobAssetReference!
//                        // 1. Создаем джобу утилизации старого блоба
//                        var disposeJob = new DisposeBlobAssetJob
//                        {
//                            BlobContainer = oldTag.SafeColliderBlob
//                        };

//                        // 2. Планируем её запуск СТРОГО ПОСЛЕ завершения старой джобы выпекания.
//                        // Она встает в очередь воркера и выполнится сама, когда старый коллайдер освободится.
//                        Unity.Jobs.JobHandle disposeHandle = disposeJob.Schedule(oldTag.LastBakingJobHandle);

//                        // 3. Подмешиваем этот хэндл в общую цепочку зависимостей кадра,
//                        // чтобы Unity знал, что память будет легально очищена в фоне.
//                        state.Dependency = Unity.Jobs.JobHandle.CombineDependencies(state.Dependency, disposeHandle);

//                    }
//                    //oldTag.SafeColliderBlob.Dispose(oldTag.LastBakingJobHandle);
//                }
//            }
//            // ====================================================================


//            // ====================================================================
//            // БЕЗОПАСНАЯ РЕГИСТРАЦИЯ НА СУЩНОСТИ ЧАНКА:
//            // Откладываем добавление компонента-маркера выгрузки графики в ECB.
//            // OnUpdate завершается мгновенно, оставаясь на 100% BurstCompile-чистым!
//            // ====================================================================
//            // Теперь мы со спокойной душой перезаписываем маркер свежими массивами текущего кадра!
//            m_FlushTagLookup[entity] = new ChunkGraphicsFlushTag
//            {
//                LastBakingJobHandle = chunkColliderHandle,
//                SafeVertices = tempVertices, // Новые чистые Persistent-массивы кадра
//                SafeIndices = tempIndices,
//                SafeCounter = singleChunkCounter,
//                SafeColliderBlob = singleChunkColliderBlob,
//                RootVehicleEntity = rootVehicleEntity,
//                LocalOffsetWithPivot = localOffsetWithPivot,
//                LocalBounds = new MinMaxAABB { Min = aabbLocal.Min, Max = aabbLocal.Max },
//                WorldBounds = new MinMaxAABB { Min = aabbWorld.Min, Max = aabbWorld.Max },
//                HasGraphicsBefore = hasGraphics,
//                index = chunkIndex.ValueRO.Value
//            };

//            //var isClient = state.WorldUnmanaged.IsClient();
//            //string textWorld = isClient ? "Client" : "Server";

//            //UnityEngine.Debug.Log($"[{textWorld}] Добавление ChunkGraphicsFlushTag для {chunkIndex.ValueRO.Value}");

//            //// Если маркер уже был — обновляем его структуру атомарно, если нет — добавляем
//            //if (state.EntityManager.HasComponent<ChunkGraphicsFlushTag>(entity))
//            //{
//            //    ecb.SetComponent(entity, finalFlushComponent);
//            //}
//            //else
//            //{
//            //    ecb.AddComponent(entity, finalFlushComponent);
//            //}
//            m_FlushTagLookup.SetComponentEnabled(entity, true);

//            jobIndex++;
//        }

//        if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();

//        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        if (m_JobHandles.Length == 0)
//        {
//            // Если в этом кадре разрушений не было — просто сбрасываем список
//            m_JobHandles.Clear();

//            // Чистим пустую записную книжку кадра, чтобы не было утечек памяти
//            //if (ecb.IsCreated) ecb.Dispose();
//            return;
//        }

//        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        if (m_JobHandles.Length == 0) return;

//        // ====================================================================
//        // ЖЕЛЕЗОБЕТОННЫЙ АККОРД БЕЗОПАСНОСТИ:
//        // Объединяем хэндлы асинхронного выпекания ВСЕХ индивидуальных чанков.
//        // ====================================================================
//        JobHandle allChunksBakedHandle = JobHandle.CombineDependencies(m_JobHandles.AsArray());
//        // Передаем итоговую чистую зависимость кадра в систему
//        state.Dependency = allChunksBakedHandle;
//        //m_JobHandles.Dispose();

//        //ecb.Playback(state.EntityManager);
//        //ecb.Dispose();

//        //if (m_JobHandles.Length == 0) return;

//        //// Соединяем и завершаем параллельные воркеры на всех ядрах CPU одновременно
//        //state.Dependency = JobHandle.CombineDependencies(m_JobHandles.AsArray());
//        //state.Dependency.Complete();

//        ////=================Physics===============
//        //// ====================================================================
//        //// СБОРКА СЛОВНОГО КОЛЛАЙДЕРА ИЗ ИЗОЛИРОВАННЫХ МАССИВОВ КАДРА
//        //// МЕНЯЕМ СИМВОЛЫ СРАВНЕНИЯ В ЦИКЛЕ НА СЛОВА ПО ПРАВИЛУ!
//        //// ====================================================================
//        //int validCollidersCount = 0;
//        //for (int i = 0; i < m_ChunksToInitialize.Length; i++)
//        //{
//        //    var chunkData = m_ChunksToInitialize[i];
//        //    int2 finalCounts = chunkData.SafeCounter[0];

//        //    // Считаем чанки, которые реально сгенерировали полигоны (вершины > 0)
//        //    if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated && chunkData.SafeColliderBlob.IsCreated)
//        //    {
//        //        validCollidersCount++;
//        //    }
//        //}

//        //BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

//        //if (validCollidersCount > 0)
//        //{
//        //    var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//        //    int currentInstanceIdx = 0;

//        //    for (int i = 0; i < m_ChunksToInitialize.Length; i++)
//        //    {
//        //        var chunkData = m_ChunksToInitialize[i];
//        //        int2 finalCounts = chunkData.SafeCounter[0];

//        //        if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated)
//        //        {
//        //            // Извлекаем блоб из нулевого индекса персонального массива чанка!
//        //            var chunkMeshColliderRef = chunkData.SafeColliderBlob;

//        //            compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//        //            {
//        //                Collider = (BlobAssetReference<Unity.Physics.Collider>)chunkMeshColliderRef[0],
//        //                CompoundFromChild = new RigidTransform(quaternion.identity, childOffsetsArray[i])
//        //            };
//        //            currentInstanceIdx++;
//        //        }
//        //    }

//        //    // Атомарно выпекаем общий коллайдер автомобиля в C++ куче Unity Physics
//        //    //finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//        //    ////compoundInstances.Dispose();
//        //    bool isCompoundCreatedSuccessfully = false;
//        //    try
//        //    {
//        //        // Выпекаем общий коллайдер автомобиля
//        //        finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//        //        isCompoundCreatedSuccessfully = finalVehicleCompoundCollider.IsCreated;
//        //    }
//        //    finally
//        //    {
//        //        // ИСПРАВЛЕНИЕ: Очищаем ТОЛЬКО временный массив структур. 
//        //        // Дочерние коллайдеры (compoundInstances[k].Collider) НЕ ТРОГАЕМ!
//        //        if (compoundInstances.IsCreated)
//        //        {
//        //            compoundInstances.Dispose();
//        //        }

//        //        // Если выпекание упало с ошибкой, чистим только бесхозный корень
//        //        if (!isCompoundCreatedSuccessfully && finalVehicleCompoundCollider.IsCreated)
//        //        {
//        //            finalVehicleCompoundCollider.Dispose();
//        //        }
//        //    }
//        //}

//        //// Чистим массив смещений кадра
//        //if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();
//        //// ====================================================================
//        ////=======================================


//        //// Передаем собранные данные в изолированный managed-метод выгрузки ассетов
//        //ExecuteManagedMeshAllocation(
//        //    ref state,
//        //    m_ChunksToInitialize,
//        //    rootVehicleEntity,
//        //    finalVehicleCompoundCollider,
//        //    m_ConfigQuery,
//        //    m_StorageQuery
//        //);

//        //if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();

//        //// ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        //if (m_JobHandles.Length == 0) return;

//        //// ====================================================================
//        //// ЖЕЛЕЗОБЕТОННЫЙ АККОРД БЕЗОПАСНОСТИ:
//        //// Объединяем хэндлы асинхронного выпекания ВСЕХ индивидуальных чанков.
//        //// ====================================================================
//        //JobHandle allChunksBakedHandle = JobHandle.CombineDependencies(m_JobHandles.AsArray());
//        ////m_JobHandles.Dispose();
//        //// Передаем итоговую чистую зависимость кадра в систему
//        //state.Dependency = allChunksBakedHandle;

//        // ====================================================================
//        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: ЖЕСТКИЙ ПРИНУДИТЕЛЬНЫЙ COMPLETE ДО ЧТЕНИЯ МАССИВОВ!
//        // Мы заставляем главный поток дождаться завершения выпекания чанков 
//        // ДО того, как начнем читать данные из chunkData.SafeColliderBlob.
//        // Это полностью очистит ошибку AtomicSafetyHandle CheckReadAndThrow!
//        // ====================================================================
//        //allChunksBakedHandle.Complete();
//        // ====================================================================

//        //int totalChunksCount = m_ChunksToInitialize.Length;

//        //int validCollidersCount = 0;
//        //// ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        //for (int i = 0; i < totalChunksCount; i++)
//        //{
//        //    var chunkData = m_ChunksToInitialize[i];
//        //    int2 finalCounts = chunkData.SafeCounter[0];

//        //    // Теперь читать из SafeColliderBlob на 100% легально и безопасно, так как Complete() уже отработал!
//        //    if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated)
//        //    {
//        //        validCollidersCount++;
//        //    }
//        //}

//        //BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

//        //if (validCollidersCount > 0)
//        //{
//        //    var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//        //    int currentInstanceIdx = 0;

//        //    // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        //    for (int i = 0; i < totalChunksCount; i++)
//        //    {
//        //        var chunkData = m_ChunksToInitialize[i];
//        //        int2 finalCounts = chunkData.SafeCounter[0];

//        //        if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated)
//        //        {
//        //            var chunkMeshColliderRef = chunkData.SafeColliderBlob;

//        //            compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//        //            {
//        //                // Извлекаем готовый, выпеченный воркером BlobAsset-коллайдер чанка из нулевой ячейки
//        //                Collider = (BlobAssetReference<Unity.Physics.Collider>)chunkMeshColliderRef[0],
//        //                CompoundFromChild = new RigidTransform(quaternion.identity, childOffsetsArray[i])
//        //            };
//        //            currentInstanceIdx++;
//        //        }
//        //    }

//        //    bool isCompoundCreatedSuccessfully = false;
//        //    try
//        //    {
//        //        // Синхронно склеиваем чанки в единый коллайдер автомобиля.
//        //        // Так как тяжелое выпекание MeshCollider для каждого чанка уже завершено на воркерах,
//        //        // метод CompoundCollider.Create отработает мгновенно (за сотые доли миллисекунды)!
//        //        finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//        //        isCompoundCreatedSuccessfully = finalVehicleCompoundCollider.IsCreated;
//        //    }
//        //    finally
//        //    {
//        //        if (compoundInstances.IsCreated)
//        //        {
//        //            compoundInstances.Dispose();
//        //        }

//        //        if (!isCompoundCreatedSuccessfully && finalVehicleCompoundCollider.IsCreated)
//        //        {
//        //            finalVehicleCompoundCollider.Dispose();
//        //        }
//        //    }
//        //}

//        //// Чистим массив смещений кадра пивотов
//        //if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();


//        //// Выгружаем готовый Compound-коллайдер и меши в managed-конвейер отображения автомобиля
//        //ExecuteManagedMeshAllocation(
//        //    ref state,
//        //    m_ChunksToInitialize,
//        //    rootVehicleEntity,
//        //    //finalVehicleCompoundCollider,
//        //    m_ConfigQuery,
//        //    m_StorageQuery,
//        //    childOffsetsArray
//        //);
//    }



//    //    // ЭТОТ МЕТОД ВЫПОЛНЯЕТСЯ В MANAGED РЕЖИМЕ БЕЗ КОНФЛИКТОВ С BURST
//    //    [MethodImpl(MethodImplOptions.NoInlining)]
//    //    private void ExecuteManagedMeshAllocation(
//    //            ref SystemState state,
//    //            NativeList<NewChunkGraphicsData> chunksData,
//    //            Entity rootVehicleEntity,
//    //            //BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider,
//    //            EntityQuery m_ConfigQuery,
//    //            EntityQuery m_StorageQuery
//    //,
//    //            NativeArray<float3> childOffsetsArray)
//    //    {
//    //        state.Dependency.Complete();


//    //        int totalChunksCount = m_ChunksToInitialize.Length;

//    //        int validCollidersCount = 0;
//    //        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//    //        for (int i = 0; i < totalChunksCount; i++)
//    //        {
//    //            var chunkData = m_ChunksToInitialize[i];
//    //            int2 finalCounts = chunkData.SafeCounter[0];

//    //            // Теперь читать из SafeColliderBlob на 100% легально и безопасно, так как Complete() уже отработал!
//    //            if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated)
//    //            {
//    //                validCollidersCount++;
//    //            }
//    //        }

//    //        BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

//    //        if (validCollidersCount > 0)
//    //        {
//    //            var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//    //            int currentInstanceIdx = 0;

//    //            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//    //            for (int i = 0; i < totalChunksCount; i++)
//    //            {
//    //                var chunkData = m_ChunksToInitialize[i];
//    //                int2 finalCounts = chunkData.SafeCounter[0];

//    //                if (finalCounts.x > 0 && chunkData.SafeColliderBlob.IsCreated)
//    //                {
//    //                    var chunkMeshColliderRef = chunkData.SafeColliderBlob;

//    //                    compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//    //                    {
//    //                        // Извлекаем готовый, выпеченный воркером BlobAsset-коллайдер чанка из нулевой ячейки
//    //                        Collider = (BlobAssetReference<Unity.Physics.Collider>)chunkMeshColliderRef[0],
//    //                        CompoundFromChild = new RigidTransform(quaternion.identity, childOffsetsArray[i])
//    //                    };
//    //                    currentInstanceIdx++;
//    //                }
//    //            }

//    //            bool isCompoundCreatedSuccessfully = false;
//    //            try
//    //            {
//    //                // Синхронно склеиваем чанки в единый коллайдер автомобиля.
//    //                // Так как тяжелое выпекание MeshCollider для каждого чанка уже завершено на воркерах,
//    //                // метод CompoundCollider.Create отработает мгновенно (за сотые доли миллисекунды)!
//    //                finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//    //                isCompoundCreatedSuccessfully = finalVehicleCompoundCollider.IsCreated;
//    //            }
//    //            finally
//    //            {
//    //                if (compoundInstances.IsCreated)
//    //                {
//    //                    compoundInstances.Dispose();
//    //                }

//    //                if (!isCompoundCreatedSuccessfully && finalVehicleCompoundCollider.IsCreated)
//    //                {
//    //                    finalVehicleCompoundCollider.Dispose();
//    //                }
//    //            }
//    //        }

//    //        // Чистим массив смещений кадра пивотов
//    //        if (childOffsetsArray.IsCreated) childOffsetsArray.Dispose();


//    //        var isClient = state.WorldUnmanaged.IsClient();
//    //        string textWorld = isClient ? "Client" : "Server";

//    //        // Извлекаем конфиг
//    //        var brgConfig = state.EntityManager.GetComponentObject<VoxelGlobalConfigComponent>(m_ConfigQuery.GetSingletonEntity());

//    //        // Извлекаем хранилище фреймов
//    //        var frameStorage = state.EntityManager.GetComponentObject<ClientVoxelMeshFrameStorage>(m_StorageQuery.GetSingletonEntity());

//    //        // Получаем графическую систему
//    //        var graphicsSystem = state.World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

//    //        // Буфер команд
//    //        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
//    //        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//    //        if (frameStorage != null)
//    //        {
//    //            frameStorage.RuntimeMeshes.Clear();
//    //            frameStorage.DataArrays.Clear();
//    //            frameStorage.TargetEntities.Clear();
//    //            //#if UNITY_EDITOR
//    //            //            UnityEngine.Debug.Log($"[{textWorld}]: ClientVoxelMeshFrameStorage found and clear!");
//    //            //#endif
//    //        }
//    //        else
//    //        {
//    //#if UNITY_EDITOR
//    //            UnityEngine.Debug.Log($"[{textWorld}]: ClientVoxelMeshFrameStorage not found!");
//    //#endif
//    //        }

//    //        bool isColliderAssignedToEntity = false;
//    //        try
//    //        {
//    //            // Проверка: если маркер есть, но физики (скорости или самого коллайдера) уже нет — 
//    //            // значит машина деспавнится прямо сейчас! Игнорируем её и сразу уничтожаем свежевыпеченный блоб.
//    //            bool isEntityDying = state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity)
//    //                                 && !state.EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity);

//    //            if (rootVehicleEntity != Entity.Null && state.EntityManager.Exists(rootVehicleEntity) && finalVehicleCompoundCollider.IsCreated && !isEntityDying)
//    //            {
//    //                // 1. Создаем маркер и заполняем его в 100% безопасном управляемом коде
//    //                var newCleanupData = new VoxelColliderCleanupMarker
//    //                {
//    //                    ColliderBlob = finalVehicleCompoundCollider,
//    //                    ChildBlobs = new FixedList128Bytes<BlobAssetReference<Unity.Physics.Collider>>()
//    //                };

//    //                // Переносим ссылки на дочерние блобы из вашего m_ChunksToInitialize
//    //                for (int i = 0; i < m_ChunksToInitialize.Length; i++)
//    //                {
//    //                    var chunkData = m_ChunksToInitialize[i];
//    //                    if (chunkData.SafeColliderBlob.IsCreated)
//    //                    {
//    //                        // Сохраняем ссылку на дочерний чанк в маркер для будущей safe-очистки
//    //                        newCleanupData.ChildBlobs.Add(chunkData.SafeColliderBlob[0]);
//    //                    }
//    //                }

//    //                // 2. Если машина перепекается (маркер уже был) — сначала чистим СТАРЫЕ ассеты
//    //                if (state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
//    //                {
//    //                    var oldCleanupData = state.EntityManager.GetComponentData<VoxelColliderCleanupMarker>(rootVehicleEntity);

//    //                    // Safe-удаление старого корня машины
//    //                    if (oldCleanupData.ColliderBlob.IsCreated) oldCleanupData.ColliderBlob.Dispose();

//    //                    // Safe-удаление всех старых дочерних чанков
//    //                    for (int c = 0; c < oldCleanupData.ChildBlobs.Length; c++)
//    //                    {
//    //                        if (oldCleanupData.ChildBlobs[c].IsCreated)
//    //                        {
//    //                            oldCleanupData.ChildBlobs[c].Dispose();
//    //                        }
//    //                    }

//    //                    // Записываем новые данные поверх старых
//    //                    state.EntityManager.SetComponentData(rootVehicleEntity, newCleanupData);
//    //                    state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });
//    //                }
//    //                else
//    //                {
//    //                    // Если машина только создалась
//    //                    state.EntityManager.AddComponentData(rootVehicleEntity, new VoxelColliderCleanupMarker { ColliderBlob = finalVehicleCompoundCollider });
//    //                    state.EntityManager.AddComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });
//    //                }

//    //                isColliderAssignedToEntity = true;

//    //                // Настройка массы и скорости
//    //                var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
//    //                dynamicMass.CenterOfMass = float3.zero;

//    //                if (state.EntityManager.HasComponent<PhysicsMass>(rootVehicleEntity))
//    //                {
//    //                    state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);
//    //                }
//    //                else
//    //                {
//    //                    state.EntityManager.AddComponentData(rootVehicleEntity, dynamicMass);
//    //                    state.EntityManager.AddComponentData(rootVehicleEntity, new PhysicsVelocity());
//    //                }

//    //                // Add the physics world index via the command buffer
//    //                if (!state.EntityManager.HasComponent<PhysicsWorldIndex>(rootVehicleEntity))
//    //                {
//    //                    state.EntityManager.AddSharedComponentManaged(rootVehicleEntity, new PhysicsWorldIndex
//    //                    {
//    //                        Value = 0
//    //                    });
//    //                }

//    //                state.EntityManager.AddComponentData(rootVehicleEntity, new AAA_MovementComponent
//    //                {
//    //                    MaxSpeed = 45f,
//    //                    Acceleration = 25f,
//    //                    Deceleration = 18f
//    //                });
//    //                //#if UNITY_EDITOR
//    //                //                UnityEngine.Debug.Log($"[{textWorld}] Найдена корневая сущность! Свежий коллайдер добавлен.");
//    //                //#endif
//    //            }
//    //            else
//    //            {
//    //                // Сущности нет — просто удаляем только что созданный коллайдер
//    //                if (finalVehicleCompoundCollider.IsCreated)
//    //                {
//    //                    finalVehicleCompoundCollider.Dispose();
//    //                }
//    //#if UNITY_EDITOR
//    //                UnityEngine.Debug.LogWarning($"[{textWorld}] Не найдена корневая сущность или она уничтожена! Свежий коллайдер удален.");
//    //#endif
//    //            }
//    //        }
//    //        finally
//    //        {
//    //            // Если что-то пошло не так во время AddComponent/SetComponent (например, OutOfMemory или Burst-abort)
//    //            if (!isColliderAssignedToEntity && finalVehicleCompoundCollider.IsCreated)
//    //            {
//    //                finalVehicleCompoundCollider.Dispose();
//    //#if UNITY_EDITOR
//    //                UnityEngine.Debug.LogWarning($"[{textWorld}] Критический сбой! Коллайдер принудительно стерт в блоке finally.");
//    //#endif
//    //            }
//    //        }
//    //        // ====================================================================

//    //        var renderMeshDescription = new RenderMeshDescription
//    //        {
//    //            FilterSettings = new RenderFilterSettings { ShadowCastingMode = ShadowCastingMode.Off, ReceiveShadows = false, Layer = 0, RenderingLayerMask = 1, MotionMode = MotionVectorGenerationMode.Object, StaticShadowCaster = false },
//    //            LightProbeUsage = LightProbeUsage.Off
//    //        };

//    //        // ====================================================================
//    //        // ЭТАП 2: БЕЗОПАСНАЯ ФИКСАЦИЯ НА GPU И СБОРКА КОМПОНЕНТОВ BRG
//    //        // ====================================================================
//    //        for (int i = 0; i < chunksData.Length; i++)
//    //        {
//    //            var chunkData = chunksData[i];

//    //            int2 finalCounts = chunkData.SafeCounter[0];
//    //            int vertexCount = finalCounts.x;
//    //            int indexCount = finalCounts.y;

//    //            // Вручную и легально чистим буфер счетчика (Safe)
//    //            if (chunkData.SafeCounter.IsCreated) chunkData.SafeCounter.Dispose();

//    //            if (vertexCount == 0)
//    //            {
//    //                if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//    //                if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//    //                if (chunkData.HasGraphicsBefore)
//    //                {
//    //                    var emptyInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
//    //                    emptyInfo.MeshID = brgConfig.EmptyMeshID;
//    //                    ecb.SetComponent(chunkData.TargetEntity, emptyInfo);
//    //                }
//    //                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//    //                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
//    //                continue;
//    //            }

//    //            if (state.WorldUnmanaged.IsClient())
//    //            {
//    //                // Выделяем С++ контейнеры под меш строго под вычисленный в джобе размер
//    //                var meshDataArray = Mesh.AllocateWritableMeshData(1);
//    //                var meshData = meshDataArray[0];

//    //                var attributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
//    //                attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
//    //                attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
//    //                meshData.SetVertexBufferParams(vertexCount, attributes);
//    //                meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
//    //                attributes.Dispose();

//    //                // Создаем безопасные sub-array окна видимости одинаковой длины
//    //                var activeVerticesSubArray = chunkData.SafeVertices.GetSubArray(0, vertexCount);
//    //                var activeIndicesSubArray = chunkData.SafeIndices.GetSubArray(0, indexCount);

//    //                // Копируем массивы напрямую в MeshData
//    //                meshData.GetVertexData<VoxelVertex>(0).CopyFrom(activeVerticesSubArray);
//    //                meshData.GetIndexData<int>().CopyFrom(activeIndicesSubArray);

//    //                // Вручную освобождаем временные массивы (Safe-пайплайн)
//    //                if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//    //                if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//    //                meshData.subMeshCount = 1;
//    //                meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { topology = MeshTopology.Triangles, vertexCount = vertexCount }, MeshUpdateFlags.DontRecalculateBounds);

//    //                Mesh runtimeMesh = new Mesh();
//    //                runtimeMesh.name = "VoxelChunk_SafeDirect_BurstOptimized";

//    //                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, runtimeMesh);
//    //                runtimeMesh.RecalculateBounds();

//    //                BatchMeshID runtimeMeshId = graphicsSystem.RegisterMesh(runtimeMesh);
//    //                var finalMaterialMeshInfo = new MaterialMeshInfo(brgConfig.OpaqueMaterialRuntimeID, runtimeMeshId);

//    //                bool currentHasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(chunkData.TargetEntity);

//    //                if (!currentHasGraphics)
//    //                {
//    //                    RenderMeshUtility.AddComponents(chunkData.TargetEntity, state.EntityManager, renderMeshDescription, finalMaterialMeshInfo);
//    //                    state.EntityManager.SetComponentData(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
//    //                    state.EntityManager.SetComponentData(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
//    //                }
//    //                else
//    //                {
//    //                    var oldInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
//    //                    if (oldInfo.MeshID != BatchMeshID.Null && oldInfo.MeshID != brgConfig.EmptyMeshID) graphicsSystem.UnregisterMesh(oldInfo.MeshID);

//    //                    ecb.SetComponent(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
//    //                    ecb.SetComponent(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
//    //                    ecb.SetComponent(chunkData.TargetEntity, finalMaterialMeshInfo);
//    //                }

//    //                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//    //                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
//    //            }
//    //            else
//    //            {
//    //                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//    //                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
//    //            }
//    //            // ====================================================================
//    //            // ЕДИНАЯ ТОЧКА РУЧНОЙ SAFE-УТИЛИЗАЦИИ ВСЕХ МАССИВОВ ЧАНКА В КАДРЕ
//    //            // Память гарантированно очистится ровно ОДИН РАЗ, исключая ObjectDisposedException!
//    //            // ====================================================================
//    //            // 1. Стираем фоновые массивы вершин и индексов
//    //            if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//    //            if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//    //            // 2. Стираем буфер unmanaged-счетчиков
//    //            if (chunkData.SafeCounter.IsCreated) chunkData.SafeCounter.Dispose();

//    //            // 3. Стираем С++ блоб меш-коллайдера и сам персональный массив
//    //            if (chunkData.SafeColliderBlob.IsCreated)
//    //            {
//    //                var chunkMeshColliderRef = chunkData.SafeColliderBlob[0];
//    //                if (chunkMeshColliderRef.IsCreated)
//    //                {
//    //                    chunkMeshColliderRef.Dispose();
//    //                }
//    //                chunkData.SafeColliderBlob.Dispose();
//    //            }
//    //            // ====================================================================
//    //        }
//    //    }
//}
