using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

            FixedList512Bytes<NodeData> nodes = new FixedList512Bytes<NodeData>();
            FixedList512Bytes<ChunkData> chunks = new FixedList512Bytes<ChunkData>();

            //==================BODY=====================
            byte bodyNodeId = 1;
            var bodyNode = new NodeData
            {
                Offset = new float3(32f, 0, 32f),
                NodeId = bodyNodeId,
                TypeCollider = 1,
                ParentTargetEntity = 0
            };
            nodes.Add(bodyNode);

            ChunkData chunkBody = new ChunkData
            {
                HashName = intent.ValueRO.bodyData.HashName,
                ParentNodeId = bodyNodeId // Привязываем чанк к узлу.
            };

            chunks.Add(chunkBody);

            //==================TOWER=====================
            byte towerNodeId = 2;

            var towerNode = new NodeData
            {
                Offset = float3.zero,
                NodeId = towerNodeId, // Присваиваем ID узлу.
                TypeCollider = 0,
                ParentTargetEntity = bodyNodeId
            };

            nodes.Add(towerNode);

            ChunkData chunkTower = new ChunkData
            {
                HashName = intent.ValueRO.towerData.HashName,
                ParentNodeId = towerNodeId // Привязываем чанк к узлу.
            };

            chunks.Add(chunkTower);



            ecb.AddComponent(rpcEntity, new RequestServerSpawnModelRpc1
            {
                nodes = nodes,
                chunks = chunks,
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
