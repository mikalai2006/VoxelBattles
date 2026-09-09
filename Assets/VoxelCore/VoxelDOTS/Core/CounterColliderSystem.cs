using Unity.Burst;
using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct CounterColliderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // ====================================================================
        // ФАЗА 1: УЛЬТРА-БЫСТРАЯ ПРОВЕРКА НАЛИЧИЯ ДАННЫХ В КАДРЕ
        // Если ни один чанк вокселей в мире не находится в статусе выпекания графики,
        // система мгновенно выходит, затрачивая ровно 0.00 мс времени Main Thread!
        // ====================================================================
        var flushQuery = SystemAPI.QueryBuilder().WithAll<ChunkColliderNeedApply>().Build();
        if (flushQuery.IsEmpty)
        {
            return;
        }

        // Высокоскоростной Burst-заменитель для EntityManager.Exists
        var entityStorageInfoLookup = state.GetEntityStorageInfoLookup();

        // Получаем синглтон коллайдеров
        var voxelChildColliderRegistrySingleton = SystemAPI.GetSingleton<VoxelChildColliderRegistrySingleton>();

        // Выделяем временные контейнеры Mono-кадра для передачи в метод ExecuteManagedMeshAllocation
        //var chunksDataArray = new NativeList<NewChunkColliderData>(16, Allocator.Temp);
        //var childOffsetsList = new NativeList<float3>(16, Allocator.Temp);
        Entity rootVehicleEntity = Entity.Null;

        // ====================================================================
        // ФАЗА 2: СБОРКА ЧАНКОВ, КОТОРЫЕ ПОЛНОСТЬЮ ДОПЕКЛИСЬ НА ЯДРАХ CPU
        // ====================================================================
        foreach (var (status, chunkEntity) in SystemAPI.Query<
            RefRO<ChunkColliderNeedApply>
            >()
            .WithAll<ChunkColliderNeedApply>()  // Сущность попадет в выборку, только если у неё присутствует компонент и он включен (Enabled).
            .WithEntityAccess())
        {
            var chunkColliderData = voxelChildColliderRegistrySingleton.Registry[chunkEntity];
            // АТОМАРНЫЙ БЕЗОПАСНЫЙ ДЕТЕКТОР:
            // Если фоновый воркер процессора ВСЁ ЕЩЕ ПИШЕТ данные в Persistent-массивы этого чанка —
            // мы КАТЕГОРИЧЕСКИ пропускаем его в текущем кадре симуляции!
            // Вызов .IsCompleted занимает 0.00 мс и НИКОГДА не фризит главный поток игры.
            if (!chunkColliderData.SafeStatus.IsCreated || chunkColliderData.SafeStatus[0].z != 1)
            {
                continue;
            }

            rootVehicleEntity = chunkColliderData.RootVehicleEntity; //chunkColliderData.ValueRO.RootVehicleEntity;

            // записываем кол-во чанков.
            if (SystemAPI.HasComponent<AAA_RootData>(rootVehicleEntity))
            {
                RefRW<AAA_RootData> controller = SystemAPI.GetComponentRW<AAA_RootData>(rootVehicleEntity);

                controller.ValueRW.countApplyChunks += 1;

                if (controller.ValueRO.countApplyChunks == controller.ValueRO.countChunks)
                {
                    SystemAPI.SetComponentEnabled<RootColliderNeedApply>(rootVehicleEntity, true);
                }
            }


            if (SystemAPI.Exists(chunkEntity))
            {
                SystemAPI.SetComponentEnabled<ChunkColliderNeedApply>(chunkEntity, false);
            }
        }
    }
}