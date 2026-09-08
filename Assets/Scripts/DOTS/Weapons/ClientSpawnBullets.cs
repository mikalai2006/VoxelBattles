using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Только для клиентов
public partial struct ClientSpawnBullets : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<InputStateSingleton>(out var inputSingleton)) return;

        if (!SystemAPI.TryGetSingleton<VoxelPrefabConfig>(out var prefabsRef)) return;

        // Ввод в Netcode for Entities обрабатывается через циклы по игрокам
        foreach (var (transform, playerEntity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
        {
            // Проверяем, нажал ли игрок кнопку «Огонь» на текущем сетевом тике
            if (!inputSingleton.ShootTriggered) continue;

            //// Хелпер Netcode гарантирует, что предсказанный спавн на клиенте 
            //// произойдет только один раз для этого тика (игнорируя роллбэки сети)
            //if (!PredictSpawnHelper.CheckAndIsFirstTime(ref state, playerEntity)) continue;

            // --- Ваши данные конфигурации из кэша ---
            uint currentWeaponHash = 12345;
            float bulletSpeed = 50f;
            float maxLifetime = 5f;
            int radiusExplode = 5;
            // ----------------------------------------

            float3 spawnPos = transform.ValueRO.Position; // Спавним от позиции игрока
            float3 baseDir = math.forward(transform.ValueRO.Rotation);

            int bulletsCount = 5; // Выстрел пачкой (дробь)

            // Локальный спавн снарядов для мгновенного визуального фидбека (0 пинга)
            NativeArray<Entity> localBullets = new NativeArray<Entity>(bulletsCount, Allocator.Temp);
            state.EntityManager.Instantiate(prefabsRef.BulletPrefab, localBullets);

            ShootEventRPC rpcData = new ShootEventRPC
            {
                ConfigHashName = currentWeaponHash,
                ShooterEntity = playerEntity
            };

            var random = new Unity.Mathematics.Random((uint)SystemAPI.Time.ElapsedTime + 1);

            for (int i = 0; i < localBullets.Length; i++)
            {
                float3 randomDir = math.normalize(baseDir + random.NextFloat3(-0.05f, 0.05f));

                state.EntityManager.SetComponentData(localBullets[i], new BulletData
                {
                    ConfigHashName = currentWeaponHash,
                    Direction = randomDir,
                    Speed = bulletSpeed,
                    RadiusExplode = radiusExplode,
                    Shooter = playerEntity
                });
                state.EntityManager.SetComponentData(localBullets[i], new Lifetime { Value = maxLifetime });
                state.EntityManager.SetComponentData(localBullets[i], LocalTransform.FromPosition(spawnPos));

                // Упаковываем выстрел в RPC для сервера
                rpcData.SpawnedBullets.Add(new BulletSpawnInfo { Position = spawnPos, Direction = randomDir });
            }

            localBullets.Dispose();

            // 1. Создаем пустую сущность-конверт для нашего сетевого пакета
            Entity rpcEntity = state.EntityManager.CreateEntity();

            // 2. Добавляем на неё данные выстрела
            state.EntityManager.AddComponentData(rpcEntity, rpcData);

            // 3. Добавляем стандартный компонент запроса отправки Netcode.
            // На клиенте TargetConnection можно оставить пустым (Entity.Null) — 
            // Netcode автоматически поймет, что пакет нужно отправить на единственный сервер.
            state.EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = Entity.Null });
        }
    }
}