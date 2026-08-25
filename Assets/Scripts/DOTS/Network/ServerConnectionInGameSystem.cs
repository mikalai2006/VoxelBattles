using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

//[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerConnectionInGameSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    //public void OnDestroy(ref SystemState state) { }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommand, Entity entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
        {
            ecb.AddComponent<NetworkStreamInGame>(receiveRpcCommand.ValueRO.SourceConnection);
#if UNITY_EDITOR
            UnityEngine.Debug.Log("[Сервер] Клиент подключается к серверу.");
#endif
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);

        ecb.Dispose();
    }
}
