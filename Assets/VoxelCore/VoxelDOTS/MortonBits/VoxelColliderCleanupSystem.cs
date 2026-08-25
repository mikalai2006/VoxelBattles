//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.NetCode;
//using Unity.Physics;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
//[BurstCompile]
//public partial struct VoxelColliderCleanupSystem : ISystem
//{
//    private EntityQuery m_DestroyedVehiclesQuery;
//    private EntityQuery m_ZombieColliderQuery;
//    private EntityQuery m_AllMarkersQuery;

//    public void OnCreate(ref SystemState state)
//    {
//        m_DestroyedVehiclesQuery = state.GetEntityQuery(
//            ComponentType.ReadOnly<VoxelColliderCleanupMarker>(),
//            ComponentType.Exclude<GhostInstance>()
//        );

//        m_ZombieColliderQuery = state.GetEntityQuery(
//            ComponentType.ReadOnly<VoxelColliderCleanupMarker>(),
//            ComponentType.Exclude<PhysicsCollider>()
//        );

//        m_AllMarkersQuery = state.GetEntityQuery(ComponentType.ReadOnly<VoxelColliderCleanupMarker>());
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        // --- БЛОК 1: ОЧИСТКА ПРИ СЕТЕВОМ ДЕСПАВНЕ (Netcode) ---
//        if (!m_DestroyedVehiclesQuery.IsEmptyIgnoreFilter)
//        {
//            var entities = m_DestroyedVehiclesQuery.ToEntityArray(Allocator.Temp);
//            var cleanupData = m_DestroyedVehiclesQuery.ToComponentDataArray<VoxelColliderCleanupMarker>(Allocator.Temp);

//            for (int i = 0; i < entities.Length; i++)
//            {
//                var marker = cleanupData[i];

//                // 1. Сначала принудительно выжигаем меши чанков деспавненной машины
//                if (!marker.ChildBlobs.IsEmpty)
//                {
//                    for (int c = 0; c < marker.ChildBlobs.Length; c++)
//                    {
//                        var childBlob = marker.ChildBlobs[c];
//                        if (childBlob.IsCreated) childBlob.Dispose();
//                    }
//                }

//                // 2. Только потом удаляем корень компаунда
//                if (marker.ColliderBlob.IsCreated)
//                {
//                    marker.ColliderBlob.Dispose();
//                }

//                state.EntityManager.RemoveComponent<VoxelColliderCleanupMarker>(entities[i]);
//            }

//            entities.Dispose();
//            cleanupData.Dispose();
//        }

//        // --- БЛОК 2: ОЧИСТКА ЗОМБИ-КОЛЛАЙДЕРОВ (Физический сброс) ---
//        if (!m_ZombieColliderQuery.IsEmptyIgnoreFilter)
//        {
//            var entities = m_ZombieColliderQuery.ToEntityArray(Allocator.Temp);
//            var cleanupData = m_ZombieColliderQuery.ToComponentDataArray<VoxelColliderCleanupMarker>(Allocator.Temp);

//            for (int i = 0; i < entities.Length; i++)
//            {
//                var marker = cleanupData[i];

//                // 1. Выжигаем меши чанков зомби-коллайдера
//                if (!marker.ChildBlobs.IsEmpty)
//                {
//                    for (int c = 0; c < marker.ChildBlobs.Length; c++)
//                    {
//                        var childBlob = marker.ChildBlobs[c];
//                        if (childBlob.IsCreated) childBlob.Dispose();
//                    }
//                }

//                // 2. Удаляем корень
//                if (marker.ColliderBlob.IsCreated)
//                {
//                    marker.ColliderBlob.Dispose();
//                }

//                state.EntityManager.RemoveComponent<VoxelColliderCleanupMarker>(entities[i]);
//            }

//            entities.Dispose();
//            cleanupData.Dispose();
//        }
//    }

//    [BurstCompile]
//    public void OnDestroy(ref SystemState state)
//    {
//        // --- БЛОК 3: ТОТАЛЬНЫЙ ВЫХОД ИЗ ИГРЫ (Очистка всей иерархии при закрытии Play Mode) ---
//        if (!m_AllMarkersQuery.IsEmpty)
//        {
//            var cleanupArray = m_AllMarkersQuery.ToComponentDataArray<VoxelColliderCleanupMarker>(Allocator.Temp);

//            for (int i = 0; i < cleanupArray.Length; i++)
//            {
//                var marker = cleanupArray[i];

//                // 1. При выходе из игры стираем абсолютно ВСЕ меши чанков изо всех машин на сцене
//                if (!marker.ChildBlobs.IsEmpty)
//                {
//                    for (int c = 0; c < marker.ChildBlobs.Length; c++)
//                    {
//                        var childBlob = marker.ChildBlobs[c];
//                        if (childBlob.IsCreated) childBlob.Dispose();
//                    }
//                }

//                // 2. Стираем корни машин
//                if (marker.ColliderBlob.IsCreated)
//                {
//                    marker.ColliderBlob.Dispose();
//                }
//            }

//            cleanupArray.Dispose();
//        }
//    }
//}



////using Unity.Collections;
////using Unity.Entities;
////using Unity.NetCode;
////using Unity.Physics;

////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
////[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)] // Выполняем строго в самом конце кадра
////public partial struct VoxelColliderCleanupSystem : ISystem
////{
////    private EntityQuery m_CleanupQuery;

////    private EntityQuery m_DestroyedVehiclesQuery;

////    public void OnCreate(ref SystemState state)
////    {
////        // Кэшируем жесткий запрос к компоненту один раз при старте мира
////        m_CleanupQuery = state.GetEntityQuery(typeof(VoxelColliderCleanupMarker));

////        // Ищем сущности, у которых ЕСТЬ наш маркер коллайдера, 
////        // но которые УЖЕ ПОТЕРЯЛИ сетевой компонент GhostInstance (значит, сеть их удалила)
////        m_DestroyedVehiclesQuery = state.GetEntityQuery(
////            ComponentType.ReadOnly<VoxelColliderCleanupMarker>(),
////            ComponentType.Exclude<GhostInstance>() // или исключаем ваш корневой компонент машины
////        );
////    }

////    public void OnUpdate(ref SystemState state)
////    {
////        if (!m_DestroyedVehiclesQuery.IsEmptyIgnoreFilter)
////        {
////            var entities = m_DestroyedVehiclesQuery.ToEntityArray(Allocator.Temp);
////            var cleanupData = m_DestroyedVehiclesQuery.ToComponentDataArray<VoxelColliderCleanupMarker>(Allocator.Temp);

////            for (int i = 0; i < entities.Length; i++)
////            {
////                // 1. Безопасно удаляем сам Blob Asset из памяти, чтобы не было утечки
////                if (cleanupData[i].ColliderBlob.IsCreated)
////                {
////                    cleanupData[i].ColliderBlob.Dispose();
////                }

////                // 2. Полностью уничтожаем сущность (теперь её ничего не держит)
////                state.EntityManager.DestroyEntity(entities[i]);
////            }
////        }
////        // РАБОТАЕМ НАПРЯМУЮ: Ищем "зомби-сущности" машин, у которых Netcode/Физика уже удалили PhysicsCollider,
////        // но остался наш Cleanup-маркер. Без отложенных ECB, чтобы исключить рассинхронизацию при выходе!
////        using (var query = state.EntityManager.CreateEntityQuery(
////            ComponentType.ReadOnly<VoxelColliderCleanupMarker>(),
////            ComponentType.Exclude<PhysicsCollider>()))
////        {
////            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
////            var chunkData = query.ToComponentDataArray<VoxelColliderCleanupMarker>(Unity.Collections.Allocator.Temp);

////            for (int i = 0; i < entities.Length; i++)
////            {
////                Entity entity = entities[i];
////                var cleanup = chunkData[i];

////                //// 1. Мгновенно чистим Блобы дочерних чанков
////                //for (int c = 0; c < cleanup.ChildBlobs.Length; c++)
////                //{
////                //    if (cleanup.ChildBlobs[c].IsCreated)
////                //    {
////                //        try { cleanup.ChildBlobs[c].Dispose(); } catch { }
////                //    }
////                //}

////                // 2. Мгновенно чистим Блоб кузова машины
////                if (cleanup.ColliderBlob.IsCreated)
////                {
////                    try { cleanup.ColliderBlob.Dispose(); } catch { }
////                }

////                // 3. МГНОВЕННО удаляем маркер. Сущность сотрется из памяти ECS ТУТ ЖЕ
////                state.EntityManager.RemoveComponent<VoxelColliderCleanupMarker>(entity);
////            }
////        }
////    }

////    public void OnDestroy(ref SystemState state)
////    {
////        // БРОНИРОВАННЫЙ ВЫХОД (Без аллокаций массивов и без EntityManager):
////        // Когда мир закрывается, мы забираем данные напрямую из сырых чанков памяти ECS (Archetype Chunks).
////        // Этот метод работает на физическом уровне хранения таблиц Unity, поэтому он 
////        // 100% успевает вытащить последние 2 Блоба до того, как C++ ядро очистит указатели.
////        if (!m_CleanupQuery.IsEmpty)
////        {
////            var chunks = m_CleanupQuery.ToArchetypeChunkArray(Unity.Collections.Allocator.Temp);
////            var markerType = state.GetComponentTypeHandle<VoxelColliderCleanupMarker>(true);

////            for (int i = 0; i < chunks.Length; i++)
////            {
////                var chunk = chunks[i];
////                var markers = chunk.GetNativeArray(ref markerType);

////                for (int j = 0; j < markers.Length; j++)
////                {
////                    var cleanup = markers[j];

////                    //// Зачищаем дочерние Блобы чанков
////                    //for (int c = 0; c < cleanup.ChildBlobs.Length; c++)
////                    //{
////                    //    if (cleanup.ChildBlobs[c].IsCreated)
////                    //    {
////                    //        try { cleanup.ChildBlobs[c].Dispose(); } catch { }
////                    //    }
////                    //}


////                    // Зачищаем корень составного коллидера машины
////                    if (cleanup.ColliderBlob.IsCreated)
////                    {
////                        try { cleanup.ColliderBlob.Dispose(); } catch { }
////                    }
////                }
////            }
////        }
////    }
////}

//////using Unity.Entities;
//////using Unity.Physics;

//////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//////[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)] // В самом конце кадра
//////public partial struct VoxelColliderCleanupSystem : ISystem
//////{
//////    // Убираем BurstCompile, так как Dispose() Blob-ассетов на ECB выполняется в управляемом потоке
//////    public void OnUpdate(ref SystemState state)
//////    {
//////        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//////                           .CreateCommandBuffer(state.WorldUnmanaged);

//////        // Ищем сущности-призраки (машина удалена из ECS, PhysicsCollider пропал, маркер остался)
//////        foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<VoxelColliderCleanupMarker>>()
//////                     .WithNone<PhysicsCollider>()
//////                     .WithEntityAccess())
//////        {
//////            // 1. Освобождаем память дочерних чанков машины
//////            for (int c = 0; c < cleanup.ValueRO.ChildBlobs.Length; c++)
//////            {
//////                try
//////                {

//////                    if (cleanup.ValueRO.ChildBlobs[c].IsCreated)
//////                    {
//////                        cleanup.ValueRO.ChildBlobs[c].Dispose();
//////                    }
//////                }
//////                catch
//////                {

//////                }
//////            }

//////            // 2. Освобождаем память корня составного коллайдера
//////            if (cleanup.ValueRO.ColliderBlob.IsCreated)
//////            {
//////                cleanup.ValueRO.ColliderBlob.Dispose();
//////            }

//////            // 3. Позволяем ECS окончательно стереть сущность
//////            ecb.RemoveComponent<VoxelColliderCleanupMarker>(entity);
//////        }
//////    }

//////    public void OnDestroy(ref SystemState state)
//////    {
//////        // Защищенная очистка всех оставшихся машин при выходе из игры
//////        foreach (var cleanup in SystemAPI.Query<RefRO<VoxelColliderCleanupMarker>>())
//////        {
//////            // 1. Чистим дочерние чанки
//////            for (int c = 0; c < cleanup.ValueRO.ChildBlobs.Length; c++)
//////            {
//////                var childBlob = cleanup.ValueRO.ChildBlobs[c];
//////                if (childBlob.IsCreated)
//////                {
//////                    try
//////                    {
//////                        // Пробуем безопасно удалить. Если он уже удален в другом месте, 
//////                        // Unity выкинет InvalidOperationException, которое мы перехватим.
//////                        childBlob.Dispose();
//////                    }
//////                    catch (System.InvalidOperationException)
//////                    {
//////                        // Игнорируем: ассет уже был успешно выгружен ранее
//////                    }
//////                }
//////            }

//////            // 2. Чистим корень составного коллайдера машины
//////            var rootBlob = cleanup.ValueRO.ColliderBlob;
//////            if (rootBlob.IsCreated)
//////            {
//////                try
//////                {
//////                    rootBlob.Dispose();
//////                }
//////                catch (System.InvalidOperationException)
//////                {
//////                    // Игнорируем повторное удаление
//////                }
//////            }
//////        }
//////    }
//////}

////////using Unity.Burst;
////////using Unity.Entities;
////////using Unity.Physics;

////////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
////////[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)] // В самом конце кадра
////////public partial struct VoxelColliderCleanupSystem : ISystem
////////{
////////    //[BurstCompile]
////////    //public void OnCreate(ref SystemState state)
////////    //{
////////    //    state.RequireForUpdate<VoxelColliderCleanupMarker>();
////////    //}

////////    [BurstCompile]
////////    public void OnUpdate(ref SystemState state)
////////    {
////////        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
////////                           .CreateCommandBuffer(state.WorldUnmanaged);

////////        // Ищем сущности, у которых ЕСТЬ маркер, но НЕТ компонента PhysicsCollider 
////////        // (это значит, что сущность была уничтожена)
////////        foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<VoxelColliderCleanupMarker>>()
////////                     .WithNone<PhysicsCollider>()
////////                     .WithEntityAccess())
////////        {
////////            // На всякий случай проверяем, вдруг остался сам компонент, но его удалили отдельно
////////            if (SystemAPI.HasComponent<PhysicsCollider>(entity))
////////            {
////////                var collider = SystemAPI.GetComponent<PhysicsCollider>(entity);
////////                if (collider.IsValid && collider.Value.IsCreated)
////////                {
////////                    collider.Value.Dispose();
////////                }
////////            }

////////            // Убираем маркер, позволяя ECS окончательно удалить сущность из памяти
////////            ecb.RemoveComponent<VoxelColliderCleanupMarker>(entity);
////////        }
////////    }
////////}