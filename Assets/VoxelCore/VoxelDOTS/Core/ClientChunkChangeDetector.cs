//using Unity.Collections;
//using Unity.Entities;
//using Unity.NetCode;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Работает строго на клиенте
//// КРИТИЧЕСКИ ВАЖНО: Встаем в группу сетевой симуляции ПОСЛЕ того, как Netcode применил данные из сети
//[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
//[UpdateAfter(typeof(GhostUpdateSystem))]
//public partial struct ClientChunkChangeDetector : ISystem
//{
//    private EntityQuery m_ChangedMasksQuery;

//    public void OnCreate(ref SystemState state)
//    {
//        // 1. Собираем запрос на чанки, у которых есть маска деструкции.
//        // Также добавляем компоненты-флаги в WithAny/WithAll, чтобы убедиться, что они есть на архетипе.
//        m_ChangedMasksQuery = new EntityQueryBuilder(Allocator.Temp)
//            .WithAll<LocalChunkDestructionMask>()
//        .Build(ref state);

//        // 2. КРИТИЧЕСКИ ВАЖНО: Настраиваем Change Filter.
//        // Запрос вернет чанк ТОЛЬКО в том кадре, когда NetCode запишет в него обновленную с сервера маску.
//        m_ChangedMasksQuery.SetChangedVersionFilter(typeof(LocalChunkDestructionMask));
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        // 1. Проверяем изменения с учетом фильтра версий. 
//        if (m_ChangedMasksQuery.IsEmpty) return;

//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        // Считываем чанки памяти
//        var chunkArray = m_ChangedMasksQuery.ToArchetypeChunkArray(Allocator.Temp);
//        var entityType = state.GetEntityTypeHandle();

//        // Получаем хэндл именно ДЛЯ БУФЕРА
//        var bufferTypeHandle = state.GetBufferTypeHandle<LocalChunkDestructionMask>();


//        // Получаем хэндлы для проверки: включены ли уже компоненты генерации
//        var meshNeedCreateHandle = state.GetComponentTypeHandle<ChunkMeshNeedCreate>();
//        var colliderNeedCreateHandle = state.GetComponentTypeHandle<ChunkColliderNeedCreate>();


//        // Внешний цикл по чанкам памяти: пока i МЕНЬШЕ, чем chunkArray.Length
//        for (int i = 0; i < chunkArray.Length; i++)
//        {
//            var chunk = chunkArray[i];

//            // Проверяем, изменился ли этот конкретный чанк
//            if (!chunk.DidChange(ref bufferTypeHandle, state.LastSystemVersion))
//            {
//                continue;
//            }

//            var entities = chunk.GetNativeArray(entityType);

//            // Внутренний цикл по сущностям внутри чанка: пока j МЕНЬШЕ, чем entities.Length
//            for (int j = 0; j < entities.Length; j++)
//            {
//                // ИСПРАВЛЕНИЕ: Используем официальный метод chunk.IsComponentEnabled
//                // Передаем хэндл типа компонента и индекс j сущности внутри чанка
//                bool isMeshEnabled = chunk.IsComponentEnabled(ref meshNeedCreateHandle, j);
//                bool isColliderEnabled = chunk.IsComponentEnabled(ref colliderNeedCreateHandle, j);

//                // Если флаги генерации у этой сущности уже взведены — пропускаем ее,
//                // чтобы остановить бесконечный цикл обновлений и спам логов.
//                if (isMeshEnabled || isColliderEnabled)
//                {
//                    continue;
//                }

//                Entity chunkEntity = entities[j];

//                // Отложенно взводим локальные флаги для генератора мешей и коллайдеров
//                ecb.SetComponentEnabled<ChunkMeshNeedCreate>(chunkEntity, true);
//                ecb.SetComponentEnabled<ChunkColliderNeedCreate>(chunkEntity, true);

//                UnityEngine.Debug.Log($"[Client Trigger]: Сетевой буфер маски чанка {chunkEntity} изменился! Флаги взведены.");
//            }
//        }

//        // Освобождаем память и применяем команды
//        chunkArray.Dispose();
//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}
