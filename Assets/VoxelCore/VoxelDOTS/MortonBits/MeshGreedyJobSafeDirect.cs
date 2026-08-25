using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VoxelVertex
{
    public float3 Position;
    public Color32 VertexColor;
}

[BurstCompile]
public struct MeshGreedyJobSafeDirect : IJob
{
    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMask;
    [ReadOnly] public NativeArray<byte> FlattenedModelColors;
    [ReadOnly] public NativeArray<Color32> GlobalPaletteColors;
    public int ChunkOffsetInFlattenedArray;

    public NativeArray<VoxelVertex> OutputVertices;
    public NativeArray<int> OutputIndices;
    public NativeArray<int2> JobCountersRef;

    [BurstCompile]
    public void Execute()
    {
        NativeArray<short> mask = new NativeArray<short>(1024, Allocator.Temp);

        int vertexCounter = 0;
        int indexCounter = 0;

        for (int back = 0; back < 3; back++)
        {
            int u = (back + 1) % 3;
            int v = (back + 2) % 3;

            int3 chunkPos = int3.zero;
            int3 axisVector = int3.zero;
            axisVector[back] = 1;

            // Сканируем от 0 до 32 включительно, чтобы закрыть внешние заглушки чанка!
            for (chunkPos[back] = 0; chunkPos[back] < 33; chunkPos[back]++)
            {
                int n = 0;

                for (chunkPos[v] = 0; chunkPos[v] < 32; chunkPos[v]++)
                {
                    for (chunkPos[u] = 0; chunkPos[u] < 32; chunkPos[u]++)
                    {
                        // Читаем текущий воксель и соседа СЗАДИ (минус axisVector)
                        bool voxelCurrentLive = IsVoxelLive(chunkPos, out byte colorCurrent);
                        bool voxelNeighborLive = IsVoxelLive(chunkPos - axisVector, out byte colorNeighbor);

                        if (voxelCurrentLive == voxelNeighborLive)
                        {
                            mask[n++] = 0; // Грани скрыты внутри монолита
                        }
                        else if (voxelCurrentLive)
                        {
                            // Лицевая грань вокселя (направление четное: 0, 2, 4)
                            mask[n++] = (short)(colorCurrent | ((back * 2) << 8));
                        }
                        else
                        {
                            // Обратная грань вокселя сзади (направление нечетное: 1, 3, 5)
                            mask[n++] = (short)(colorNeighbor | (((back * 2) + 1) << 8));
                        }
                    }
                }

                ExecuteSliceGreedyMeshing(mask, chunkPos[back], back, u, v, ref vertexCounter, ref indexCounter);
            }
        }
        mask.Dispose();
    }
    [BurstCompile]
    private void ExecuteSliceGreedyMeshing(NativeArray<short> sliceMask, int backCoord, int back, int u, int v, ref int vertexCounter, ref int indexCounter)
    {
        for (int j = 0; j < 32; j++)
        {
            for (int i = 0; i < 32; i++)
            {
                int currentMaskIndex = i + (j << 5);
                short maskValue = sliceMask[currentMaskIndex];
                if (maskValue == 0) continue;

                int direction = maskValue >> 8;
                byte colorIndex = (byte)(maskValue & 0xFF);

                int w;
                for (w = 1; (i + w) < 32; w++)
                {
                    if (sliceMask[currentMaskIndex + w] != maskValue) break;
                }

                int h;
                bool canGrowHeight = true;
                for (h = 1; (j + h) < 32; h++)
                {
                    for (int k = 0; k < w; k++)
                    {
                        if (sliceMask[currentMaskIndex + k + (h << 5)] != maskValue)
                        {
                            canGrowHeight = false;
                            break;
                        }
                    }
                    if (!canGrowHeight) break;
                }

                if ((vertexCounter + 4) < OutputVertices.Length || (vertexCounter + 4) == OutputVertices.Length)
                {
                    if ((indexCounter + 6) < OutputIndices.Length || (indexCounter + 6) == OutputIndices.Length)
                    {
                        EmitQuad(backCoord, direction, u, v, i, j, w, h, colorIndex, ref vertexCounter, ref indexCounter);
                    }
                }

                for (int l = 0; l < h; l++)
                {
                    for (int k = 0; k < w; k++)
                    {
                        sliceMask[currentMaskIndex + k + (l << 5)] = 0;
                    }
                }

                i += w - 1;
            }
        }
    }

    [BurstCompile]
    private bool IsVoxelLive(int3 pos, out byte color)
    {
        color = 0;
        if (pos.x < 0 || pos.x > 31 || pos.y < 0 || pos.y > 31 || pos.z < 0 || pos.z > 31)
        {
            return false;
        }

        int flatIndex = pos.x + (pos.y << 5) + (pos.z << 10);

        int ulongIndex = flatIndex >> 6;
        int bitOffset = flatIndex & 63;
        bool isVoxelNotDestroyed = (LiveMask[ulongIndex].Value & (1UL << bitOffset)) != 0;
        if (!isVoxelNotDestroyed) return false;

        int targetColorIndex = ChunkOffsetInFlattenedArray + flatIndex;
        if (targetColorIndex < 0 || targetColorIndex > FlattenedModelColors.Length || targetColorIndex == FlattenedModelColors.Length) return false;

        color = FlattenedModelColors[targetColorIndex];
        return color > 0;
    }

    [BurstCompile]
    private void EmitQuad(int backCoord, int d, int u, int v, int i, int j, int w, int h, byte colorIndex, ref int vCount, ref int iCount)
    {
        Color32 realColor = GlobalPaletteColors[colorIndex];
        int backAxis = d / 2;

        // Координата слоя строго равна текущей координате среза цикла маски кадра!
        float renderX = (float)backCoord;

        float3 p0 = float3.zero; float3 p1 = float3.zero; float3 p2 = float3.zero; float3 p3 = float3.zero;

        switch (backAxis)
        {
            case 0: // Ось X (Грани Слева / Справа)
                p0 = new float3(renderX, (float)i, (float)j);
                p1 = new float3(renderX, (float)i, (float)(j + h));
                p2 = new float3(renderX, (float)(i + w), (float)(j + h));
                p3 = new float3(renderX, (float)(i + w), (float)j);
                break;

            case 1: // Ось Y (Грани Снизу / Сверху)
                p0 = new float3((float)j, renderX, (float)i);
                p1 = new float3((float)j, renderX, (float)(i + w));
                p2 = new float3((float)(j + h), renderX, (float)(i + w));
                p3 = new float3((float)(j + h), renderX, (float)i);
                break;

            case 2: // Ось Z (Грани Сзади / Спереди)
                p0 = new float3((float)i, (float)j, renderX);
                p1 = new float3((float)i, (float)(j + h), renderX);
                p2 = new float3((float)(i + w), (float)(j + h), renderX);
                p3 = new float3((float)(i + w), (float)j, renderX);
                break;
        }

        OutputVertices[vCount + 0] = new VoxelVertex { Position = p0, VertexColor = realColor };
        OutputVertices[vCount + 1] = new VoxelVertex { Position = p1, VertexColor = realColor };
        OutputVertices[vCount + 2] = new VoxelVertex { Position = p2, VertexColor = realColor };
        OutputVertices[vCount + 3] = new VoxelVertex { Position = p3, VertexColor = realColor };

        if (d % 2 == 0) // Лицевые грани
        {
            OutputIndices[iCount + 0] = vCount + 0; OutputIndices[iCount + 1] = vCount + 1; OutputIndices[iCount + 2] = vCount + 2;
            OutputIndices[iCount + 3] = vCount + 0; OutputIndices[iCount + 4] = vCount + 2; OutputIndices[iCount + 5] = vCount + 3;
        }
        else // Обратные грани
        {
            OutputIndices[iCount + 0] = vCount + 0; OutputIndices[iCount + 1] = vCount + 2; OutputIndices[iCount + 2] = vCount + 1;
            OutputIndices[iCount + 3] = vCount + 0; OutputIndices[iCount + 4] = vCount + 3; OutputIndices[iCount + 5] = vCount + 2;
        }

        vCount += 4;
        iCount += 6;
        JobCountersRef[0] = new int2(vCount, iCount);
    }
}


//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;

//[BurstCompile]
//public struct MeshGreedyJobSafeDirect : IJob
//{
//    [ReadOnly] public NativeArray<LocalChunkDestructionMask> LiveMask;
//    [ReadOnly] public NativeArray<byte> FlattenedModelColors;
//    [ReadOnly] public NativeArray<Color32> GlobalPaletteColors;
//    public int ChunkOffsetInFlattenedArray;

//    // ====================================================================
//    // АБСОЛЮТНО БЕЗОПАСНЫЙ ПАЙПЛАЙН (БЕЗ АЛИАСИНГА И КОСТЫЛЕЙ БЕЗОПАСНОСТИ)
//    // Память выделится в системе, а Unity сама уничтожит её на C++ уровне
//    // строго в момент окончания работы этой джобы в фоновом потоке!
//    // ====================================================================
//    public NativeArray<VoxelVertex> OutputVertices;
//    public NativeArray<int> OutputIndices;

//    // ====================================================================
//    // ЧИСТЫЙ SAFE-ФИКС: Убираем [DeallocateOnJobCompletion] отсюда!
//    // Теперь джоба запишет данные, но массив останется жить, пока мы его не прочитаем.
//    // ====================================================================
//    public NativeArray<int2> JobCountersRef;
//    // ====================================================================
//    public int JobIndex;

//    [BurstCompile]
//    public void Execute()
//    {
//        NativeArray<short> mask = new NativeArray<short>(1024, Allocator.Temp);
//        int vertexCounter = 0;
//        int indexCounter = 0;

//        for (int back = 0; back < 3; back++)
//        {
//            int u = (back + 1) % 3;
//            int v = (back + 2) % 3;
//            int3 chunkPos = int3.zero;
//            int3 axisVector = int3.zero;
//            axisVector[back] = 1;

//            for (chunkPos[back] = -1; chunkPos[back] < 32; chunkPos[back]++)
//            {
//                int n = 0;
//                // ... [Линейная сборка маски среза Шага А] ...
//                for (chunkPos[v] = 0; chunkPos[v] < 32; chunkPos[v]++)
//                {
//                    for (chunkPos[u] = 0; chunkPos[u] < 32; chunkPos[u]++)
//                    {
//                        bool voxelCurrentLive = IsVoxelLive(chunkPos, out byte colorCurrent);
//                        bool voxelNeighborLive = IsVoxelLive(chunkPos + axisVector, out byte colorNeighbor);
//                        if (voxelCurrentLive == voxelNeighborLive) mask[n++] = 0;
//                        else if (voxelCurrentLive) mask[n++] = (short)(colorCurrent | ((back * 2) << 8));
//                        else mask[n++] = (short)(colorNeighbor | (((back * 2) + 1) << 8));
//                    }
//                }

//                // Шаг Б: Сшивание квадов по зафиксированным координатам
//                int currentN = 0;
//                for (int j = 0; j < 32; j++)
//                {
//                    for (int i = 0; i < 32; i++)
//                    {
//                        int currentMaskIndex = i + (j << 5);
//                        short maskValue = mask[currentMaskIndex];
//                        if (maskValue == 0) { currentN++; continue; }

//                        int direction = maskValue >> 8;
//                        byte colorIndex = (byte)(maskValue & 0xFF);

//                        int w; for (w = 1; i + w < 32; w++) if (mask[currentMaskIndex + w] != maskValue) break;
//                        int h; bool canGrowHeight = true;
//                        for (h = 1; j + h < 32; h++)
//                        {
//                            for (int k = 0; k < w; k++) if (mask[currentMaskIndex + k + (h << 5)] != maskValue) { canGrowHeight = false; break; }
//                            if (!canGrowHeight) break;
//                        }

//                        if (vertexCounter + 4 <= OutputVertices.Length && indexCounter + 6 <= OutputIndices.Length)
//                        {
//                            EmitQuad(chunkPos[back], direction, u, v, i, j, w, h, colorIndex, ref vertexCounter, ref indexCounter);
//                        }

//                        for (int l = 0; l < h; l++) for (int k = 0; k < w; k++) mask[currentMaskIndex + k + (l << 5)] = 0;
//                        i += w - 1;
//                    }
//                }
//            }
//        }
//        mask.Dispose();
//    }

//    [BurstCompile]
//    private bool IsVoxelLive(int3 pos, out byte color)
//    {
//        color = 0;
//        if (pos.x < 0 || pos.x > 31 || pos.y < 0 || pos.y > 31 || pos.z < 0 || pos.z > 31) return false;

//        int flatIndex = pos.x + (pos.y << 5) + (pos.z << 10);
//        int ulongIndex = flatIndex >> 6;
//        int bitOffset = flatIndex & 63;
//        bool isVoxelNotDestroyed = (LiveMask[ulongIndex].Value & (1UL << bitOffset)) != 0;
//        if (!isVoxelNotDestroyed) return false;

//        int targetColorIndex = ChunkOffsetInFlattenedArray + flatIndex;
//        if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) return false;

//        color = FlattenedModelColors[targetColorIndex];
//        return color > 0;
//    }

//    [BurstCompile]
//    private void EmitQuad(int backCoord, int d, int u, int v, int i, int j, int w, int h, byte colorIndex, ref int vCount, ref int iCount)
//    {
//        Color32 realColor = GlobalPaletteColors[colorIndex];
//        int offset = (d % 2 == 1) ? 1 : 0;
//        float renderX = (float)(backCoord + offset);
//        int backAxis = d / 2;

//        float3 p0 = float3.zero; float3 p1 = float3.zero; float3 p2 = float3.zero; float3 p3 = float3.zero;
//        p0[backAxis] = renderX; p1[backAxis] = renderX; p2[backAxis] = renderX; p3[backAxis] = renderX;

//        switch (backAxis)
//        {
//            case 0:
//                p0 = new float3(renderX, (float)i, (float)j); p1 = new float3(renderX, (float)i, (float)(j + h));
//                p2 = new float3(renderX, (float)(i + w), (float)(j + h)); p3 = new float3(renderX, (float)(i + w), (float)j);
//                break;
//            case 1:
//                p0 = new float3((float)j, renderX, (float)i); p1 = new float3((float)j, renderX, (float)(i + w));
//                p2 = new float3((float)(j + h), renderX, (float)(i + w)); p3 = new float3((float)(j + h), renderX, (float)i);
//                break;
//            case 2:
//                p0 = new float3((float)i, (float)j, renderX); p1 = new float3((float)i, (float)(j + h), renderX);
//                p2 = new float3((float)(i + w), (float)(j + h), renderX); p3 = new float3((float)(i + w), (float)j, renderX);
//                break;
//        }

//        OutputVertices[vCount + 0] = new VoxelVertex { Position = p0, VertexColor = realColor };
//        OutputVertices[vCount + 1] = new VoxelVertex { Position = p1, VertexColor = realColor };
//        OutputVertices[vCount + 2] = new VoxelVertex { Position = p2, VertexColor = realColor };
//        OutputVertices[vCount + 3] = new VoxelVertex { Position = p3, VertexColor = realColor };

//        if (d % 2 == 0)
//        {
//            OutputIndices[iCount + 0] = vCount + 0; OutputIndices[iCount + 1] = vCount + 1; OutputIndices[iCount + 2] = vCount + 2;
//            OutputIndices[iCount + 3] = vCount + 0; OutputIndices[iCount + 4] = vCount + 2; OutputIndices[iCount + 5] = vCount + 3;
//        }
//        else
//        {
//            OutputIndices[iCount + 0] = vCount + 0; OutputIndices[iCount + 1] = vCount + 2; OutputIndices[iCount + 2] = vCount + 1;
//            OutputIndices[iCount + 3] = vCount + 0; OutputIndices[iCount + 4] = vCount + 3; OutputIndices[iCount + 5] = vCount + 2;
//        }

//        vCount += 4; iCount += 6;
//        JobCountersRef[0] = new int2(vCount, iCount);
//    }
//}
