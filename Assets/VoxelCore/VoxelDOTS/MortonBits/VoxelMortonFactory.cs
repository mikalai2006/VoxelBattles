using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Структура, возвращающую плоские байтовые шаблоны цветов, которые будут храниться 
/// в persistent-памяти глобального кэша ServerVoxelCache под Entities 1.4.7.
/// </summary>
public struct MortonBakedModelResult
{
    // Список трехмерных координат для каждого активного чанка
    public NativeArray<int3> ChunkCoords;

    // Плотный плоский массив байт для ВСЕХ чанков модели.
    // Размер равен: Количество Чанков * 32768.
    // Индексация внутри чанка идет строго по Z-кривой Мортона.
    public NativeArray<byte> FlattenedMortonColors;

    // ДОБАВЛЯЕМ: Сюда фабрика запишет уникальные цвета модели
    public NativeArray<Color32> ModelLocalPalette;

    // Вспомогательный метод для освобождения persistent-памяти
    public void Dispose()
    {
        if (ChunkCoords.IsCreated) ChunkCoords.Dispose();
        if (FlattenedMortonColors.IsCreated) FlattenedMortonColors.Dispose();
        if (ModelLocalPalette.IsCreated) ModelLocalPalette.Dispose();
    }
}

public static class VoxelMortonFactory
{
    public static MortonBakedModelResult BakeMortonModel(SOVoxelData data)
    {
        if (data == null || data.groups == null || data.groups.Count == 0)
        {
            Debug.LogWarning("[Voxel System]: Попытка запечь пустой SOVoxelData!");
            return default;
        }

        int3 bigBounds = new int3(data.Bounds.x, data.Bounds.y, data.Bounds.z);
        var chunksDictionary = new Dictionary<int3, List<Vector3Int>>();
        var chunkColors = new Dictionary<Vector3Int, Color32>();

        // Временный список для сбора УНИКАЛЬНЫХ цветов конкретно этой модели
        var uniqueColorsList = new List<Color32>();
        var localPaletteMap = new Dictionary<Color32, byte>();

        // Индекс 0 строго зарезервирован под воздух/пустоту
        byte currentByteIndex = 1;

        // Сбор и сортировка вокселей по трехмерным индексам чанков
        for (int g = 0; g < data.groups.Count; g++)
        {
            var group = data.groups[g];
            if (group.voxels == null) continue;

            Color32 groupColor = group.color;

            // Если этот цвет мы еще не встречали в модели — регистрируем его
            if (!localPaletteMap.ContainsKey(groupColor) && currentByteIndex <= 255)
            {
                localPaletteMap[groupColor] = currentByteIndex;
                uniqueColorsList.Add(groupColor);
                currentByteIndex++;
            }

            for (int v = 0; v < group.voxels.Count; v++)
            {
                Vector3Int voxelPos = group.voxels[v];

                if (voxelPos.x < 0 || voxelPos.x >= bigBounds.x ||
                    voxelPos.y < 0 || voxelPos.y >= bigBounds.y ||
                    voxelPos.z < 0 || voxelPos.z >= bigBounds.z)
                {
                    continue;
                }

                int3 chunkCoord = new int3(voxelPos.x >> 5, voxelPos.y >> 5, voxelPos.z >> 5);

                if (!chunksDictionary.ContainsKey(chunkCoord))
                {
                    chunksDictionary[chunkCoord] = new List<Vector3Int>();
                }
                chunksDictionary[chunkCoord].Add(voxelPos);
                chunkColors[voxelPos] = groupColor;
            }
        }

        return ProcessFlattenedMorton(chunksDictionary, chunkColors, uniqueColorsList);
    }

    private static MortonBakedModelResult ProcessFlattenedMorton(
    Dictionary<int3, List<Vector3Int>> chunksDictionary,
    Dictionary<Vector3Int, Color32> chunkColors,
    List<Color32> modelUniqueColors)
    {
        int activeChunksCount = chunksDictionary.Count;
        int chunkVolume = 32768;

        var outCoords = new NativeArray<int3>(activeChunksCount, Allocator.Persistent);
        var flattenedColors = new NativeArray<byte>(activeChunksCount * chunkVolume, Allocator.Persistent);

        // Создаем локальную мапу для этой модели
        var localRegistry = new Dictionary<Color32, byte>();
        var outPalette = new NativeArray<Color32>(modelUniqueColors.Count + 1, Allocator.Persistent);

        outPalette[0] = new Color32(0, 0, 0, 0); // 0 - всегда воздух
        for (int i = 0; i < modelUniqueColors.Count; i++)
        {
            outPalette[i + 1] = modelUniqueColors[i];
            localRegistry[modelUniqueColors[i]] = (byte)(i + 1);
        }

        int chunkIdx = 0;
        foreach (var pair in chunksDictionary)
        {
            int3 currentChunkCoord = pair.Key;
            List<Vector3Int> globalVoxels = pair.Value;

            outCoords[chunkIdx] = currentChunkCoord;

            // Вычисляем стартовый индекс в общем массиве для текущего чанка
            int chunkOffset = chunkIdx * chunkVolume;

            // Заполняем область текущего чанка нулями (воздух)
            for (int i = 0; i < chunkVolume; i++)
            {
                flattenedColors[chunkOffset + i] = 0;
            }

            // Переносим воксели с учетом Z-порядка Мортона
            for (int v = 0; v < globalVoxels.Count; v++)
            {
                Vector3Int globalPos = globalVoxels[v];
                int3 localPos = new int3(globalPos.x & 31, globalPos.y & 31, globalPos.z & 31);
                int mortonIndex = VoxelMortonMath.GetMortonIndex(localPos);

                Color32 rawColor = chunkColors[globalPos];

                // Ищем индекс цвета строго внутри палитры этой модели
                if (!localRegistry.TryGetValue(rawColor, out byte colorIndex)) colorIndex = 1;

                flattenedColors[chunkOffset + mortonIndex] = colorIndex;
            }

            chunkIdx++;
        }

        return new MortonBakedModelResult
        {
            ChunkCoords = outCoords,
            FlattenedMortonColors = flattenedColors,
            ModelLocalPalette = outPalette // Передаем палитру наружу
        };
    }
}

