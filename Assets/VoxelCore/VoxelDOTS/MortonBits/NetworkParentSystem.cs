


using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// ====================================================================
// ЖЕЛЕЗОБЕТОННЫЙ ФИКС СОРТИРОВКИ ДЛЯ NETWORK_PARENT_SYSTEM:
// Отправляем систему жить прямо внутрь встроенной TransformSystemGroup.
// Теперь мы находимся в одной группе со встроенной ParentSystem, 
// и планировщик Unity DOTS сможет выстроить порядок кадра без варнингов!
// ====================================================================
[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(ParentSystem))] // Выполняемся строго ДО того, как ядро начнет собирать кэш матриц
public partial struct NetworkParentSystem : ISystem
{
    private ComponentLookup<GhostInstance> m_GhostInstanceLookup;

    public void OnCreate(ref SystemState state)
    {
        m_GhostInstanceLookup = state.GetComponentLookup<GhostInstance>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        m_GhostInstanceLookup.Update(ref state);

        var modelCache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. Собираем все потенциальные родительские машины с GhostInstance
        var allGhostsQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance>().Build();
        var ghostEntities = allGhostsQuery.ToEntityArray(Allocator.Temp);

        // 2. Ищем чанки, требующие привязки к родителю
        foreach (var (netParent, modelHeader, chunkEntity) in SystemAPI.Query<RefRO<NetworkParent>, RefRO<VoxelModelHeader>>()
                     .WithNone<Parent>()
                     .WithEntityAccess())
        {
            Entity foundVehicleEntity = Entity.Null;

            // 3. Сопоставляем ghostId
            for (int i = 0; i < ghostEntities.Length; i++)
            {
                Entity vehicleEntity = ghostEntities[i];

                if (m_GhostInstanceLookup.TryGetComponent(vehicleEntity, out GhostInstance vehicleInstance))
                {
                    if (vehicleInstance.ghostId == netParent.ValueRO.ParentGhostId)
                    {
                        foundVehicleEntity = vehicleEntity;
                        break;
                    }
                }
            }


            // 4. Если машина найдена, жестко собираем локальную иерархию
            // 4. Если машина нашлась в мире клиента, связываем их стандартным Parent
            if (foundVehicleEntity != Entity.Null && state.EntityManager.Exists(foundVehicleEntity))
            {
                var rootData = SystemAPI.GetComponent<VoxelModelHeader>(chunkEntity);
                var ghostInstanceComponent = SystemAPI.GetComponent<GhostInstance>(chunkEntity);
                // Ищем шаблон в unmanaged-кэше синглтона по хэшу
                if (!modelCache.Templates.TryGetValue(rootData.ConfigHashName, out var template))
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError($"[NetworkParent]: Модель с хэшем {rootData.ConfigHashName} не найдена в кэше при спавне чанков!");
#endif
                    continue;
                }

                // планируем получение маски разрушений.
                // Легально и безопасно получаем сущность сетевого соединения
                Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkId>();
                Entity rpcRequestMask = ecb.CreateEntity();
                //#if UNITY_EDITOR
                //                UnityEngine.Debug.Log($"[NetworkParent]: Создаем RPC для запроса маски изменений для ghostId={ghostInstanceComponent.ghostId}!");
                //#endif
                ecb.AddComponent(rpcRequestMask, new RequestMaskFromServerRpc
                {
                    GhostId = (uint)ghostInstanceComponent.ghostId,
                });
                ecb.AddComponent(rpcRequestMask, new SendRpcCommandRequest { TargetConnection = connectionEntity });


                // Связываем иерархию в ECS
                ecb.AddComponent(chunkEntity, new Parent { Value = foundVehicleEntity });
                //ecb.AddComponent(chunkEntity, new PreviousParent { Value = foundVehicleEntity });

                // ====================================================================
                // ЖЕЛЕЗОБЕТОННАЯ ПРОВЕРКА БУФЕРА ИЕРАРХИИ:
                // Если у сущности машины на клиенте еще нет буфера LinkedEntityGroup, 
                // создаем его, при этом ПЕРВЫМ элементом буфер ОБЯЗАН содержать саму машину!
                // ====================================================================
                if (!SystemAPI.HasBuffer<LinkedEntityGroup>(foundVehicleEntity))
                {
                    ecb.AddBuffer<LinkedEntityGroup>(foundVehicleEntity);
                    // Каноничное правило Unity: первый элемент LinkedEntityGroup — это всегда Self (сама сущность)
                    ecb.AppendToBuffer(foundVehicleEntity, new LinkedEntityGroup { Value = foundVehicleEntity });
                }

                ecb.AppendToBuffer(foundVehicleEntity, new LinkedEntityGroup { Value = chunkEntity });

                if (SystemAPI.HasComponent<ChunkIndexComponent>(chunkEntity))
                {
                    var chunkIndex = SystemAPI.GetComponent<ChunkIndexComponent>(chunkEntity);

                    //// Математически восстанавливаем чистый локальный оффсет
                    //float3 localOffset = (float3)chunkIndex.Value * 32.0f * 1.0f;
                    // 1. Вычисляем исходный базовый оффсет чанка (от угла)
                    float3 baseLocalOffset = (float3)chunkIndex.Value * 32.0f * 1.0f;

                    // ====================================================================
                    // КАНOНИЧНЫЙ ФИКС ПИВОТА НА КЛИЕНТЕ:
                    // Вычисляем абсолютно идентичный pivotOffset, как на сервере!
                    // Замените modelSizeInChunks на реальные габариты модели вашей машины.
                    // ====================================================================
                    float3 pivotOffset = new float3(
                        (template.SizeModel.x * 32f) / 2f,
                        0f, // Оставляем 0, чтобы дно меша совпало с дном коллайдера
                        (template.SizeModel.z * 32f) / 2f
                    );

                    // Вычитаем смещение пивота, возвращая меш чанка в центр!
                    float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

                    // 1. Записываем локальный трансформ
                    var localTransform = LocalTransform.FromPositionRotationScale(
                        localOffsetWithPivot,
                        quaternion.identity,
                        1.0f
                    );
                    ecb.SetComponent(chunkEntity, localTransform);

                    // 2. ЖЕСТКИЙ ПРИНУДИТЕЛЬНЫЙ ПЕРЕСЧЕТ МАТРИЦЫ ДЛЯ КЛИЕНТА:
                    // Если у родительской машины уже есть готовая матрица в мире
                    if (SystemAPI.HasComponent<LocalToWorld>(foundVehicleEntity))
                    {
                        var parentL2W = SystemAPI.GetComponent<LocalToWorld>(foundVehicleEntity);

                        // Напрямую рассчитываем итоговую мировую матрицу: МирРодителя * ЛокальныйОффсетЧанка
                        float4x4 forcedWorldMatrix = math.mul(parentL2W.Value, localTransform.ToMatrix());

                        // Вручную вбиваем её в чанк через ECB!
                        // Это выведет матрицу из дефолтных нулей и мгновенно поставит чанк на его физическое место.
                        ecb.SetComponent(chunkEntity, new LocalToWorld { Value = forcedWorldMatrix });
                    }
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}


//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Transforms;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//// Переносим систему в официальную группу предсказанной симуляции Netcode.
//// Никаких [UpdateAfter] здесь писать НЕ НУЖНО. Варнинг исчезнет!
//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//public partial struct NetworkParentSystem : ISystem
//{
//    private ComponentLookup<GhostInstance> m_GhostInstanceLookup;

//    public void OnCreate(ref SystemState state)
//    {
//        m_GhostInstanceLookup = state.GetComponentLookup<GhostInstance>(true);
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        m_GhostInstanceLookup.Update(ref state);

//        var modelCache = SystemAPI.GetSingleton<GlobalVoxelModelCache>();

//        // Создаем локальный ECB, который применится МГНОВЕННО на выходе из группы спавна,
//        // до того, как системы физики и предсказания начнут тикать кадрами!
//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        // 1. Кэшируем все существующие машины с GhostInstance в массив
//        var allGhostsQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance>().Build();
//        var ghostEntities = allGhostsQuery.ToEntityArray(Allocator.Temp);

//        // 2. ИЩЕМ ТОЛЬКО НОВЫЕ ЧАНКИ, КОТОРЫЕ ЕЩЕ НЕ БЫЛИ СВЯЗАНЫ
//        // Фильтр WithNone<VoxelChunkLinkedTag> гарантирует, что этот код 
//        // выполнится для каждого чанка РОВНО ОДИН РАЗ в момент его сетевого рождения!
//        foreach (var (netParent, modelHeader, chunkEntity) in
//                 SystemAPI.Query<RefRO<NetworkParent>, RefRO<VoxelModelHeader>>()
//                     .WithNone<VoxelChunkLinkedTag>()
//                     //.WithNone<Parent>()
//                     .WithEntityAccess())
//        {
//            Entity foundVehicleEntity = Entity.Null;

//            // 3. Сопоставляем ParentGhostId
//            for (int i = 0; i < ghostEntities.Length; i++)
//            {
//                Entity vehicleEntity = ghostEntities[i];
//                if (m_GhostInstanceLookup.TryGetComponent(vehicleEntity, out GhostInstance vehicleInstance))
//                {
//                    if (vehicleInstance.ghostId == netParent.ValueRO.ParentGhostId)
//                    {
//                        foundVehicleEntity = vehicleEntity;
//                        break;
//                    }
//                }
//            }

//            if (foundVehicleEntity != Entity.Null && state.EntityManager.Exists(foundVehicleEntity))
//            {
//                if (!modelCache.Templates.TryGetValue(modelHeader.ValueRO.ConfigHashName, out var template))
//                {
//                    continue;
//                }

//                // =========================================================================
//                // БЕЗОПАСНОЕ ВОССТАНОВЛЕНИЕ ИЕРАРХИИ (В ОДИН СЕТЕВОЙ КАДР РОЖДЕНИЯ):
//                // =========================================================================

//                // 1. Помечаем чанк тегом привязки. Больше этот чанк в цикл никогда не попадет!
//                // Нагрузка на систему падает до нуля, когда модели уже заспавнены.
//                ecb.AddComponent<VoxelChunkLinkedTag>(chunkEntity);

//                // // 2. Физически восстанавливаем иерархию ядра Unity Transforms
//                ecb.AddComponent(chunkEntity, new Parent { Value = foundVehicleEntity });

//                // ИСПРАВЛЕНИЕ: Вместо старых позиций и ротаций проверяем и добавляем единый LocalTransform
//                // Если его вдруг не было на сетевом префабе чанка, добавляем дефолтный (Identity)
//                if (!state.EntityManager.HasComponent<LocalTransform>(chunkEntity))
//                {
//                    ecb.AddComponent(chunkEntity, LocalTransform.Identity);
//                }

//                // 3. Восстанавливаем LinkedEntityGroup у родительской машины
//                if (!state.EntityManager.HasBuffer<LinkedEntityGroup>(foundVehicleEntity))
//                {
//                    ecb.AddBuffer<LinkedEntityGroup>(foundVehicleEntity);
//                    ecb.AppendToBuffer(foundVehicleEntity, new LinkedEntityGroup { Value = foundVehicleEntity });
//                }
//                ecb.AppendToBuffer(foundVehicleEntity, new LinkedEntityGroup { Value = chunkEntity });

//                // =========================================================================
//                // ВАША МАТЕМАТИКА СДВИГА ПО ПИВОТАМ
//                // =========================================================================
//                if (state.EntityManager.HasComponent<ChunkIndexComponent>(chunkEntity))
//                {
//                    var chunkIndex = state.EntityManager.GetComponentData<ChunkIndexComponent>(chunkEntity);

//                    float3 baseLocalOffset = (float3)chunkIndex.Value * 32.0f;
//                    float3 pivotOffset = new float3((template.SizeModel.x * 32f) / 2f, 0f, (template.SizeModel.z * 32f) / 2f);
//                    float3 localOffsetWithPivot = baseLocalOffset - pivotOffset;

//                    var localTransform = LocalTransform.FromPositionRotationScale(localOffsetWithPivot, quaternion.identity, 1.0f);
//                    ecb.SetComponent(chunkEntity, localTransform);

//                    if (state.EntityManager.HasComponent<LocalToWorld>(foundVehicleEntity))
//                    {
//                        var parentL2W = state.EntityManager.GetComponentData<LocalToWorld>(foundVehicleEntity);
//                        float4x4 forcedWorldMatrix = math.mul(parentL2W.Value, localTransform.ToMatrix());
//                        ecb.SetComponent(chunkEntity, new LocalToWorld { Value = forcedWorldMatrix });
//                    }
//                }
//            }
//        }

//        // Мгновенный плейбэк в безопасной точке сетевого кадра
//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}

