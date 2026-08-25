using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial struct GlobalVoxelCacheLifecycleSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Создаем и инициализируем синглтон кэша
        var cache = new GlobalVoxelModelCache();
        cache.Init();

        state.EntityManager.CreateSingleton(cache, "GlobalVoxelModelCache");
    }

    public void OnDestroy(ref SystemState state)
    {
        // Безопасно очищаем persistent-коллекции при уничтожении мира
        if (SystemAPI.TryGetSingleton<GlobalVoxelModelCache>(out var cache))
        {
            cache.Dispose();
        }
    }
}
