using Unity.Burst;
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
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile] // <-- КРИТИЧЕСКИ ВАЖНО: Вешаем Burst на всю систему! 
public partial struct ServerHandleSwitchRpcSystem : ISystem
{
    // Вместо EntityManager.HasComponent используем эффективные lookup-хранилища,
    // которые идеально работают внутри Burst и не создают GC.Alloc мусора.
    private ComponentLookup<NetworkId> _networkIdLookup;
    private BufferLookup<InputBufferData<AAA_InputComponent>> _inputBufferLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RequestVehicleSwitchRpc>();

        // Инициализируем нативные lookup-кэши
        _networkIdLookup = state.GetComponentLookup<NetworkId>(true); // ReadOnly
        _inputBufferLookup = state.GetBufferLookup<InputBufferData<AAA_InputComponent>>(false); // ReadWrite
    }

    [BurstCompile] // Заставляем Burst полностью скомпилировать OnUpdate в машинный код
    public void OnUpdate(ref SystemState state)
    {
        // Обновляем lookup-данные для текущего кадра без блокировки потоков джоб
        _networkIdLookup.Update(ref state);
        _inputBufferLookup.Update(ref state);

        // 1. Запрашиваем нативный синглтон фабрики буферов для группы симуляции
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        // 2. Создаем полностью Burst-совместимый буфер команд одной строчкой!
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Перебираем прилетевшие RPC-запросы
        foreach (var (rpcHeader, request, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RequestVehicleSwitchRpc>>().WithEntityAccess())
        {
            Entity clientConnection = rpcHeader.ValueRO.SourceConnection;

            // БЕЗОПАСНАЯ ПРОВЕРКА ДЛЯ BURST (Замена state.EntityManager.HasComponent):
            if (!_networkIdLookup.HasComponent(clientConnection))
            {
                ecb.DestroyEntity(rpcEntity);
                continue;
            }

            int clientNetworkId = _networkIdLookup[clientConnection].Value;
            uint newGhostId = request.ValueRO.IdNewEntity;
            uint oldGhostId = request.ValueRO.IdOldEntity;

            // КАТЕГОРИЧЕСКИ УДАЛЯЕМ Debug.Log ОТСЮДА! 
            // Строки C# внутри систем Netcode — это главный источник лагов и GC-аллокаций.

            Entity targetVehicleEntity = Entity.Null;
            Entity oldVehicleEntity = Entity.Null;

            // Асинхронный перебор сетевых машин через легкий Query
            foreach (var (ghostInstance, entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithAll<GhostOwner>().WithEntityAccess())
            {
                uint currentGhostId = (uint)ghostInstance.ValueRO.ghostId;

                if (currentGhostId == newGhostId)
                {
                    targetVehicleEntity = entity;
                }
                else if (currentGhostId == oldGhostId)
                {
                    oldVehicleEntity = entity;
                }

                if (targetVehicleEntity != Entity.Null && oldVehicleEntity != Entity.Null)
                {
                    break;
                }
            }

            // Отключение старой машины
            if (oldVehicleEntity != Entity.Null)
            {
                ecb.SetComponent(oldVehicleEntity, new IsControlledTag { IsActive = false });
                ecb.SetComponent(oldVehicleEntity, new GhostOwner { NetworkId = -1 });

                // БЕЗОПАСНАЯ ОЧИСТКА БУФЕРА ДЛЯ BURST (Замена state.EntityManager.HasBuffer):
                if (_inputBufferLookup.HasBuffer(oldVehicleEntity))
                {
                    ecb.SetBuffer<InputBufferData<AAA_InputComponent>>(oldVehicleEntity).Clear();
                }
            }

            // Включение новой машины
            if (targetVehicleEntity != Entity.Null)
            {
                ecb.SetComponent(targetVehicleEntity, new IsControlledTag { IsActive = true });
                ecb.SetComponent(targetVehicleEntity, new GhostOwner { NetworkId = clientNetworkId });
            }

            ecb.DestroyEntity(rpcEntity);
        }
    }
}


//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial struct ServerHandleSwitchRpcSystem : ISystem
//{
//    public void OnUpdate(ref SystemState state)
//    {
//        // 1. Собираем все сетевые госты (машины), существующие в мире СЕРВЕРА
//        var serverGhostsQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwner>().Build();
//        var serverGhostEntities = serverGhostsQuery.ToEntityArray(Allocator.Temp);
//        var serverGhostInstances = serverGhostsQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);

//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        // 1. Перебираем прилетевшие от клиентов RPC-запросы
//        foreach (var (rpcHeader, request, entityRpc) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RequestVehicleSwitchRpc>>()
//                     //.WithAll<RequestVehicleSwitchRpc>()
//                     .WithEntityAccess())
//        {
//            // Извлекаем соединение
//            Entity clientConnection = rpcHeader.ValueRO.SourceConnection;

//            // извлекаем данные
//            uint newGhostId = request.ValueRO.IdNewEntity;
//            uint oldGhostId = request.ValueRO.IdOldEntity;
//            //#if UNITY_EDITOR
//            //            UnityEngine.Debug.Log($"[Server]: Получем RPC для смены машины с {oldGhostId} на {newGhostId}!");
//            //#endif
//            // ИСПРАВЛЕНИЕ: Используем NetworkId вместо устаревшего NetworkIdComponent
//            if (!state.EntityManager.HasComponent<NetworkId>(clientConnection))
//            {
//                ecb.DestroyEntity(entityRpc);
//                continue;
//            }

//            // ИСПРАВЛЕНИЕ: Читаем данные через новый тип NetworkId
//            int clientNetworkId = state.EntityManager.GetComponentData<NetworkId>(clientConnection).Value;

//            //Entity targetVehicleEntity = Entity.Null;
//            //Entity oldVehicleEntity = Entity.Null;

//            //for (int i = 0; i < serverGhostInstances.Length; i++)
//            //{
//            //    //if ((uint)serverGhostInstances[i].ghostId == newGhostId)
//            //    //{
//            //    //    targetVehicleEntity = serverGhostEntities[i];
//            //    //    break;
//            //    //}
//            //    uint currentGhostId = (uint)serverGhostInstances[i].ghostId;

//            //    if (currentGhostId == newGhostId)
//            //    {
//            //        targetVehicleEntity = serverGhostEntities[i];
//            //    }
//            //    else if (currentGhostId == oldGhostId)
//            //    {
//            //        oldVehicleEntity = serverGhostEntities[i];
//            //    }

//            //    // Если нашли обе — можно досрочно выйти из цикла экономии ради
//            //    if (targetVehicleEntity != Entity.Null && oldVehicleEntity != Entity.Null)
//            //    {
//            //        break;
//            //    }
//            //}
//            //// ====================================================================

//            ////if (targetVehicleEntity != Entity.Null && state.EntityManager.Exists(targetVehicleEntity))
//            ////{
//            ////    //// Сверяем права безопасности, чтобы один игрок не угнал чужую воксельную машину
//            ////    //GhostOwner vehicleOwner = state.EntityManager.GetComponentData<GhostOwner>(targetVehicleEntity);

//            ////    //if (vehicleOwner.NetworkId == clientNetworkId)
//            ////    //{
//            ////    // Безопасность пройдена! Очищаем IsControlledTag со ВСЕХ старых машин этого игрока на сервере
//            ////    foreach (var (owner, vehicle) in SystemAPI.Query<GhostOwnerIsLocal>().WithAll<IsControlledTag>().WithEntityAccess())
//            ////    {
//            ////        //if (owner.NetworkId == clientNetworkId)
//            ////        //{
//            ////        ecb.SetComponent(vehicle, new IsControlledTag { IsActive = false });
//            ////        ecb.SetComponent(vehicle, new GhostOwner { NetworkId = -1 });
//            ////        //}
//            ////    }

//            ////    // Вешаем тег активного управления на новую одобренную сервером машину
//            ////    ecb.SetComponent<IsControlledTag>(targetVehicleEntity, new IsControlledTag { IsActive = true });
//            ////    ecb.SetComponent(targetVehicleEntity, new GhostOwner { NetworkId = clientNetworkId });
//            ////    //}
//            ////}
//            //// 3. ТОЧЕЧНОЕ ОТКЛЮЧЕНИЕ СТАРOЙ МАШИНЫ (Без вложенных Query!)
//            //if (oldVehicleEntity != Entity.Null && state.EntityManager.Exists(oldVehicleEntity))
//            //{
//            //    // Отключаем флаг активности и забираем права владельца
//            //    ecb.SetComponent(oldVehicleEntity, new IsControlledTag { IsActive = false });
//            //    ecb.SetComponent(oldVehicleEntity, new GhostOwner { NetworkId = -1 });

//            //    // КРИТИЧЕСКИЙ ФИКС ФРИЗА: Полностью очищаем буфер сетевого ввода пустой машины!
//            //    if (state.EntityManager.HasBuffer<InputBufferData<AAA_InputComponent>>(oldVehicleEntity))
//            //    {
//            //        var inputBuffer = state.EntityManager.GetBuffer<InputBufferData<AAA_InputComponent>>(oldVehicleEntity);
//            //        inputBuffer.Clear(); // Стираем старые команды, чтобы они не раздували сеть на -380%
//            //        inputBuffer = default;
//            //    }
//            //}

//            //// 4. ТОЧЕЧНОЕ ВКЛЮЧЕНИЕ НОВОЙ МАШИНЫ
//            //if (targetVehicleEntity != Entity.Null && state.EntityManager.Exists(targetVehicleEntity))
//            //{
//            //    // Передаем права владения и активируем флаг управления на сервере
//            //    ecb.SetComponent(targetVehicleEntity, new IsControlledTag { IsActive = true });
//            //    ecb.SetComponent(targetVehicleEntity, new GhostOwner { NetworkId = clientNetworkId });
//            //}

//            Entity targetVehicleEntity = Entity.Null;
//            Entity oldVehicleEntity = Entity.Null;

//            // ====================================================================
//            // АСИНХРОННЫЙ ПОИСК МАШИН БЕЗ ФРИЗОВ (Замена ToEntityArray):
//            // Мы перебираем сетевые сущности через легкий Query. Он полностью 
//            // Burst-совместим, не блокирует Jobs-потоки и не вызывает WaitForJobGroupID!
//            // ====================================================================
//            foreach (var (ghostInstance, entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithAll<GhostOwner>().WithEntityAccess())
//            {
//                uint currentGhostId = (uint)ghostInstance.ValueRO.ghostId;

//                if (currentGhostId == newGhostId)
//                {
//                    targetVehicleEntity = entity;
//                }
//                else if (currentGhostId == oldGhostId)
//                {
//                    oldVehicleEntity = entity;
//                }

//                // Маленькая оптимизация: если обе машины найдены в цикле, выходим досрочно
//                if (targetVehicleEntity != Entity.Null && oldVehicleEntity != Entity.Null)
//                {
//                    break;
//                }
//            }
//            // ====================================================================

//            // 3. ТОЧЕЧНОЕ ОТКЛЮЧЕНИЕ СТАРOЙ МАШИНЫ
//            if (oldVehicleEntity != Entity.Null && state.EntityManager.Exists(oldVehicleEntity))
//            {
//                // Сбрасываем флаг активности управления
//                ecb.SetComponent(oldVehicleEntity, new IsControlledTag { IsActive = false });

//                // Забираем права сетевого владения объектом
//                ecb.SetComponent(oldVehicleEntity, new GhostOwner { NetworkId = -1 });

//                // КРИТИЧЕСКИЙ ФИКС СЕТЕВОГО ТРАФИКА: 
//                // Принудительно очищаем буфер ввода брошенной машины, чтобы сервер не слал 
//                // по сети пустой "осиротевший" InputBuffer со сжатием -380% другим клиентам.
//                if (state.EntityManager.HasBuffer<InputBufferData<AAA_InputComponent>>(oldVehicleEntity))
//                {
//                    // Метод SetBuffer инициализирует буфер в ECB и возвращает его для записи команд.
//                    // Вызов .Clear() запишется в очередь и выполнится асинхронно между кадрами!
//                    var deferredBuffer = ecb.SetBuffer<InputBufferData<AAA_InputComponent>>(oldVehicleEntity);
//                    deferredBuffer.Clear();
//                }
//            }

//            // 4. ТОЧЕЧНОЕ ВКЛЮЧЕНИЕ НОВОЙ МАШИНЫ
//            if (targetVehicleEntity != Entity.Null && state.EntityManager.Exists(targetVehicleEntity))
//            {
//                // Активируем флаг управления (теперь джоба движения начнется для этого транспорта)
//                ecb.SetComponent(targetVehicleEntity, new IsControlledTag { IsActive = true });

//                // Передаем права владения («Ghost Ownership») клиенту, приславшему RPC
//                ecb.SetComponent(targetVehicleEntity, new GhostOwner { NetworkId = clientNetworkId });
//            }
//        }

//        serverGhostEntities.Dispose();
//        serverGhostInstances.Dispose();

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }

//}




////Entity currentVehicle = Entity.Null;
////Entity nextVehicle = Entity.Null;

////// 2. Ищем, какой машиной этот клиент владеет сейчас на сервере
////foreach (var (ghostOwner, vehicleEntity) in SystemAPI.Query<RefRO<GhostOwner>>()
////             .WithEntityAccess())
////{
////    // Проверяем, является ли эта сущность машиной (есть ли нужный компонент движения)
////    if (SystemAPI.HasComponent<AAA_InputComponent>(vehicleEntity))
////    {
////        if (ghostOwner.ValueRO.NetworkId == clientNetworkId)
////        {
////            currentVehicle = vehicleEntity;
////            break;
////        }
////    }
////}

////if (currentVehicle == Entity.Null)
////{
////    UnityEngine.Debug.LogWarning($"[Server]: currentVehicle = null (clientNetworkId={clientNetworkId})");
////}
////else
////{
////    ecb.AddComponent<IsControlledTag>(currentVehicle);
////}

////// 3. Ищем СЛЕДУЮЩУЮ свободную или чужую машину на сервере для переключения
////foreach (var (ghostOwner, vehicleEntity) in SystemAPI.Query<RefRO<GhostOwner>>()
////             .WithEntityAccess())
////{
////    if (SystemAPI.HasComponent<AAA_InputComponent>(vehicleEntity) && vehicleEntity != currentVehicle)
////    {
////        // Если машина ничья (NetworkId == 0) или принадлежит не нам — выбираем её
////        if (ghostOwner.ValueRO.NetworkId == 0 || ghostOwner.ValueRO.NetworkId != clientNetworkId)
////        {
////            nextVehicle = vehicleEntity;
////            break;
////        }
////    }
////}
////if (nextVehicle == Entity.Null)
////{
////    UnityEngine.Debug.LogWarning("[Server]: nextVehicle = null");
////}

////// 4. АВТОРИТАРНОЕ СЕРВЕРНОЕ ПЕРЕКЛЮЧЕНИЕ
////if (nextVehicle != Entity.Null)
////{
////    // Снимаем владение со старой машины
////    if (currentVehicle != Entity.Null)
////    {
////        ecb.SetComponent(currentVehicle, new GhostOwner { NetworkId = 0 });
////    }

////    // Передаем новую машину клиенту
////    ecb.SetComponent(nextVehicle, new GhostOwner { NetworkId = clientNetworkId });

////    UnityEngine.Debug.Log($"[Сервер] Клиент {clientNetworkId} успешно пересажен в машину {nextVehicle.Index}");
////}