using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

public struct GoInGameRequestRpc : IRpcCommand { }

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Выполнять строго на Клиенте
public partial struct ClientConnectionInGameSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //// Система спит, пока в мире не появится хоть одно сетевое соединение
        //state.RequireForUpdate<NetworkStreamConnection>();

        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    //public void OnDestroy(ref SystemState state) { }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach ((RefRO<NetworkId> networkId, Entity entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            ecb.AddComponent<NetworkStreamInGame>(entity);
#if UNITY_EDITOR
            UnityEngine.Debug.Log("[Клиент]: Изменил статус на - В ИГРЕ");
#endif
            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new GoInGameRequestRpc());
            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest());
        }

        ecb.Playback(state.EntityManager);

        ecb.Dispose();
        //var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
        //    .CreateCommandBuffer(state.WorldUnmanaged);

        //// Ищем сущность сетевого соединения, которая только что успешно подключилась к серверу,
        //// но еще не переведена в игровое состояние (WithNone<NetworkStreamInGame>)
        //foreach (var (connection, entity) in SystemAPI.Query<RefRO<NetworkStreamConnection>>()
        //    .WithNone<NetworkStreamInGame>()
        //    .WithEntityAccess())
        //{
        //    // Проверяем, что соединение действительно установлено (валидно)
        //    if (connection.ValueRO.CurrentState == Unity.NetCode.ConnectionState.State.Connecting)
        //    {
        //        // ====================================================================
        //        // AAA-АКТИВАЦИЯ ПАКЕТОВ: Разрешаем клиенту принимать Snapshot-ы вокселей!
        //        // ====================================================================
        //        ecb.AddComponent<NetworkStreamInGame>(entity);
        //        // ====================================================================

        //        UnityEngine.Debug.LogWarning("[CLIENT]: Сетевое соединение успешно установлено и переведено В ИГРУ. ОжиданиеSnapshot-ов от сервера...");
        //    }
        //}
    }
}
