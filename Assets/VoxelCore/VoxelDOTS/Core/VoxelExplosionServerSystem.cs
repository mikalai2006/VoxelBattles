using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct VoxelExplosionServerSystem : ISystem
{
    private EntityQuery m_RpcQuery;

    public void OnCreate(ref SystemState state)
    {
        // Ищем входящие RPC-команды взрыва от клиентов
        m_RpcQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<VoxelExplosionRequest>(),
            ComponentType.ReadOnly<ReceiveRpcCommandRequest>()
        );
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (m_RpcQuery.IsEmptyIgnoreFilter) return;

        var entities = m_RpcQuery.ToEntityArray(Allocator.Temp);
        var requests = m_RpcQuery.ToComponentDataArray<VoxelExplosionRequest>(Allocator.Temp);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < entities.Length; i++)
        {
            var req = requests[i];

            // Если машина, в которую кликнули, уже уничтожена на сервере — игнорируем
            if (!state.EntityManager.Exists(req.TargetEntity) || !state.EntityManager.HasComponent<LocalTransform>(req.TargetEntity))
            {
                ecb.DestroyEntity(entities[i]);
                continue;
            }

            LocalTransform vehicleTransform = state.EntityManager.GetComponentData<LocalTransform>(req.TargetEntity);
            float3 localHitPos = math.transform(math.inverse(vehicleTransform.ToMatrix()), req.WorldPosition);

            DynamicBuffer<LocalChunkDestructionMask> destructionMask = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(req.TargetEntity);

            int3 centerVoxel = (int3)math.round(localHitPos);
            int intRadius = (int)math.ceil(req.Radius);

            // Тройной цикл побитового выжигания маски
            for (int z = centerVoxel.z - intRadius; z <= centerVoxel.z + intRadius; z++)
            {
                if (z < 0 || z > 31) continue;
                for (int y = centerVoxel.y - intRadius; y <= centerVoxel.y + intRadius; y++)
                {
                    if (y < 0 || y > 31) continue;
                    for (int x = centerVoxel.x - intRadius; x <= centerVoxel.x + intRadius; x++)
                    {
                        if (x < 0 || x > 31) continue;

                        float3 currentVoxelPos = new float3(x, y, z);
                        if (math.distance(localHitPos, currentVoxelPos) <= req.Radius)
                        {
                            int flatIndex = x + (y << 5) + (z << 10);
                            int ulongIndex = flatIndex >> 6;
                            int bitOffset = flatIndex & 63;

                            ulong currentMaskBit = 1UL << bitOffset;

                            LocalChunkDestructionMask maskElement = destructionMask[ulongIndex];
                            maskElement.Value |= currentMaskBit; // Маска реплицируется автоматически!
                            destructionMask[ulongIndex] = maskElement;
                        }
                    }
                }
            }


            UnityEngine.Debug.LogWarning("Принудительно взводим тег обновления физики/визуала на сервере");
            //// Принудительно взводим тег обновления физики/визуала на сервере
            //if (state.EntityManager.HasComponent<ChunkGraphicsFlushTag>(req.TargetEntity))
            //{
            //    state.EntityManager.SetComponentEnabled<ChunkGraphicsFlushTag>(req.TargetEntity, true);
            //}

            // Удаляем сущность входящего RPC-пакета, она обработана
            ecb.DestroyEntity(entities[i]);
        }

        entities.Dispose();
        requests.Dispose();
    }
}
