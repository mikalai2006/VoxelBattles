using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
public struct VisualsReplyMaskTag : IComponentData { }


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientHandleMaskReplySystem : ISystem
{
    private EntityQuery m_ChunkQuery;

    public void OnCreate(ref SystemState state)
    {
        // Кэшируем запрос для быстрого поиска локальных чанков на клиенте по хэшу.
        // Ищем чанки, у которых уже есть буфер маски разрушений.
        m_ChunkQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<VoxelModelHeader>(),
            ComponentType.ReadOnly<GhostInstance>(),
            ComponentType.ReadWrite<LocalChunkDestructionMask>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        // Используем EntityCommandBuffer для безопасного удаления обработанных RPC и добавления тегов
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. Ищем сущность чанка на клиенте по его хэшу
        var chunkEntities = m_ChunkQuery.ToEntityArray(Allocator.Temp);
        var ghostInstanceComponents = m_ChunkQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);

        // Перебираем все пришедшие с сервера ответы
        foreach (var (replyData, rpcEntity) in SystemAPI.Query<ReplyMaskToClientRpc>().WithEntityAccess())
        {
            uint replyGhostId = replyData.GhostId;

            // Клонируем сжатые байты в локальную переменную (фикс CS1655)
            var compressedBytes = replyData.CompressedBytes;

            // Итерируемся по ВСЕМ чанкам в мире
            for (int i = 0; i < ghostInstanceComponents.Length; i++)
            {
                // Находим КАЖДЫЙ чанк, у которого совпадает хэш
                if (ghostInstanceComponents[i].ghostId == replyGhostId)
                {
                    Entity foundChunkEntity = chunkEntities[i];

                    if (state.EntityManager.Exists(foundChunkEntity))
                    {
                        // Получаем доступ к буферу маски конкретного чанка
                        var clientBuffer = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(foundChunkEntity);

                        // Распаковываем RLE в буфер этого чанка
                        ChunkRleSerializer.DecompressFromRle(in compressedBytes, clientBuffer);
#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"[Client]: Reply: Меняем маску разрушений для ghostId={ghostInstanceComponents[i].ghostId}[{replyGhostId}]!");
#endif
                        // Помечаем этот конкретный чанк тегом для обновления меша/коллайдера
                        ecb.AddComponent<VisualsReplyMaskTag>(foundChunkEntity);

                        ecb.SetComponentEnabled<ChunkMeshNeedCreate>(foundChunkEntity, true);
                        ecb.SetComponentEnabled<ChunkColliderNeedCreate>(foundChunkEntity, true);
                    }

                    // Убираем break! Цикл пойдет дальше и найдет следующие чанки с этим же хэшем
                }
            }

            // Уничтожаем сущность RPC сообщения, так как мы применили его ко всем копиям
            ecb.DestroyEntity(rpcEntity);
        }

        //// Перебираем все пришедшие с сервера ответы ReplyMaskToClientRpc
        //foreach (var (replyData, rpcEntity) in SystemAPI.Query<ReplyMaskToClientRpc>().WithEntityAccess())
        //{
        //    uint replyHash = replyData.HashName;
        //    Entity foundChunkEntity = Entity.Null;


        //    for (int i = 0; i < chunkNames.Length; i++)
        //    {
        //        if (chunkNames[i].ConfigHashName == replyHash)
        //        {
        //            foundChunkEntity = chunkEntities[i];
        //            break;
        //        }
        //    }

        //    // 2. Если чанк найден на клиенте — восстанавливаем его буфер из RLE
        //    if (foundChunkEntity != Entity.Null)
        //    {
        //        // Получаем доступ к буферу маски чанка на клиенте
        //        var clientBuffer = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(foundChunkEntity);

        //        // ИСПРАВЛЕНИЕ: Копируем сжатые байты в локальную переменную, чтобы обойти ограничение foreach
        //        var compressedBytes = replyData.CompressedBytes;

        //        // Вызываем ваш оригинальный SAFE статический метод распаковки для NativeArray буфера
        //        ChunkRleSerializer.DecompressFromRle(ref compressedBytes, clientBuffer.AsNativeArray());

        //        // 3. Вешаем тег-маркер "МЕШ ТРЕБУЕТ ОБНОВЛЕНИЯ"
        //        // Ваши системы генерации меша/коллайдера должны отлавливать этот тег, 
        //        // перестраивать геометрию и затем снимать его.
        //        ecb.AddComponent<VisualsReplyMaskTag>(foundChunkEntity);

        //        ecb.SetComponentEnabled<ChunkMeshNeedCreate>(foundChunkEntity, true);
        //        ecb.SetComponentEnabled<ChunkColliderNeedCreate>(foundChunkEntity, true);
        //    }

        //    // 4. Уничтожаем сущность сетевого ответа, чтобы не обрабатывать её повторно
        //    ecb.DestroyEntity(rpcEntity);
        //}

        // Освобождаем временные массивы поиска чанка
        chunkEntities.Dispose();
        ghostInstanceComponents.Dispose();

        // Применяем отложенные команды кадра
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
