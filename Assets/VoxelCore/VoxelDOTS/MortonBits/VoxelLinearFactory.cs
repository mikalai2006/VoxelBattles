using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct LinearBakedModelResult
{
    public NativeArray<int3> ChunkCoords;
    public NativeArray<byte> FlattenedLinearColors;
    public NativeArray<Color32> ModelLocalPalette;

    public void Dispose()
    {
        if (ChunkCoords.IsCreated) ChunkCoords.Dispose();
        if (FlattenedLinearColors.IsCreated) FlattenedLinearColors.Dispose();
        if (ModelLocalPalette.IsCreated) ModelLocalPalette.Dispose();
    }
}

public static class VoxelLinearFactory
{
    public static LinearBakedModelResult BakeLinearModel(SOVoxelData data)
    {
        if (data == null || data.groups == null || data.groups.Count == 0)
        {
            Debug.LogWarning("[Voxel System]: Ïîïûòêà çàïå÷ü ïóñòîé SOVoxelData!");
            return default;
        }

        int3 bigBounds = new int3(data.Bounds.x, data.Bounds.y, data.Bounds.z);
        var chunksDictionary = new Dictionary<int3, List<Vector3Int>>();
        var chunkColors = new Dictionary<Vector3Int, Color32>();
        var uniqueColorsList = new List<Color32>();
        var localPaletteMap = new Dictionary<Color32, byte>();

        byte currentByteIndex = 1;

        for (int g = 0; g < data.groups.Count; g++)
        {
            var group = data.groups[g];
            if (group.voxels == null) continue;

            Color32 groupColor = group.color;
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
                    voxelPos.z < 0 || voxelPos.z >= bigBounds.z) continue;

                int3 chunkCoord = new int3(voxelPos.x >> 5, voxelPos.y >> 5, voxelPos.z >> 5);
                if (!chunksDictionary.ContainsKey(chunkCoord)) chunksDictionary[chunkCoord] = new List<Vector3Int>();
                chunksDictionary[chunkCoord].Add(voxelPos);
                chunkColors[voxelPos] = groupColor;
            }
        }

        int activeChunksCount = chunksDictionary.Count;
        int chunkVolume = 32768; // 32^3

        var outCoords = new NativeArray<int3>(activeChunksCount, Allocator.Persistent);
        var flattenedColors = new NativeArray<byte>(activeChunksCount * chunkVolume, Allocator.Persistent);
        var outPalette = new NativeArray<Color32>(uniqueColorsList.Count + 1, Allocator.Persistent);

        outPalette[0] = new Color32(0, 0, 0, 0); // Âîçäóõ
        for (int i = 0; i < uniqueColorsList.Count; i++) outPalette[i + 1] = uniqueColorsList[i];

        int chunkIdx = 0;
        foreach (var pair in chunksDictionary)
        {
            outCoords[chunkIdx] = pair.Key;
            int chunkOffset = chunkIdx * chunkVolume;

            for (int i = 0; i < chunkVolume; i++) flattenedColors[chunkOffset + i] = 0;

            List<Vector3Int> globalVoxels = pair.Value;
            for (int v = 0; v < globalVoxels.Count; v++)
            {
                Vector3Int globalPos = globalVoxels[v];
                int3 localPos = new int3(globalPos.x & 31, globalPos.y & 31, globalPos.z & 31);

                // ÊÀÍÎÍÈ×ÅÑÊÈÉ ËÈÍÅÉÍÛÉ ÈÍÄÅÊÑ ÂÌÅÑÒÎ ÌÎÐÒÎÍÀ
                int linearIndex = localPos.x + (localPos.y << 5) + (localPos.z << 10);

                Color32 rawColor = chunkColors[globalPos];
                if (!localPaletteMap.TryGetValue(rawColor, out byte colorIndex)) colorIndex = 1;

                flattenedColors[chunkOffset + linearIndex] = colorIndex;
            }
            chunkIdx++;
        }

        return new LinearBakedModelResult { ChunkCoords = outCoords, FlattenedLinearColors = flattenedColors, ModelLocalPalette = outPalette };
    }
}
