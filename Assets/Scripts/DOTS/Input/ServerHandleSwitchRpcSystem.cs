using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

public struct RequestVehicleSwitchRpc : IRpcCommand
{
    public uint IdOldEntity;
    public uint IdNewEntity;
}

// ТЕГ-МАРКЕР
[GhostComponent]
public struct IsControlledTag : IComponentData, IEnableableComponent
{
    // Помечаем поле атрибутом репликации. 
    // Изменение этого флага на сервере Netcode гарантированно пустит в сеть!
    [GhostField] public bool IsActive;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerHandleSwitchRpcSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Собираем все сетевые госты (машины), существующие в мире СЕРВЕРА
        var serverGhostsQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwner>().Build();
        var serverGhostEntities = serverGhostsQuery.ToEntityArray(Allocator.Temp);
        var serverGhostInstances = serverGhostsQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. Перебираем прилетевшие от клиентов RPC-запросы
        foreach (var (rpcHeader, request, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RequestVehicleSwitchRpc>>()
                     //.WithAll<RequestVehicleSwitchRpc>()
                     .WithEntityAccess())
        {
            // Извлекаем соединение
            Entity clientConnection = rpcHeader.ValueRO.SourceConnection;

            // извлекаем данные
            uint newGhostId = request.ValueRO.IdNewEntity;
            uint oldGhostId = request.ValueRO.IdOldEntity;
            //#if UNITY_EDITOR
            //            UnityEngine.Debug.Log($"[Server]: Получем RPC для смены машины с {oldGhostId} на {newGhostId}!");
            //#endif
            // ИСПРАВЛЕНИЕ: Используем NetworkId вместо устаревшего NetworkIdComponent
            if (!state.EntityManager.HasComponent<NetworkId>(clientConnection))
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            // ИСПРАВЛЕНИЕ: Читаем данные через новый тип NetworkId
            int clientNetworkId = state.EntityManager.GetComponentData<NetworkId>(clientConnection).Value;


            Entity targetVehicleEntity = Entity.Null;
            // ====================================================================
            // КАНОНИЧНАЯ ЗАМЕНА GHOST_LOOKUP:
            // Прямой поиск нужной серверной Entity по её числовому uint ghostId!
            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
            // ====================================================================
            for (int i = 0; i < serverGhostInstances.Length; i++)
            {
                if ((uint)serverGhostInstances[i].ghostId == newGhostId)
                {
                    targetVehicleEntity = serverGhostEntities[i];
                    break;
                }
            }
            // ====================================================================

            if (targetVehicleEntity != Entity.Null && state.EntityManager.Exists(targetVehicleEntity))
            {
                // Сверяем права безопасности, чтобы один игрок не угнал чужую воксельную машину
                GhostOwner vehicleOwner = state.EntityManager.GetComponentData<GhostOwner>(targetVehicleEntity);

                if (vehicleOwner.NetworkId == clientNetworkId)
                {
                    // Безопасность пройдена! Очищаем IsControlledTag со ВСЕХ старых машин этого игрока на сервере
                    foreach (var (owner, vehicle) in SystemAPI.Query<GhostOwner>().WithAll<IsControlledTag>().WithEntityAccess())
                    {
                        if (owner.NetworkId == clientNetworkId)
                        {
                            ecb.SetComponent(vehicle, new IsControlledTag { IsActive = false });
                        }
                    }

                    // Вешаем тег активного управления на новую одобренную сервером машину
                    ecb.SetComponent<IsControlledTag>(targetVehicleEntity, new IsControlledTag { IsActive = true });
                }
            }


            //Entity currentVehicle = Entity.Null;
            //Entity nextVehicle = Entity.Null;

            //// 2. Ищем, какой машиной этот клиент владеет сейчас на сервере
            //foreach (var (ghostOwner, vehicleEntity) in SystemAPI.Query<RefRO<GhostOwner>>()
            //             .WithEntityAccess())
            //{
            //    // Проверяем, является ли эта сущность машиной (есть ли нужный компонент движения)
            //    if (SystemAPI.HasComponent<AAA_InputComponent>(vehicleEntity))
            //    {
            //        if (ghostOwner.ValueRO.NetworkId == clientNetworkId)
            //        {
            //            currentVehicle = vehicleEntity;
            //            break;
            //        }
            //    }
            //}

            //if (currentVehicle == Entity.Null)
            //{
            //    UnityEngine.Debug.LogWarning($"[Server]: currentVehicle = null (clientNetworkId={clientNetworkId})");
            //}
            //else
            //{
            //    ecb.AddComponent<IsControlledTag>(currentVehicle);
            //}

            //// 3. Ищем СЛЕДУЮЩУЮ свободную или чужую машину на сервере для переключения
            //foreach (var (ghostOwner, vehicleEntity) in SystemAPI.Query<RefRO<GhostOwner>>()
            //             .WithEntityAccess())
            //{
            //    if (SystemAPI.HasComponent<AAA_InputComponent>(vehicleEntity) && vehicleEntity != currentVehicle)
            //    {
            //        // Если машина ничья (NetworkId == 0) или принадлежит не нам — выбираем её
            //        if (ghostOwner.ValueRO.NetworkId == 0 || ghostOwner.ValueRO.NetworkId != clientNetworkId)
            //        {
            //            nextVehicle = vehicleEntity;
            //            break;
            //        }
            //    }
            //}
            //if (nextVehicle == Entity.Null)
            //{
            //    UnityEngine.Debug.LogWarning("[Server]: nextVehicle = null");
            //}

            //// 4. АВТОРИТАРНОЕ СЕРВЕРНОЕ ПЕРЕКЛЮЧЕНИЕ
            //if (nextVehicle != Entity.Null)
            //{
            //    // Снимаем владение со старой машины
            //    if (currentVehicle != Entity.Null)
            //    {
            //        ecb.SetComponent(currentVehicle, new GhostOwner { NetworkId = 0 });
            //    }

            //    // Передаем новую машину клиенту
            //    ecb.SetComponent(nextVehicle, new GhostOwner { NetworkId = clientNetworkId });

            //    UnityEngine.Debug.Log($"[Сервер] Клиент {clientNetworkId} успешно пересажен в машину {nextVehicle.Index}");
            //}

            // Уничтожаем сущность обработанного RPC пакета
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

}
