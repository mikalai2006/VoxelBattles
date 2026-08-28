using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct MonitorClientConnectionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ищем сущности сетевого соединения, которые только что создались (WithFirstTimeStarted)
        foreach (var netId in SystemAPI.Query<RefRO<NetworkId>>().WithChangeFilter<NetworkId>())
        {
            UnityEngine.Debug.Log($"[CLIENT] Network ID: {netId.ValueRO.Value}");
        }
    }
}
