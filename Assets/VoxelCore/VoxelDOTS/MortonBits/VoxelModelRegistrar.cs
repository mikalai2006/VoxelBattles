using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Этот класс служит мостом между вашей C# фабрикой запекания и unmanaged-кэшем. 
/// Он перекладывает данные из временных массивов фабрики в persistent-структуры синглтона.
/// </summary>
public static class VoxelModelRegistrar
{
    public static void RegisterModel(World world, uint configHashName, LinearBakedModelResult bakedResult)
    {
        var entityManager = world.EntityManager;

        // Извлекаем синглтон кэша из текущего мира
        var cacheQuery = entityManager.CreateEntityQuery(typeof(GlobalVoxelModelCache));
        if (cacheQuery.IsEmpty) return;

        var cache = cacheQuery.GetSingleton<GlobalVoxelModelCache>();

        // Проверяем, если модель с таким хэшем уже существует — перезаписываем её с очисткой старой памяти
        if (cache.Templates.TryGetValue(configHashName, out var oldTemplate))
        {
            oldTemplate.Dispose();
            cache.Templates.Remove(configHashName);
        }

        int chunkCount = bakedResult.ChunkCoords.Length;
        int chunkVolume = 32768; // 32^3

        // Создаем unmanaged-структуру для хранения рантайм-шаблона модели
        var runtimeTemplate = new ModelRuntimeTemplate
        {
            ChunkCoordToOrderIndexMap = new NativeParallelHashMap<int3, int>(chunkCount, Allocator.Persistent),
            FlattenedLinearColors = new NativeArray<byte>(chunkCount * chunkVolume, Allocator.Persistent),

            // Копируем палитру этой конкретной модели в unmanaged-кучу текущего мира
            PaletteColors = new NativeArray<Color32>(bakedResult.ModelLocalPalette, Allocator.Persistent)
        };

        // Заполняем карту смещений и плоский массив цветов
        PopulateRuntimeTemplate(ref runtimeTemplate, bakedResult, chunkCount, chunkVolume);

        // Помещаем собранный unmanaged-шаблон в глобальный хэш-мап кэша
        cache.Templates.TryAdd(configHashName, runtimeTemplate);

        // Обновляем синглтон в мире
        cacheQuery.SetSingleton(cache);
    }

    /// <summary>
    /// Внутренний изолированный метод для быстрого копирования данных. 
    /// </summary>
    /// <param name="runtimeTemplate"></param>
    /// <param name="bakedResult"></param>
    /// <param name="chunkCount"></param>
    /// <param name="chunkVolume"></param>
    private static void PopulateRuntimeTemplate(
    ref ModelRuntimeTemplate runtimeTemplate,
    LinearBakedModelResult bakedResult,
    int chunkCount,
    int chunkVolume)
    {
        // 1. Заполняем карту соответствия координат чанка к его порядковому номеру
        for (int i = 0; i < chunkCount; i++)
        {
            int3 coord = bakedResult.ChunkCoords[i];
            runtimeTemplate.ChunkCoordToOrderIndexMap.TryAdd(coord, i);
        }

        // 2. Атомарно копируем весь сплошной массив цветов Мортона в persistent-память кэша
        runtimeTemplate.FlattenedLinearColors.CopyFrom(bakedResult.FlattenedLinearColors);

        // ====================================================================
        // РАСЧЕТ ГАБАРИТОВ МОДЕЛИ В ЧАНКАХ (modelSizeInChunks)
        // Инициализируем минимальные и максимальные границы экстремальными значениями
        // ====================================================================
        int3 minCoord = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
        int3 maxCoord = new int3(int.MinValue, int.MinValue, int.MinValue);

        var chunkCoords = runtimeTemplate.ChunkCoordToOrderIndexMap.GetKeyArray(Allocator.Temp);
        int totalActiveChunks = chunkCoords.Length;
        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
        for (int i = 0; i < totalActiveChunks; i++)
        {
            int3 coord = chunkCoords[i];

            // Находим крайние точки по оси X
            if (coord.x < minCoord.x) minCoord.x = coord.x;
            if (coord.x > maxCoord.x) maxCoord.x = coord.x;

            // Находим крайние точки по оси Y
            if (coord.y < minCoord.y) minCoord.y = coord.y;
            if (coord.y > maxCoord.y) maxCoord.y = coord.y;

            // Находим крайние точки по оси Z
            if (coord.z < minCoord.z) minCoord.z = coord.z;
            if (coord.z > maxCoord.z) maxCoord.z = coord.z;
        }

        // Вычисляем размер: (Максимум - Минимум) + 1 чанк 
        // Прибавляем 1, так как если max и min равны 0 (модель в 1 чанк), размер должен быть равен 1, а не 0!
        int3 modelSizeInChunks = (maxCoord - minCoord) + new int3(1, 1, 1);

        // Для дебага выводим размеры в консоль
        //#if UNITY_EDITOR
        //        UnityEngine.Debug.Log($"[Voxel Registr]: Габариты модели в чанках: X={modelSizeInChunks.x}, Y={modelSizeInChunks.y}, Z={modelSizeInChunks.z}");
        //#endif
        runtimeTemplate.SizeModel = modelSizeInChunks;
        // ====================================================================

    }
}

