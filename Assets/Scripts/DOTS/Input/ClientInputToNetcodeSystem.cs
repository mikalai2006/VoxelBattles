using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WithAll(typeof(GhostOwnerIsLocal))] // Собираем ввод ТОЛЬКО для своей машины!
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct ClientInputToNetcodeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InputStateSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var inputSingleton = SystemAPI.GetSingleton<InputStateSingleton>();

        // 1. ОТПРАВКА RPC (Запрос серверу на смену машины)
        if (inputSingleton.SwitchTargetTriggered)
        {
            uint oldGhostId = 0;

            // Ищем текущую активную машину
            foreach (var (ghostInstance, controlTag, oldEntity) in SystemAPI.Query<RefRO<GhostInstance>, RefRO<IsControlledTag>>()
                         .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
            {
                if (controlTag.ValueRO.IsActive)
                {
                    oldGhostId = (uint)ghostInstance.ValueRO.ghostId;
                    break;
                }
            }

            // Собираем все доступные машины игрока
            var availableVehiclesQuery = SystemAPI.QueryBuilder()
                .WithAll<GhostInstance, IsControlledTag, AAA_MovementComponent>()
                .Build();
            var allMyInstances = availableVehiclesQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
            var allEntities = availableVehiclesQuery.ToEntityArray(Allocator.Temp);

            if (allMyInstances.Length < 1)
            {
                allMyInstances.Dispose();
                return;
            }

            uint newGhostId = 0;
            int currentIdx = -1;

            for (int i = 0; i < allMyInstances.Length; i++)
            {
                if ((uint)allMyInstances[i].ghostId == oldGhostId && SystemAPI.IsComponentEnabled<IsControlledTag>(allEntities[i]))
                {
                    currentIdx = i;
                    break;
                }
            }

            int nextIdx = (currentIdx + 1) % allMyInstances.Length;
            newGhostId = (uint)allMyInstances[nextIdx].ghostId;
            allMyInstances.Dispose();
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[Client]: Хочу поменять управление с {oldGhostId} на {newGhostId}!");
#endif


            if (newGhostId > 0)
            {
                Entity rpcRequestEntity = state.EntityManager.CreateEntity(
                    typeof(SendRpcCommandRequest),
                    typeof(RequestVehicleSwitchRpc)
                );

                state.EntityManager.SetComponentData(rpcRequestEntity, new RequestVehicleSwitchRpc
                {
                    IdOldEntity = oldGhostId,
                    IdNewEntity = newGhostId
                });

                state.EntityManager.SetComponentData(rpcRequestEntity, new SendRpcCommandRequest
                {
                    TargetConnection = Entity.Null
                });
            }
        }


        //// ==========================================
        //// 2. ИСПРАВЛЕНИЕ ДЛЯ AAA: СТРОГАЯ РЕТРАНСЛЯЦИЯ WASD
        //// ==========================================
        //// Запрашиваем RefRO<IsControlledTag>, чтобы проверить, какая именно машина активна!
        //foreach (var (inputComponent, controlTag, entity) in SystemAPI.Query<RefRW<AAA_InputComponent>, RefRO<IsControlledTag>>()
        //             .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
        //{
        //    // Если эта машина сейчас выбрана игроком (активна):
        //    if (controlTag.ValueRO.IsActive)
        //    {
        //        // Записываем реальный WASD в активную машину
        //        inputComponent.ValueRW.MoveInput = inputSingleton.MoveInput;
        //    }
        //    else
        //    {
        //        // КРИТИЧЕСКИЙ ФИКС: Если машина неактивна, мы принудительно обнуляем её ввод!
        //        // Когда значения осей равны чистым нулям, алгоритм дельта-сжатия Netcode 
        //        // понимает, что данные не меняются, и полностью прекращает слать InputBuffer для этой машины.
        //        inputComponent.ValueRW.MoveInput = float2.zero;
        //    }
        //}
        // ==========================================
        // 2. AAA-РЕШЕНИЕ: Пишем ТОЛЬКО при реальном изменении ввода активной машины
        // ==========================================
        foreach (var (inputComponent, controlTag) in SystemAPI.Query<RefRW<AAA_InputComponent>, RefRO<IsControlledTag>>().WithAll<GhostOwnerIsLocal>())
        {
            // Условие 1: Проверяем, активна ли машина
            if (controlTag.ValueRO.IsActive)
            {
                // 1. Упаковываем текущий сырой ввод игрока из синглтона в байтовую маску
                byte currentNewMask = VoxelInputPackingUtility.PackFloat2ToBits(inputSingleton.MoveInput);
                // 2. Сравниваем напрямую байты за 1 такт процессора. 
                // Пишем ТОЛЬКО если биты реально изменились (нажали/отпустили кнопку)
                if (inputComponent.ValueRO.ButtonsMask != currentNewMask)
                {
                    inputComponent.ValueRW.ButtonsMask = currentNewMask;
                }

                //// Условие 2: Проверяем, отличается ли текущий WASD от того, что уже лежит в компоненте.
                //// math.any(a != b) вернет true, только если игрок нажал/отпустил кнопку или изменил направление.
                //if (math.any(inputComponent.ValueRO.MoveInput != inputSingleton.MoveInput))
                //{
                //    // Открываем на запись ТОЛЬКО в момент реального изменения
                //    inputComponent.ValueRW.MoveInput = inputSingleton.MoveInput;
                //}
            }
            // Для неактивных машин блок else вообще ОТСУТСТВУЕТ. 
            // Мы к ним даже не прикасаемся, благодаря чему их Change Version замораживается, 
            // а NetCode полностью исключает их буферы ввода из сетевого потока.
        }
    }
}

//using Unity.Collections;
//using Unity.Entities;
//using Unity.NetCode;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(GhostInputSystemGroup))]
//public partial struct ClientInputToNetcodeSystem : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<InputStateSingleton>();
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        var inputSingleton = SystemAPI.GetSingleton<InputStateSingleton>();
//        //// Получаем синглтон очередей переключения
//        //if (!SystemAPI.TryGetSingletonRW<GhostPredictionSwitchingQueues>(out var switchingQueues))
//        //{
//        //    UnityEngine.Debug.LogWarning("[Server] GhostPredictionSwitchingQueues not found!");
//        //    return;
//        //}
//        // 1. ОТПРАВКА RPC (Запрос серверу на смену машины)
//        if (inputSingleton.SwitchTargetTriggered)
//        {
//            // 1. КАК НАЙТИ ТЕКУЩИЙ GHOST_ID АКТИВНОЙ МАШИНЫ:
//            // Так как IsControlledTag теперь хранит поле IsActive, мы обязаны считывать его!
//            uint oldGhostId = 0;

//            // Ищем машину, у которой IsControlledTag.IsActive равен true
//            foreach (var (ghostInstance, controlTag, oldEntity) in SystemAPI.Query<RefRO<GhostInstance>, RefRO<IsControlledTag>>()
//                         .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
//            {
//                if (controlTag.ValueRO.IsActive)
//                {
//                    oldGhostId = (uint)ghostInstance.ValueRO.ghostId;
//                    //// Если тик валиден, значит предсказание сейчас активно, и его нужно выключить.
//                    //if (SystemAPI.HasComponent<PredictedGhost>(oldEntity))
//                    //{
//                    //    switchingQueues.ValueRW.ConvertToInterpolatedQueue.Enqueue(new ConvertPredictionEntry
//                    //    {
//                    //        TargetEntity = oldEntity,
//                    //        TransitionDurationSeconds = 0.0f // Мгновенный переход
//                    //    });
//                    //}

//                    break; // Нашли текущую активную машину, выходим из поиска
//                }
//            }

//            // 2. СОБИРАЕМ ВСЕ ДОСТУПНЫЕ МАШИНЫ ИГРОКА ДЛЯ ПЕРЕБОРА НА КЛИЕНТЕ:
//            var availableVehiclesQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwnerIsLocal, AAA_MovementComponent>().Build();
//            var allMyInstances = availableVehiclesQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
//            var allMyEntity = availableVehiclesQuery.ToEntityArray(Allocator.Temp);

//            if (allMyInstances.Length < 1)
//            {
//#if UNITY_EDITOR
//                UnityEngine.Debug.Log($"Пересаживаться некуда, машина всего {allMyInstances.Length}!");
//#endif
//                allMyInstances.Dispose();
//                return; // Пересаживаться некуда, машина всего одна или ноль
//            }
//            //#if UNITY_EDITOR
//            //            UnityEngine.Debug.Log($"Сейчас у вас {allMyInstances.Length} сущностей для перебора!");
//            //#endif
//            uint newGhostId = 0;
//            int currentIdx = -1;

//            // Ищем порядковый индекс текущей машины в массиве, чтобы взять следующую за ней
//            for (int i = 0; i < allMyInstances.Length; i++)
//            {
//                if ((uint)allMyInstances[i].ghostId == oldGhostId)
//                {
//                    currentIdx = i;
//                    break;
//                }
//            }

//            // Вычисляем по кругу сетевой ID следующей машины
//            int nextIdx = (currentIdx + 1) % allMyInstances.Length;
//            newGhostId = (uint)allMyInstances[nextIdx].ghostId;
//            allMyInstances.Dispose();

//            // 3. ПРАВИЛЬНАЯ И КАНOНИЧНАЯ ОТПРАВКА RPC-ПАКЕТА НА СЕРВЕР:
//            if (newGhostId > 0)
//            {
//                ////Проверяем внутренний флаг Netcode. Если объект еще не в режиме предсказания:
//                //if (!SystemAPI.HasComponent<PredictedGhost>(allMyEntity[nextIdx]))
//                //{
//                //    switchingQueues.ValueRW.ConvertToPredictedQueue.Enqueue(new ConvertPredictionEntry
//                //    {
//                //        TargetEntity = allMyEntity[nextIdx],
//                //        TransitionDurationSeconds = 0.0f // Мгновенный переход
//                //    });
//                //}

//                // Создаем сущность СРАЗУ с архетипом отправки Netcode, передавая оба компонента в один миг!
//                // Это гарантирует, что сетевой планировщик сразу увидит исходящий RPC-пакет.
//                Entity rpcRequestEntity = state.EntityManager.CreateEntity(
//                    typeof(SendRpcCommandRequest),
//                    typeof(RequestVehicleSwitchRpc)
//                );

//                // Инициализируем данные сетевого запроса чистыми uint идентификаторами
//                state.EntityManager.SetComponentData(rpcRequestEntity, new RequestVehicleSwitchRpc
//                {
//                    IdOldEntity = oldGhostId,
//                    IdNewEntity = newGhostId
//                });

//                // Настройка TargetConnection для отправки на сервер (для надежности в некоторых версиях 1.4)
//                state.EntityManager.SetComponentData(rpcRequestEntity, new SendRpcCommandRequest
//                {
//                    TargetConnection = Entity.Null // Указывает Netcode, что адресатом является именно Сервер
//                });
//                //#if UNITY_EDITOR
//                //                UnityEngine.Debug.Log($"[Voxel Client]: Запрос на пересадку отправлен! Из GhostId {oldGhostId} в GhostId {newGhostId}");
//                //#endif
//            }
//            //// 1. КАК НАЙТИ ТОГО, КТО СЕЙЧАС УПРАВЛЯЕТСЯ:
//            //// Используем встроенный синглтон-запрос. Если управляемой машины нет, query вернет Null.
//            //uint oldGhostId = 0;

//            //// Находим первую машину, она должна быть единственной для управления.
//            //var currentQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance>()
//            //    .WithAll<GhostOwnerIsLocal, IsControlledTag>().Build();
//            //if (!currentQuery.IsEmpty)
//            //{
//            //    var activeEntities = currentQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
//            //    oldGhostId = (uint)activeEntities[0].ghostId;
//            //    activeEntities.Dispose();
//            //}

//            //// Собираем вообще все машины этого игрока, которые есть в мире клиента
//            //var availableVehiclesQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwnerIsLocal, AAA_MovementComponent>().Build();
//            //var allMyInstances = availableVehiclesQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
//            //if (allMyInstances.Length < 2)
//            //{
//            //    allMyInstances.Dispose();
//            //    UnityEngine.Debug.LogWarning($"Пересаживаться некуда, машина всего {allMyInstances.Length}!");
//            //    return; // Пересаживаться некуда, машина всего одна
//            //}

//            //UnityEngine.Debug.LogWarning($"Сейчас у вас {allMyInstances.Length} сущностей!");

//            //// Ищем индекс текущей машины в массиве, чтобы взять следующую за ней
//            //uint newGhostId = 0;
//            //int currentIdx = -1;
//            //for (int i = 0; i < allMyInstances.Length; i++)
//            //{
//            //    if ((uint)allMyInstances[i].ghostId == oldGhostId)
//            //    {
//            //        currentIdx = i;
//            //        break;
//            //    }
//            //}

//            //// Берем по кругу следующий сетевой ID машины
//            //int nextIdx = (currentIdx + 1) % allMyInstances.Length;
//            //newGhostId = (uint)allMyInstances[nextIdx].ghostId;
//            //allMyInstances.Dispose();

//            //// 3. ОТПРАВЛЯЕМ ЧИСТЫЕ UINT ДАННЫЕ НА СЕРВЕР
//            //if (newGhostId > 0)
//            //{
//            //    Entity rpcRequestEntity = state.EntityManager.CreateEntity(typeof(SendRpcCommandRequest));

//            //    // Передаем строго безопасные числовые идентификаторы!
//            //    state.EntityManager.AddComponentData(rpcRequestEntity, new RequestVehicleSwitchRpc
//            //    {
//            //        IdOldEntity = oldGhostId,
//            //        IdNewEntity = newGhostId
//            //    });
//            //}

//            ////var rpcEntity = state.EntityManager.CreateEntity(typeof(SendRpcCommandRequest));
//            ////state.EntityManager.SetComponentData(rpcEntity, new SendRpcCommandRequest
//            ////{
//            ////    TargetConnection = Entity.Null // Отправляем на сервер
//            ////});

//            ////state.EntityManager.AddComponentData(rpcEntity, new RequestVehicleSwitchRpc()
//            ////{
//            ////    IdNewEntity = 0,
//            ////    IdOldEntity = 1,
//            ////});
//        }

//        // 2. РЕТРАНСЛЯЦИЯ WASD (Только в СВОЮ локальную машину)
//        // Перебираем все сущности с вводом, доступные на клиенте
//        foreach (var (inputComponent, entity) in SystemAPI.Query<RefRW<AAA_InputComponent>>()
//                     .WithAll<GhostOwnerIsLocal, IsControlledTag>().WithEntityAccess())
//        {
//            // Безопасно проверяем в коде, является ли этот Ghost локальным для нашего клиента
//            if (SystemAPI.HasComponent<GhostOwnerIsLocal>(entity))
//            {
//                // Записываем ввод только в нашу машину
//                inputComponent.ValueRW.MoveInput = inputSingleton.MoveInput;
//            }
//        }

//        //// ====================================================================
//        //// ГЛАВНЫЙ ФИЛЬТР КЛИЕНТА:
//        //// Мы ищем машину, у которой ОДНОВРЕМЕННО есть и сетевое право владения,
//        //// и наш локальный тег текущего активного управления!
//        //// ====================================================================
//        //foreach (var inputBuffer in SystemAPI.Query<DynamicBuffer<InputBufferData<AAA_InputComponent>>>()
//        //             .WithAll<GhostOwnerIsLocal, IsControlledTag>()) // <-- ЖЕСТКИЙ ФИЛЬТР
//        //{
//        //    // Получаем доступ к тику, в который нужно записать команду
//        //    var networkTime = SystemAPI.GetSingleton<NetworkTime>();

//        //    var newInput = new AAA_InputComponent { MoveInput = inputSingleton.MoveInput };

//        //    // Записываем ввод с клавиатуры в буфер именно ЭТОЙ машины
//        //    inputBuffer.AddCommandData(networkTime.ServerTick, newInput);
//        //}

//    }
//}



////using Unity.Collections;
////using Unity.Entities;
////using Unity.NetCode;

////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
////[UpdateInGroup(typeof(GhostInputSystemGroup))]
////public partial struct ClientInputToNetcodeSystem : ISystem
////{
////    public void OnCreate(ref SystemState state)
////    {
////        state.RequireForUpdate<InputStateSingleton>();
////    }

////    public void OnUpdate(ref SystemState state)
////    {
////        var inputSingleton = SystemAPI.GetSingleton<InputStateSingleton>();

////        // Получаем синглтон очередей переключения
////        if (!SystemAPI.TryGetSingletonRW<GhostPredictionSwitchingQueues>(out var switchingQueues))
////        {
////            UnityEngine.Debug.LogWarning("[Server] GhostPredictionSwitchingQueues not found!");
////            return;
////        }

////        // 1. ОТПРАВКА RPC (Запрос серверу на смену машины)
////        if (inputSingleton.SwitchTargetTriggered)
////        {
////            // 1. КАК НАЙТИ ТЕКУЩИЙ GHOST_ID АКТИВНОЙ МАШИНЫ:
////            // Так как IsControlledTag теперь хранит поле IsActive, мы обязаны считывать его!
////            uint oldGhostId = 0;

////            // Ищем машину, у которой IsControlledTag.IsActive равен true
////            foreach (var (ghostInstance, controlTag, oldEntity) in SystemAPI.Query<RefRO<GhostInstance>, RefRO<IsControlledTag>>()
////                         .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
////            {
////                if (controlTag.ValueRO.IsActive)
////                {
////                    oldGhostId = (uint)ghostInstance.ValueRO.ghostId;

////                    //// ИСПРАВЛЕНИЕ: Если тик валиден, значит предсказание сейчас активно, и его нужно выключить.
////                    //if (SystemAPI.HasComponent<PredictedGhost>(oldEntity))
////                    //{
////                    //    switchingQueues.ValueRW.ConvertToInterpolatedQueue.Enqueue(new ConvertPredictionEntry
////                    //    {
////                    //        TargetEntity = oldEntity,
////                    //        TransitionDurationSeconds = 0.0f // Мгновенный переход
////                    //    });


////                    //}
////                    break; // Нашли текущую активную машину, выходим из поиска
////                }
////            }

////            // 2. СОБИРАЕМ ВСЕ ДОСТУПНЫЕ МАШИНЫ ИГРОКА ДЛЯ ПЕРЕБОРА НА КЛИЕНТЕ:
////            var availableVehiclesQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwnerIsLocal, AAA_MovementComponent>().Build();
////            var allMyInstances = availableVehiclesQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
////            var allMyEntity = availableVehiclesQuery.ToEntityArray(Allocator.Temp);

////            if (allMyInstances.Length < 1)
////            {
////#if UNITY_EDITOR
////                UnityEngine.Debug.Log($"Пересаживаться некуда, машина всего {allMyInstances.Length}!");
////#endif
////                allMyInstances.Dispose();
////                return; // Пересаживаться некуда, машина всего одна или ноль
////            }
////            //#if UNITY_EDITOR
////            //            UnityEngine.Debug.Log($"Сейчас у вас {allMyInstances.Length} сущностей для перебора!");
////            //#endif
////            uint newGhostId = 0;
////            int currentIdx = -1;

////            // Ищем порядковый индекс текущей машины в массиве, чтобы взять следующую за ней
////            for (int i = 0; i < allMyInstances.Length; i++)
////            {
////                if ((uint)allMyInstances[i].ghostId == oldGhostId)
////                {
////                    currentIdx = i;
////                    break;
////                }
////            }

////            // Вычисляем по кругу сетевой ID следующей машины
////            int nextIdx = (currentIdx + 1) % allMyInstances.Length;
////            GhostInstance ghostInstanceNew = currentIdx > -1 ? allMyInstances[currentIdx] : default;
////            newGhostId = (uint)ghostInstanceNew.ghostId;
////            allMyInstances.Dispose();

////            // 3. ПРАВИЛЬНАЯ И КАНOНИЧНАЯ ОТПРАВКА RPC-ПАКЕТА НА СЕРВЕР:
////            if (newGhostId > 0)
////            {
////                // Создаем сущность СРАЗУ с архетипом отправки Netcode, передавая оба компонента в один миг!
////                // Это гарантирует, что сетевой планировщик сразу увидит исходящий RPC-пакет.
////                Entity rpcRequestEntity = state.EntityManager.CreateEntity(
////                    typeof(SendRpcCommandRequest),
////                    typeof(RequestVehicleSwitchRpc)
////                );

////                // Инициализируем данные сетевого запроса чистыми uint идентификаторами
////                state.EntityManager.SetComponentData(rpcRequestEntity, new RequestVehicleSwitchRpc
////                {
////                    IdOldEntity = oldGhostId,
////                    IdNewEntity = newGhostId
////                });

////                // Настройка TargetConnection для отправки на сервер (для надежности в некоторых версиях 1.4)
////                state.EntityManager.SetComponentData(rpcRequestEntity, new SendRpcCommandRequest
////                {
////                    TargetConnection = Entity.Null // Указывает Netcode, что адресатом является именно Сервер
////                });

////                //// Проверяем внутренний флаг Netcode. Если объект еще не в режиме предсказания:
////                //if (!SystemAPI.HasComponent<PredictedGhost>(allMyEntity[nextIdx]))
////                //{
////                //    switchingQueues.ValueRW.ConvertToPredictedQueue.Enqueue(new ConvertPredictionEntry
////                //    {
////                //        TargetEntity = allMyEntity[nextIdx],
////                //        TransitionDurationSeconds = 0.0f // Мгновенный переход
////                //    });
////                //}
////                //#if UNITY_EDITOR
////                //                UnityEngine.Debug.Log($"[Voxel Client]: Запрос на пересадку отправлен! Из GhostId {oldGhostId} в GhostId {newGhostId}");
////                //#endif
////            }
////            //// 1. КАК НАЙТИ ТОГО, КТО СЕЙЧАС УПРАВЛЯЕТСЯ:
////            //// Используем встроенный синглтон-запрос. Если управляемой машины нет, query вернет Null.
////            //uint oldGhostId = 0;

////            //// Находим первую машину, она должна быть единственной для управления.
////            //var currentQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance>()
////            //    .WithAll<GhostOwnerIsLocal, IsControlledTag>().Build();
////            //if (!currentQuery.IsEmpty)
////            //{
////            //    var activeEntities = currentQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
////            //    oldGhostId = (uint)activeEntities[0].ghostId;
////            //    activeEntities.Dispose();
////            //}

////            //// Собираем вообще все машины этого игрока, которые есть в мире клиента
////            //var availableVehiclesQuery = SystemAPI.QueryBuilder().WithAll<GhostInstance, GhostOwnerIsLocal, AAA_MovementComponent>().Build();
////            //var allMyInstances = availableVehiclesQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
////            //if (allMyInstances.Length < 2)
////            //{
////            //    allMyInstances.Dispose();
////            //    UnityEngine.Debug.LogWarning($"Пересаживаться некуда, машина всего {allMyInstances.Length}!");
////            //    return; // Пересаживаться некуда, машина всего одна
////            //}

////            //UnityEngine.Debug.LogWarning($"Сейчас у вас {allMyInstances.Length} сущностей!");

////            //// Ищем индекс текущей машины в массиве, чтобы взять следующую за ней
////            //uint newGhostId = 0;
////            //int currentIdx = -1;
////            //for (int i = 0; i < allMyInstances.Length; i++)
////            //{
////            //    if ((uint)allMyInstances[i].ghostId == oldGhostId)
////            //    {
////            //        currentIdx = i;
////            //        break;
////            //    }
////            //}

////            //// Берем по кругу следующий сетевой ID машины
////            //int nextIdx = (currentIdx + 1) % allMyInstances.Length;
////            //newGhostId = (uint)allMyInstances[nextIdx].ghostId;
////            //allMyInstances.Dispose();

////            //// 3. ОТПРАВЛЯЕМ ЧИСТЫЕ UINT ДАННЫЕ НА СЕРВЕР
////            //if (newGhostId > 0)
////            //{
////            //    Entity rpcRequestEntity = state.EntityManager.CreateEntity(typeof(SendRpcCommandRequest));

////            //    // Передаем строго безопасные числовые идентификаторы!
////            //    state.EntityManager.AddComponentData(rpcRequestEntity, new RequestVehicleSwitchRpc
////            //    {
////            //        IdOldEntity = oldGhostId,
////            //        IdNewEntity = newGhostId
////            //    });
////            //}

////            ////var rpcEntity = state.EntityManager.CreateEntity(typeof(SendRpcCommandRequest));
////            ////state.EntityManager.SetComponentData(rpcEntity, new SendRpcCommandRequest
////            ////{
////            ////    TargetConnection = Entity.Null // Отправляем на сервер
////            ////});

////            ////state.EntityManager.AddComponentData(rpcEntity, new RequestVehicleSwitchRpc()
////            ////{
////            ////    IdNewEntity = 0,
////            ////    IdOldEntity = 1,
////            ////});
////        }

////        // 2. РЕТРАНСЛЯЦИЯ WASD (Только в СВОЮ локальную машину)
////        // Перебираем все сущности с вводом, доступные на клиенте
////        foreach (var (inputComponent, entity) in SystemAPI.Query<RefRW<AAA_InputComponent>>()
////                     .WithAll<GhostOwnerIsLocal, IsControlledTag>().WithEntityAccess())
////        {
////            // Безопасно проверяем в коде, является ли этот Ghost локальным для нашего клиента
////            if (SystemAPI.HasComponent<GhostOwnerIsLocal>(entity))
////            {
////                // Записываем ввод только в нашу машину
////                inputComponent.ValueRW.MoveInput = inputSingleton.MoveInput;
////            }
////        }

////        //// ====================================================================
////        //// ГЛАВНЫЙ ФИЛЬТР КЛИЕНТА:
////        //// Мы ищем машину, у которой ОДНОВРЕМЕННО есть и сетевое право владения,
////        //// и наш локальный тег текущего активного управления!
////        //// ====================================================================
////        //foreach (var inputBuffer in SystemAPI.Query<DynamicBuffer<InputBufferData<AAA_InputComponent>>>()
////        //             .WithAll<GhostOwnerIsLocal, IsControlledTag>()) // <-- ЖЕСТКИЙ ФИЛЬТР
////        //{
////        //    // Получаем доступ к тику, в который нужно записать команду
////        //    var networkTime = SystemAPI.GetSingleton<NetworkTime>();

////        //    var newInput = new AAA_InputComponent { MoveInput = inputSingleton.MoveInput };

////        //    // Записываем ввод с клавиатуры в буфер именно ЭТОЙ машины
////        //    inputBuffer.AddCommandData(networkTime.ServerTick, newInput);
////        //}

////    }
////}
