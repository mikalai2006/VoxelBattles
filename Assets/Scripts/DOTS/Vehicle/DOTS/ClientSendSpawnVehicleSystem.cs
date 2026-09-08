using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Только для клиента
public partial struct ClientSendSpawnVehicleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Проверяем, существует ли в данный момент сетевое соединение с сервером.
        // Это полностью защищает систему от падений (InvalidOperationException).
        if (!SystemAPI.HasSingleton<NetworkId>()) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Легально и безопасно получаем сущность сетевого соединения
        Entity connectionEntity = SystemAPI.GetSingletonEntity<NetworkId>();

        // Перебираем все намерения спавна, созданные из UI-скрипта
        foreach (var (intent, entity) in SystemAPI.Query<RefRO<SpawnVehicleIntent>>().WithEntityAccess())
        {
            // 1. Создаем сущность сетевого RPC запроса
            Entity rpcEntity = ecb.CreateEntity();

            ecb.AddComponent(rpcEntity, new RequestSpawnVehicleRpc
            {
                //ConfigHashNameMuzzle = intent.ValueRO.ConfigHashNameMuzzle,
                //ConfigHashNameBody = intent.ValueRO.ConfigHashNameBody,
                //ConfigHashNameTower = intent.ValueRO.ConfigHashNameTower,
                //ConfigHashNameWheels = intent.ValueRO.ConfigHashNameWheels,
                towerData = intent.ValueRO.towerData,
                bodyData = intent.ValueRO.bodyData,
                wheelsData = intent.ValueRO.wheelsData,
                SpawnPosition = intent.ValueRO.SpawnPosition,
                SpawnRotation = intent.ValueRO.SpawnRotation,
                //IsAddMove = intent.ValueRO.IsAddMove,
                //IsDynamic = intent.ValueRO.IsDynamic
            });

            // 2. Привязываем RPC к серверному соединению
            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
            //#if UNITY_EDITOR
            //            UnityEngine.Debug.Log("[CLIENT] Системно отправлен RPC запрос на спавн модели !");
            //#endif
            // 3. Уничтожаем сущность намерения, чтобы не дублировать отправку
            ecb.DestroyEntity(entity);
        }
    }
}
