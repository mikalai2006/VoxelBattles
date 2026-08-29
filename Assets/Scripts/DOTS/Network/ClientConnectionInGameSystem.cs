using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

public struct GoInGameRequestRpc : IRpcCommand
{
    public byte ValueForBurstRegistry;
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientConnectionInGameSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        ////// Система спит, пока в мире не появится хоть одно сетевое соединение
        //state.RequireForUpdate<NetworkStreamDriver>();
        //state.RequireForUpdate<GhostCollectionPrefab>();
        // Система спит, пока Netcode полностью не создаст сетевой мир клиента
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    //public void OnDestroy(ref SystemState state) { }

    public void OnUpdate(ref SystemState state)
    {
        // 1. Берем официальный маркер готовности сетевой конфигурации в 1.13+
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime))
        {
            return; // Если сетевое время еще не синхронизировано, значит мир не готов — ждем
        }

        // 1. Проверяем, создана ли уже коллекция префабов Netcode на клиенте
        var collectionQuery = SystemAPI.QueryBuilder().WithAll<GhostCollectionPrefab>().Build();
        if (collectionQuery.IsEmpty) return; // Если коллекции еще нет, ждем

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // 2. Ищем сущность сетевого соединения с сервером
        foreach (var (
            networkId,
            entity
        ) in SystemAPI.Query<
            RefRO<NetworkId>
            >()
            .WithNone<NetworkStreamInGame>()
            //.WithChangeFilter<NetworkId>()
            .WithEntityAccess()
        )
        {
            // Проверяем, что мы еще НЕ отправили запрос и еще НЕ в игре
            if (!SystemAPI.HasComponent<NetworkStreamInGame>(entity))
            {
                // Отправляем RPC серверу: "Я готов, включай меня!"
                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new GoInGameRequestRpc() { ValueForBurstRegistry = 1 });
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest() { TargetConnection = entity });

                ecb.AddComponent<NetworkStreamInGame>(entity);

#if UNITY_EDITOR
                UnityEngine.Debug.Log("[Клиент]: Клиент готов к игр! Изменяет статус на - В ИГРЕ");
#endif
            }
        }
        ecb.Playback(state.EntityManager);

        ecb.Dispose();
    }
    //[BurstCompile]
    //    public void OnUpdate(ref SystemState state)
    //    {
    //        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

    //        foreach ((RefRO<NetworkId> networkId, Entity entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
    //        {
    //            ecb.AddComponent<NetworkStreamInGame>(entity);
    //#if UNITY_EDITOR
    //            UnityEngine.Debug.Log("[Клиент]: Изменил статус на - В ИГРЕ");
    //#endif
    //            Entity rpcEntity = ecb.CreateEntity();
    //            ecb.AddComponent(rpcEntity, new GoInGameRequestRpc());
    //            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest());
    //        }

    //        ecb.Playback(state.EntityManager);

    //        ecb.Dispose();
    //        //var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
    //        //    .CreateCommandBuffer(state.WorldUnmanaged);

    //        //// Ищем сущность сетевого соединения, которая только что успешно подключилась к серверу,
    //        //// но еще не переведена в игровое состояние (WithNone<NetworkStreamInGame>)
    //        //foreach (var (connection, entity) in SystemAPI.Query<RefRO<NetworkStreamConnection>>()
    //        //    .WithNone<NetworkStreamInGame>()
    //        //    .WithEntityAccess())
    //        //{
    //        //    // Проверяем, что соединение действительно установлено (валидно)
    //        //    if (connection.ValueRO.CurrentState == Unity.NetCode.ConnectionState.State.Connecting)
    //        //    {
    //        //        // ====================================================================
    //        //        // AAA-АКТИВАЦИЯ ПАКЕТОВ: Разрешаем клиенту принимать Snapshot-ы вокселей!
    //        //        // ====================================================================
    //        //        ecb.AddComponent<NetworkStreamInGame>(entity);
    //        //        // ====================================================================

    //        //        UnityEngine.Debug.LogWarning("[CLIENT]: Сетевое соединение успешно установлено и переведено В ИГРУ. ОжиданиеSnapshot-ов от сервера...");
    //        //    }
    //        //}
    //    }
}
