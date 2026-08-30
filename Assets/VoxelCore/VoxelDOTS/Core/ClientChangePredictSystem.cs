using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

// Система работает СТРОГО на клиенте в группе симуляции предсказания
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ClientChangePredictSystem : ISystem
{
    private ComponentLookup<PredictedGhost> _predictedGhostLookup;

    public void OnCreate(ref SystemState state)
    {
        // Инициализируем быстрый поиск компонента предсказания
        _predictedGhostLookup = state.GetComponentLookup<PredictedGhost>(true);

        // Система начнет работать только тогда, когда в мире сети появится менеджер очередей переключения
        state.RequireForUpdate<SwitchPredictionSmoothingSystem>();

        // Отслеживаем любые сущности, у которых изменился сетевой владелец с сервера
        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<GhostOwner>(),
            ComponentType.ReadOnly<GhostInstance>()
        ));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Ищем очереди переключения предсказания (используем ваш точный синглтон)
        if (!SystemAPI.TryGetSingletonRW<GhostPredictionSwitchingQueues>(out var switchingQueues))
        {
            // Лог защищаем проверкой, чтобы не спамить (и пишем [Client], так как это мир клиента)
            // Примечание: Для полной Burst-совместимости Debug.LogWarning лучше убрать или обернуть,
            // но для обычного отладочного кода в редакторе пойдет.
            UnityEngine.Debug.LogWarning("[Client] GhostPredictionSwitchingQueues not found!");
            return;
        }

        _predictedGhostLookup.Update(ref state);

        if (!SystemAPI.TryGetSingleton<NetworkId>(out var networkId)) return;
        int localPlayerId = networkId.Value;

        // 2. Обрабатываем сущности, у которых изменился владелец
        foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
                     .WithChangeFilter<GhostOwner>()
                     .WithEntityAccess())
        {
            bool isNowOwnedByMe = ghostOwner.ValueRO.NetworkId == localPlayerId;
            bool hasPredictedComponent = _predictedGhostLookup.HasComponent(entity);

            // Пересадка: вышли из машины
            if (!isNowOwnedByMe && hasPredictedComponent)
            {
                switchingQueues.ValueRW.ConvertToInterpolatedQueue.Enqueue(new ConvertPredictionEntry
                {
                    TargetEntity = entity,
                    TransitionDurationSeconds = 0.0f
                });
            }
            // Пересадка: сели в машину
            else if (isNowOwnedByMe && !hasPredictedComponent)
            {
                switchingQueues.ValueRW.ConvertToPredictedQueue.Enqueue(new ConvertPredictionEntry
                {
                    TargetEntity = entity,
                    TransitionDurationSeconds = 0.0f
                });
            }
        }
    }

}
