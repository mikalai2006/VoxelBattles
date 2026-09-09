using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// та система слушает входящие RPC от клиентов. 
/// При получении запроса она инстанциирует только корневую Ghost-сущность объекта. 
/// На этом кадре у объекта еще нет сетевого ghostId — Netcode присвоит его в конце кадра. 
/// Поэтому мы не можем спавнить чанки сразу, чтобы не сломать сетевую привязку.
/// </summary>

// Временный unmanaged-компонент задачи для сервера
public struct VoxelSpawnTaskComponent : IComponentData
{
    //public Entity TargetRootEntity; // На какой корень привязать чанки
    public Entity TargetNodeEntity; // На какой node привязать чанки
    public ChunkData ChunkData;
    //public BodyData bodyData;
    //public TowerData towerData;
    //public WheelsData wheelsData;
    public float3 SpawnPosition;
    public quaternion SpawnRotation; // Поворот объекта
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ServerSpawnRpcHandlerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Система не запустит OnUpdate, пока не появится ровно один синглтон
        state.RequireForUpdate<VoxelGhostPrefabConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var prefabConfig = SystemAPI.GetSingleton<VoxelGhostPrefabConfig>();

        foreach (var (rpc, requestSource, rpcEntity) in SystemAPI.Query<RefRO<RequestServerSpawnModelRpc1>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            ecb.DestroyEntity(rpcEntity);

            // 1. ИСПРАВЛЕНИЕ: Извлекаем сущность сетевого соединения (Connection Entity) из запроса
            Entity connectionEntity = requestSource.ValueRO.SourceConnection;

            // 2. ИСПРАВЛЕНИЕ: Теперь мы БЕЗОПАСНО считываем NetworkId прямо с соединения через EntityManager, 
            // так как сущность соединения уже давно существует в мире!
            int clientNetworkId = 0;
            if (state.EntityManager.HasComponent<NetworkId>(connectionEntity))
            {
                clientNetworkId = state.EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
            }
            ////#if UNITY_EDITOR
            ////            UnityEngine.Debug.Log($"[Server] Spawn request from clientNetworkId={clientNetworkId}");
            ////#endif
            //// Спавним корень из Ghost-префаба
            //Entity rootEntity = ecb.Instantiate(prefabConfig.RootGhostPrefab);

            //// НАЗНАЧАЕМ ВЛАДЕЛЬЦА (теперь это безопасно, так как мы пишем команду в ECB, а не читаем)
            //ecb.AddComponent(rootEntity, new GhostOwner { NetworkId = -1 }); //clientNetworkId

            //ecb.SetComponent(rootEntity, new LocalTransform
            //{
            //    Position = rpc.ValueRO.SpawnPosition,
            //    Rotation = quaternion.identity,
            //    Scale = 1.0f
            //});

            //// ВАЖНО: Netcode требует, чтобы в LinkedEntityGroup первым элементом ВСЕГДА был сам Root!
            //// Получаем буфер, который Netcode автоматически создал на Root-префабе
            //var linkedEntities = ecb.SetBuffer<LinkedEntityGroup>(rootEntity);
            //linkedEntities.Add(new LinkedEntityGroup { Value = rootEntity });


            // 1. Сначала спавним/создаем все Узлы
            // Сохраняем их локальные Entity в массив, где индекс равен NodeId
            NativeArray<Entity> spawnedNodes = new NativeArray<Entity>(rpc.ValueRO.nodes.Length + 1, Allocator.Temp);


            // Спавним узлы
            for (int i = 0; i < rpc.ValueRO.nodes.Length; i++)
            {
                Entity nodeEntity = ecb.Instantiate(prefabConfig.RootGhostPrefab);

                ecb.AddComponent(nodeEntity, new GhostOwner { NetworkId = -1 });

                ecb.SetComponent(nodeEntity, new LocalTransform
                {
                    Position = rpc.ValueRO.SpawnPosition + rpc.ValueRO.nodes[i].Offset,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                });

                var linkedNodeEntities = ecb.SetBuffer<LinkedEntityGroup>(nodeEntity);

                linkedNodeEntities.Add(new LinkedEntityGroup { Value = nodeEntity });

                spawnedNodes[rpc.ValueRO.nodes[i].NodeId] = nodeEntity;

                ecb.SetComponent(nodeEntity, new AAA_RootData
                {
                    TypeCollider = rpc.ValueRO.nodes[i].TypeCollider,
                });

                //ecb.AddComponent(nodeEntity, new Parent { Value = rootEntity });
            }

            // выставляем зависимости
            for (int i = 0; i < rpc.ValueRO.nodes.Length; i++)
            {
                if (rpc.ValueRO.nodes[i].ParentTargetEntity > 0)
                {
                    ecb.SetComponent(spawnedNodes[rpc.ValueRO.nodes[i].NodeId], new AAA_RootData
                    {
                        RootEntity = spawnedNodes[(int)rpc.ValueRO.nodes[i].ParentTargetEntity]
                    });
                }
            }

            for (int j = 0; j < rpc.ValueRO.chunks.Length; j++)
            {
                Entity taskEntity = ecb.CreateEntity();

                ecb.AddComponent(taskEntity, new VoxelSpawnTaskComponent
                {
                    //TargetRootEntity = rootEntity,
                    TargetNodeEntity = spawnedNodes[rpc.ValueRO.chunks[j].ParentNodeId],
                    ChunkData = rpc.ValueRO.chunks[j],
                    //bodyData = rpc.ValueRO.bodyData,
                    //towerData = rpc.ValueRO.towerData,
                    //wheelsData = rpc.ValueRO.wheelsData,
                    SpawnPosition = rpc.ValueRO.SpawnPosition + rpc.ValueRO.chunks[j].Offset,
                    SpawnRotation = rpc.ValueRO.SpawnRotation,
                });
            }


            //#if UNITY_EDITOR
            //            UnityEngine.Debug.LogWarning($"[Server] Spawn task created for hash={rpc.ValueRO.ConfigHashName}");
            //#endif
        }
    }
}