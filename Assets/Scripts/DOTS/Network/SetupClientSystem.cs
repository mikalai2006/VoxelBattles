using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Работает только на клиенте
public partial struct SetupClientSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Берем команду-буфер для безопасного удаления сущностей конфигурации в конце кадра
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        bool shouldConnect = false;
        FixedString64Bytes targetAddress = default;
        ushort targetPort = 0;

        // 1. Цикл только СБИРАЕТ данные о запросе на подключение.
        // Здесь мы ничего не меняем в мире ECS, поэтому валидатор безопасности полностью спокоен.
        foreach (var (config, entity) in SystemAPI.Query<RefRO<InitClientTag>>().WithEntityAccess())
        {
            targetAddress = config.ValueRO.Address;
            targetPort = config.ValueRO.Port;
            shouldConnect = true;

            // Кладем команду на уничтожение сущности-тега в буфер (применится в конце кадра)
            ecb.DestroyEntity(entity);
        }

        // 2. ВЫПОЛНЯЕМ ПОДКЛЮЧЕНИЕ СТРОГО ВНЕ ЦИКЛА:
        // Цикл завершен, итерации по сущностям больше нет. Теперь вызов EntityManager внутри Connect абсолютно легален!
        if (shouldConnect)
        {
            var networkDriver = SystemAPI.GetSingletonRW<NetworkStreamDriver>();

            if (NetworkEndpoint.TryParse(targetAddress, targetPort, out var endpoint))
            {
                // Приказываем Netcode подключиться к серверу. Больше никакой Structural Changes ошибки не будет.
                networkDriver.ValueRW.Connect(state.EntityManager, endpoint);
                UnityEngine.Debug.Log($"[CLIENT] Попытка ручного подключения к {targetAddress}:{targetPort}...");
            }
            else
            {
                UnityEngine.Debug.LogError($"[CLIENT] Неверный IP адрес или порт: {targetAddress}:{targetPort}");
            }
            // ПРАВИЛЬНО: После успешного запуска Listen, принудительно задаем 
            // конфигурацию тиков сервера, чтобы он не зависел от FPS редактора Unity!
            if (SystemAPI.TryGetSingletonRW<ClientServerTickRate>(out var tickRate))
            {
                //// Настраиваем сервер на стабильные 60 тиков в секунду (или 30, как в вашем проекте)
                //tickRate.ValueRW.SimulationTickRate = 45;
                //tickRate.ValueRW.NetworkTickRate = 45;

                tickRate.ValueRW.MaxSimulationStepsPerFrame = 2;
            }
        }
    }
}
