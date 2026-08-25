using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// Один единственный правильный атрибут: запуск в самом начале группы симуляции Unity.
// Никаких текстовых связей с Netcode-группами — варнинг гарантированно исчезнет.
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct VehiclePredictionOwnershipSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Ищем абсолютно все машины на клиенте, у которых есть ваш тэг контроля
        // (withPresent позволяет видеть сущность, даже если сам IEnableableComponent выключен)
        foreach (var (ghost, controlledTag, entity) in SystemAPI.Query<RefRO<GhostInstance>, RefRO<IsControlledTag>>()
                     .WithPresent<IsControlledTag>()
                     .WithEntityAccess())
        {
            // Определяем, должен ли этот конкретный клиент сейчас предсказывать машину
            // (Компонент контроля должен быть включен И сетевой флаг IsActive должен быть true)
            bool shouldPredict = state.EntityManager.IsComponentEnabled<IsControlledTag>(entity) &&
                                 controlledTag.ValueRO.IsActive;

            // Если на машине есть встроенный сетевой компонент Simulate
            if (SystemAPI.HasComponent<Simulate>(entity))
            {
                bool isSimulateEnabled = state.EntityManager.IsComponentEnabled<Simulate>(entity);

                // СЦЕНАРИЙ А: Машина под контролем, но симуляция выключена -> ВКЛЮЧАЕМ
                if (shouldPredict && !isSimulateEnabled)
                {
                    SystemAPI.SetComponentEnabled<Simulate>(entity, true);
                    //UnityEngine.Debug.LogWarning($"[Netcode] Симуляция включена для управляемой машины: {ghost.ValueRO.ghostId}");
                }
                // СЦЕНАРИЙ Б: Машина НЕ под контролем, но симуляция активна -> ВЫКЛЮЧАЕМ
                // (Именно это сработает для 19 машин прямо при их спавне!)
                else if (!shouldPredict && isSimulateEnabled)
                {
                    SystemAPI.SetComponentEnabled<Simulate>(entity, false);
                    //UnityEngine.Debug.LogWarning($"[Netcode] Симуляция выключена (интерполяция) для фоновой машины: {ghost.ValueRO.ghostId}");
                }
            }
        }
    }
}

