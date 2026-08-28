//using Unity.Burst;
//using Unity.Entities;
//using Unity.NetCode;

//[BurstCompile]
//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // Работает СТРОГО на сервере
//public partial struct ServerReceiveSpawnVehicleSystem : ISystem
//{
//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
//            .CreateCommandBuffer(state.WorldUnmanaged);

//        // Ищем все входящие RPC-запросы на спавн танков
//        foreach (var (rpc, request, entity) in SystemAPI.Query<RefRO<RequestVehicleSpawnRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
//        {
//            // 1. Создаем на СЕРВЕРЕ сущность-запрос для вашей текущей системы сборки
//            Entity requestAssemblyEntity = ecb.CreateEntity();

//            // 2. Переносим данные из сети в вашу систему сборки
//            // Примечание: Сервер должен уметь находить ScriptableObject по rpc.ValueRO.PresetId.
//            // Для этого обычно создают общую коллекцию пресетов (например, синглтон-компонент со словарем/массивом).
//            ecb.AddComponent(requestAssemblyEntity, new RequestVehicleAssembly
//            {
//                // На сервере вы подставляете реальный пресет на основе полученного ID:
//                // Preset = MyPresetCollection.GetByUniqueId(rpc.ValueRO.PresetId),
//                SpawnPosition = rpc.ValueRO.SpawnPosition,
//                SpawnRotation = Unity.Mathematics.quaternion.identity,
//                isAddMove = rpc.ValueRO.IsAddMove,
//                IsDynamic = rpc.ValueRO.IsDynamic
//            });

//            UnityEngine.Debug.Log($"[SERVER] Принят запрос от клиента {request.ValueRO.SourceConnection}. Спавним танк пресета {rpc.ValueRO.PresetId}.");

//            // 3. Уничтожаем отработанную сущность сетевого RPC пакета
//            ecb.DestroyEntity(entity);
//        }
//    }
//}
