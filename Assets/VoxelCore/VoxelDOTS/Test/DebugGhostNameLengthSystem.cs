using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DebugGhostNameLengthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Получаем менеджер сущностей для текущего мира
        var entityManager = state.EntityManager;

        // 2. Выбираем абсолютно ВСЕ сущности в мире
        var allEntitiesQuery = SystemAPI.QueryBuilder().Build();

        if (allEntitiesQuery.IsEmpty) return;

        // Превращаем запрос в массив сущностей для безопасного перебора
        using var entities = allEntitiesQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        // ПРАВИЛО: Заменили символы сравнения на слова БОЛЬШЕ и МЕНЬШЕ
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            // Используем официальный публичный метод для получения имени сущности
            FixedString64Bytes entityName = entityManager.GetName(entity);

            // ПРАВИЛО: Заменили символ сравнения длины на БОЛЬШЕ
            if (entityName.Length > 60)
            {
                UnityEngine.Debug.LogError(
                    $"[НАЙДЕНА ДЛИННАЯ СТРОКА] Сущность ID: {entity.Index}, " +
                    $"Длина: {entityName.Length} байт. " +
                    $"Полное имя сущности: {entityName}"
                );
            }
        }
    }
}
