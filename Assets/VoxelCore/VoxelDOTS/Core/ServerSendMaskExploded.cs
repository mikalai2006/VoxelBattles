using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerSendMaskExploded : ISystem
{
    private EntityQuery m_ChunkQuery;

    public void OnCreate(ref SystemState state)
    {
        // Кэшируем запрос для быстрого поиска чанков по их хэш-имени
        m_ChunkQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<VoxelModelHeader>(),
            ComponentType.ReadOnly<NetworkParent>(),
            ComponentType.ReadOnly<LocalChunkDestructionMask>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        // Используем EntityCommandBuffer для безопасного создания RPC-ответов и удаления запросов
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. Быстро ищем сущность чанка на сервере по его хэшу
        var chunkEntities = m_ChunkQuery.ToEntityArray(Allocator.Temp);
        var chunkNames = m_ChunkQuery.ToComponentDataArray<NetworkParent>(Allocator.Temp);

        // Перебираем все пришедшие от клиентов RPC-запросы
        foreach (var (request, rpcSource, rpcEntity) in SystemAPI.Query<RequestMaskFromServerRpc, ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            uint requestedGhostInstance = request.GhostInstance;

            // Итерируемся по ВСЕМ чанкам, зарегистрированным на сервере
            for (int i = 0; i < chunkNames.Length; i++)
            {
                // Находим КАЖДЫЙ чанк, у которого совпадает хэш конфигурации
                if (chunkNames[i].ParentGhostId == requestedGhostInstance)
                {
                    Entity foundChunkEntity = chunkEntities[i];

                    // Дополнительная проверка на случай, если чанк был удален во время кадра
                    if (state.EntityManager.Exists(foundChunkEntity))
                    {
                        var destructionMask = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(foundChunkEntity);

                        // Создаем структуру ответа для конкретного чанка
                        var replyRpc = new ReplyMaskToClientRpc
                        {
                            GhostInstance = requestedGhostInstance
                        };

                        // Вызываем наш SAFE статический RLE-компрессор
                        ChunkRleSerializer.CompressToRle(destructionMask.AsNativeArray(), ref replyRpc.CompressedBytes);

                        // Создаем ECS-сущность сетевой команды ответа
                        Entity responseEntity = ecb.CreateEntity();
                        ecb.AddComponent(responseEntity, replyRpc);

                        // Отправляем ответ СТРОГО ТОМУ КЛИЕНТУ, который сделал запрос
                        ecb.AddComponent(responseEntity, new SendRpcCommandRequest
                        {
                            TargetConnection = rpcSource.SourceConnection
                        });
                        //UnityEngine.Debug.LogWarning($"[Server]: Отправляем маску для {requestedGhostInstance}");
                    }

                    // Убрали break! Продолжаем искать остальные чанки с таким же хэшем
                }
            }

            // Уничтожаем сущность входящего запроса, так как мы полностью ответили по всем чанкам
            ecb.DestroyEntity(rpcEntity);
        }
        //// Перебираем все пришедшие от клиентов RPC-запросы
        //// ReceiveRpcCommandRequest хранит информацию о том, КАКОЙ конкретно клиент прислал пакет
        //foreach (var (request, rpcSource, rpcEntity) in SystemAPI.Query<
        //    RequestMaskFromServerRpc,
        //    ReceiveRpcCommandRequest
        //>()
        //    .WithEntityAccess())
        //{
        //    uint requestedHash = request.hashName;
        //    Entity foundChunkEntity = Entity.Null;

        //    for (int i = 0; i < chunkNames.Length; i++)
        //    {
        //        if (chunkNames[i].ConfigHashName == requestedHash)
        //        {
        //            foundChunkEntity = chunkEntities[i];
        //            break;
        //        }
        //    }

        //    // 2. Если чанк найден и у него есть маска — сжимаем её и отправляем обратно
        //    if (foundChunkEntity != Entity.Null)
        //    {
        //        var destructionMask = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(foundChunkEntity);

        //        // Создаем структуру ответа
        //        var replyRpc = new ReplyMaskToClientRpc
        //        {
        //            HashName = requestedHash
        //        };

        //        // Вызываем наш SAFE статический RLE-компрессор
        //        ChunkRleSerializer.CompressToRle(destructionMask.AsNativeArray(), ref replyRpc.CompressedBytes);

        //        // Создаем ECS-сущность сетевой команды ответа
        //        Entity responseEntity = ecb.CreateEntity();
        //        ecb.AddComponent(responseEntity, replyRpc);

        //        // КРИТИЧЕСКИ ВАЖНО: Отправляем ответ СТРОГО ТОМУ КЛИЕНТУ, который отправил запрос.
        //        // Передаем rpcSource.SourceConnection вместо Entity.Null, чтобы не спамить broadcast-ом на весь сервер.
        //        ecb.AddComponent(responseEntity, new SendRpcCommandRequest
        //        {
        //            TargetConnection = rpcSource.SourceConnection
        //        });
        //    }

        //    // 3. Уничтожаем сущность входящего запроса, чтобы не обрабатывать её в следующем кадре
        //    ecb.DestroyEntity(rpcEntity);
        //}

        // Освобождаем временные массивы поиска
        chunkEntities.Dispose();
        chunkNames.Dispose();

        // Применяем все изменения транзакции
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}