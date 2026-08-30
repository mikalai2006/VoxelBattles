#if !UNITY_SERVER
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ClientVoxelBrgInitializationSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Система спит, пока Subscene не запечет наш глобальный managed-конфиг
        RequireForUpdate<VoxelGlobalConfigComponent>();
    }

    protected override void OnUpdate()
    {
        // Извлекаем единственный managed-синглтон графических настроек на клиенте
        var config = SystemAPI.ManagedAPI.GetSingleton<VoxelGlobalConfigComponent>();

        // Проверяем, если материалы уже зарегистрированы в BRG — прерываем выполнение
        if (config.PoolIsPrewarmed)
        {
            Enabled = false; // ФИКС: Используем встроенное свойство SystemBase для отключения
            return;
        }

        // Извлекаем системный доступ к нативному конвейеру графики Unity 6
        var graphicsSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

        // Запускаем изолированный внутренний метод для безопасного извлечения BatchMaterialID
        RegisterMaterialsInBrg(config, graphicsSystem);

        // Помечаем, что рантайм-палитра полностью готова к отрисовке вокселей
        config.PoolIsPrewarmed = true;

        // Отключаем систему, так как регистрация выполняется строго один раз при старте
        Enabled = false; // ФИКС: Успешное выключение без использования слова state
    }
    private static void RegisterMaterialsInBrg(
    VoxelGlobalConfigComponent config,
    EntitiesGraphicsSystem graphicsSystem)
    {
        // 1. Регистрируем сплошной, непрозрачный материал для жадных квадов
        if (config.OpaqueMaterial != null)
        {
            // Нативный метод Unity 6 для регистрации под конвейер BatchRendererGroup
            config.OpaqueMaterialRuntimeID = graphicsSystem.RegisterMaterial(config.OpaqueMaterial);
        }
        else
        {
            Debug.LogError("[Voxel BRG]: OpaqueMaterial отсутствует в конфиге VoxelConfigsAuthoring!");
        }

        // 2. Регистрируем полупрозрачный материал (стекло/воды)
        if (config.TransparentMaterial != null)
        {
            config.TransparentMaterialRuntimeID = graphicsSystem.RegisterMaterial(config.TransparentMaterial);
        }
    }
}
#endif