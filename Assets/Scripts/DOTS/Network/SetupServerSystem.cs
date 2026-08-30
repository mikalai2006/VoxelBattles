using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // Только для сервера
public partial struct SetupServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Система не начнет работу, пока Netcode полностью не инициализирует сетевой драйвер
        state.RequireForUpdate<NetworkStreamDriver>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Получаем доступ к низкоуровневому сетевому драйверу Netcode for Entities
        var networkDriver = SystemAPI.GetSingletonRW<NetworkStreamDriver>();

        // Ждем, пока в мире появится сущность с запросом на запуск сервера
        foreach (var (config, entity) in SystemAPI.Query<RefRO<InitServerTag>>().WithEntityAccess())
        {
            // Настраиваем адрес прослушивания (слушать любые IPv4 адреса на указанном порту)
            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(config.ValueRO.Port);

            // Приказываем Netcode запустить сервер
            networkDriver.ValueRW.Listen(endpoint);

            UnityEngine.Debug.Log($"[SERVER] Сервер запущен и слушает порт {config.ValueRO.Port}...");

            // ПРАВИЛЬНО: После успешного запуска Listen, принудительно задаем 
            // конфигурацию тиков сервера, чтобы он не зависел от FPS редактора Unity!
            if (SystemAPI.TryGetSingletonRW<ClientServerTickRate>(out var tickRate))
            {
                // Создаем конфигурацию тиков
                var tickRateConfig = new ClientServerTickRate
                {
                    // Режим ожидания: BusyWait идеален для серверов, чтобы контролировать тайминги
                    //#if UNITY_SERVER
                    //                    TargetFrameRateMode = ClientServerTickRate.FrameRateMode.Sleep
                    //#else
                    //                    TargetFrameRateMode = ClientServerTickRate.FrameRateMode.BusyWait
                    //#endif
                    // 1. Частота логики сервера (60 тиков в секунду)
                    SimulationTickRate = 60,


                    // 2. Частота отправки пакетов в сеть (10 раз в секунду)
                    // Сервер будет пропускать по 5 кадров симуляции перед отправкой пакета!
                    NetworkTickRate = 10,

                    // Ограничение: сколько тиков сервер может обработать ЗА ОДИН кадр,
                    // если он вдруг начнет лагать (защита от "спирали смерти"). 
                    MaxSimulationStepsPerFrame = 2,
                    MaxSimulationStepBatchSize = 2,

                };
                tickRate.ValueRW = tickRateConfig;
                //                //// Настраиваем сервер на стабильные 60 тиков в секунду (или 30, как в вашем проекте)
                //#if UNITY_SERVER
                //                tickRate.ValueRW.SimulationTickRate = 30;
                //                tickRate.ValueRW.NetworkTickRate = 30;
                //#else
                //                tickRate.ValueRW.SimulationTickRate = 60;
                //                tickRate.ValueRW.NetworkTickRate = 60;
                //#endif
                //                //// КРИТИЧЕСКИЙ ФИКС: Отключаем батчинг пакетов на транспортном уровне, 
                //                //// если сервер работает локально в редакторе (в режиме Multiplayer Play Mode)
                //                //tickRate.ValueRW.MaxSimulationStepBatchSize = 1;
                //                tickRate.ValueRW.MaxSimulationStepsPerFrame = 2;
            }

            // Удаляем сущность инициализации, чтобы не запускать сервер каждый кадр
            ecb.DestroyEntity(entity);
        }
    }
}
