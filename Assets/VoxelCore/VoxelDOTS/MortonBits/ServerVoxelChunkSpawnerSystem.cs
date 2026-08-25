using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// На следующем кадре Netcode уже выдал объекту живой ghostId. 
/// Теперь сервер заглядывает в unmanaged-кэш GlobalVoxelModelCache, узнает, 
/// сколько чанков у этой модели, спавнит под каждый чанк сетевую сущность, 
/// привязывает её к родителю 
/// и заполняет ChunkDestructionMask всеми битами в true (так как изначально модель целая).
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct ServerVoxelChunkSpawnerSystem : ISystem
{
    // В файле ServerVoxelChunkSpawnerSystem.cs
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VoxelGhostPrefabConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var modelCache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();
        var prefabConfig = SystemAPI.GetSingleton<VoxelGhostPrefabConfig>();

        // Если синглтона вообще нет в мире (например, мир уже уничтожается) — мгновенно выходим!
        if (!SystemAPI.HasSingleton<VoxelGhostPrefabConfig>()) return;

        // Итерируемся по активным unmanaged-задачам спавна
        foreach (var (task, taskEntity) in SystemAPI.Query<RefRO<VoxelSpawnTaskComponent>>().WithEntityAccess())
        {
            Entity rootEntity = task.ValueRO.TargetRootEntity;

            // Ждем, пока Netcode полностью инициализирует Ghost-префаб и выдаст ему GhostInstance
            if (!SystemAPI.HasComponent<GhostInstance>(rootEntity)) continue;

            // 1. Безопасно извлекаем сетевой инстанс корня в основном потоке системы
            GhostInstance rootGhostInstance = SystemAPI.GetComponent<GhostInstance>(rootEntity);
            uint rootNetworkId = (uint)rootGhostInstance.ghostId;

            if (rootNetworkId == 0) continue;

            // Уничтожаем сущность задачи, так как корень готов к наполнению чанками
            ecb.DestroyEntity(taskEntity);

            uint modelHash = task.ValueRO.ConfigHashName;
            //#if UNITY_EDITOR
            //            UnityEngine.Debug.LogWarning($"rootNetworkId={rootNetworkId}, rootGhostInstance.ghostId={rootGhostInstance.ghostId}");
            //#endif
            // 2. ИСПРАВЛЕНИЕ: Извлекаем актуальный LocalTransform родительского корня
            LocalTransform rootTransform = SystemAPI.GetComponent<LocalTransform>(rootEntity);

            // Записываем хэш в компонент данных — теперь Netcode его не сотрет
            ecb.SetComponent(rootEntity, new VoxelModelRootData { ConfigHashName = modelHash });

            // Ищем шаблон в unmanaged-кэше синглтона по хэшу
            if (!modelCache.Templates.TryGetValue(modelHash, out var template))
            {
                //#if UNITY_EDITOR
                //                UnityEngine.Debug.LogError($"[Voxel Server]: Модель с хэшем {modelHash} не найдена в кэше при спавне чанков!");
                //#endif
                continue;
            }

            // Извлекаем массив координат чанков текущей модели
            var chunkCoords = template.ChunkCoordToOrderIndexMap.GetKeyArray(Allocator.Temp);
            int totalActiveChunks = chunkCoords.Length;

            // Передаем rootTransform через модификатор in в Burst-метод генерации подсети
            SpawnModelChunks(ref state, ref ecb, in prefabConfig.ChunkGhostPrefab, rootNetworkId, modelHash, in chunkCoords, totalActiveChunks, in rootTransform, ref rootEntity, ref template.SizeModel);
        }
    }

    [BurstCompile]
    private static void SpawnModelChunks(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        in Entity chunkPrefabEntity,
        uint rootNetworkId,
        uint configHash,
        in NativeArray<int3> chunkCoords,
        int totalActiveChunks,
        in LocalTransform rootTransform,
        ref Entity rootEntity, // Принимаем Entity родительского корня для заполнения его буфера
        ref int3 modelSizeInChunks) // Передаем габариты модели (например, X:4, Y:2, Z:6 чанков)
    {
        // 1. ВЫЧИСЛЯЕМ СМЕЩЕНИЕ ПИВОТА В МЕТРАХ (размер чанка = 32 вокселя * 1.0f)
        // Делим размеры на 2.0f, чтобы найти идеальный геометрический центр кузова
        float3 pivotOffset = new float3(
            (modelSizeInChunks.x * 32f) / 2f,
            0f, // По оси Y оставляем 0, чтобы пивот был в самом НИЗУ (под днищем машины)
            (modelSizeInChunks.z * 32f) / 2f
        );


        for (int i = 0; i < totalActiveChunks; i++)
        {
            int3 localChunkCoord = chunkCoords[i];

            // 1. Инстанциируем чанк из Ghost-префаба
            Entity chunkEntity = ecb.Instantiate(chunkPrefabEntity);


            // 2. Вычисляем начальные мировые координаты чанка на сервере (для Relevance видимости)
            float3 localOffset = (float3)localChunkCoord * 32.0f * 1.0f;
            float3 localOffsetWithPivot = localOffset - pivotOffset;
            //float3 worldChunkPos = math.transform(rootTransform.ToMatrix(), localOffset);

            ecb.AddComponent(chunkEntity, new LocalTransform
            {
                Position = localOffsetWithPivot,
                Rotation = quaternion.identity, // rootTransform.Rotation,
                Scale = 1.0f
            });

            // 3. Прописываем метаданные для Presentation-конвейера
            ecb.AddComponent(chunkEntity, new ChunkIndexComponent { Value = localChunkCoord });
            ecb.AddComponent(chunkEntity, new VoxelModelHeader { ConfigHashName = configHash });

            // 4. Локальный серверный буфер маски разрушений
            var maskBuffer = ecb.SetBuffer<LocalChunkDestructionMask>(chunkEntity);
            maskBuffer.Clear();
            for (int m = 0; m < 512; m++)
            {
                maskBuffer.Add(new LocalChunkDestructionMask { Value = 0xFFFFFFFFFFFFFFFFUL });
            }


            // 5. Спавни чанки сразу активными и взводим тег рендеринга
            ecb.SetComponentEnabled<ChunkActiveState>(chunkEntity, false);
            ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkEntity, false);
            //ecb.SetComponentEnabled<ChunkGraphicsFlushTag>(chunkEntity, false);
            ecb.AddComponent<NeedsMeshRebuildTag>(chunkEntity);

            // Внедряем компонент Parent, связывая чанк с корнем машины
            ecb.AddComponent(chunkEntity, new Parent { Value = rootEntity });

            ecb.AddComponent(chunkEntity, new NetworkParent { ParentGhostId = rootNetworkId });
            //// СВЯЗЫВАЕМ ИЕРАРХИЮ ДЛЯ СЕТИ (Unity Netcode)
            //// Добавляем маркер и регистрируем чанк в сетевой группе корня
            //ecb.AddComponent<GhostChildEntity>(chunkEntity); //

            // ====================================================================
            // КАНОНИЧНАЯ СВЯЗЬ ИЕРАРХИИ GHOST-ОБЪЕКТОВ ДЛЯ UNITY NETCODE 6
            // Чтобы Netcode сервера упаковал чанки внутрь Snapshot-пакета родителя,
            // мы обязаны принудительно прописать отложенную команду AppendToBuffer 
            // для LinkedEntityGroup корневой сущности! Это вернет чанки в семью.
            // ====================================================================
            ecb.AppendToBuffer(rootEntity, new LinkedEntityGroup { Value = chunkEntity });
            // ====================================================================


            //// Получаем GhostInstance от корневой машины
            //// (Серверная сущность машины уже должна иметь этот компонент после Instantiate)
            //if (state.EntityManager.HasComponent<GhostInstance>(rootEntity))
            //{
            //    GhostInstance rootInstance = state.EntityManager.GetComponentData<GhostInstance>(rootEntity);

            //    // Записываем его в наш сетевой компонент
            //    ecb.AddComponent(chunkEntity, new NetworkParent { ParentGhostId = rootInstance.ghostId });
            //}
            //// Обязательный тег для иерархий в Netcode
            //ecb.AddComponent<GhostChildEntity>(chunkEntity);
        }
    }

    //[BurstCompile]
    //private static void SpawnModelChunks(
    //    ref EntityCommandBuffer ecb,
    //    in Entity chunkPrefabEntity,
    //    uint rootNetworkId,
    //    uint configHash,
    //    in NativeArray<int3> chunkCoords,
    //    int totalActiveChunks)
    //{
    //    // Бежим строго от нуля до реального количества активных чанков модели
    //    for (int i = 0; i < totalActiveChunks; i++)
    //    {
    //        int3 localChunkCoord = chunkCoords[i];

    //        // 1. Инстанциируем чанк из Ghost-префаба
    //        Entity chunkEntity = ecb.Instantiate(chunkPrefabEntity);

    //        // ====================================================================
    //        // ААА-ФИКС: МЕНЯЕМ SetComponent НА AddComponent ДЛЯ СЕТЕВОГО РОДИТЕЛЯ
    //        // Так как GhostOwner отсутствует в базовом архетипе префаба чанка, 
    //        // вызов SetComponent приводил к падению Playback-а команд ECB. 
    //        // Метод AddComponent безопасно внедрит сетевой инстанс связи.
    //        // ====================================================================
    //        ecb.AddComponent(chunkEntity, new GhostOwner { NetworkId = (int)rootNetworkId });
    //        // ====================================================================

    //        // 2. Прописываем метаданные для Presentation-конвейера клиента
    //        ecb.AddComponent(chunkEntity, new ChunkIndexComponent { Value = localChunkCoord });
    //        ecb.AddComponent(chunkEntity, new VoxelModelHeader { ConfigHashName = configHash });

    //        // 3. Безопасно формируем локальный буфер маски разрушений через SetBuffer
    //        var maskBuffer = ecb.SetBuffer<LocalChunkDestructionMask>(chunkEntity);
    //        maskBuffer.Clear();
    //        for (int m = 0; m < 512; m++)
    //        {
    //            maskBuffer.Add(new LocalChunkDestructionMask { Value = 0xFFFFFFFFFFFFFFFFUL });
    //        }
    //    }
    //}
}