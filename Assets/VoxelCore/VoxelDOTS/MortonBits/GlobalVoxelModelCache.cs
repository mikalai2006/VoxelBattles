using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Для каждой модели мы регистрируем её чанки. 
/// Чтобы быстро находить смещение (ChunkOffset) внутри Burst-джоб, 
/// мы заменяем managed-словари на плоский NativeParallelHashMap
/// </summary>
public struct ModelRuntimeTemplate
{
    // Размер модели в чанках
    public int3 SizeModel;
    // Ключ: Локальная координата чанка (int3) -> Значение: Порядковый ID чанка (индекс)
    // Используя этот индекс, мы находим смещение: Порядковый_ID * 32768
    public NativeParallelHashMap<int3, int> ChunkCoordToOrderIndexMap;

    // Тот самый плоский массив цветов, сгенерированный нашей Morton-фабрикой
    public NativeArray<byte> FlattenedLinearColors;


    // ПАЛИТРА ДЛЯ КАЖДОЙ МОДЕЛИ: Выделяем память ровно под количество цветов этой модели!
    public NativeArray<Color32> PaletteColors;

    public void Dispose()
    {
        if (ChunkCoordToOrderIndexMap.IsCreated) ChunkCoordToOrderIndexMap.Dispose();
        if (FlattenedLinearColors.IsCreated) FlattenedLinearColors.Dispose();
        if (PaletteColors.IsCreated) PaletteColors.Dispose();
    }
}

/// <summary>
/// Этот компонент-синглтон живет в памяти и на сервере, и на клиенте. 
/// Он хранит шаблоны для всех зарегистрированных в игре конфигураций
/// </summary>
public struct GlobalVoxelModelCache : IComponentData
{
    // Ключ: ConfigHashName -> Значение: Вся unmanaged-информация о модели
    public NativeParallelHashMap<uint, ModelRuntimeTemplate> Templates;

    public void Init()
    {
        Templates = new NativeParallelHashMap<uint, ModelRuntimeTemplate>(512, Allocator.Persistent);
    }

    public void Dispose()
    {
        if (Templates.IsCreated)
        {
            var kvps = Templates.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < kvps.Values.Length; i++)
            {
                var template = kvps.Values[i];
                template.Dispose();
            }
            Templates.Dispose();
        }
    }
}
