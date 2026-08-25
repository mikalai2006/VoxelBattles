using Mikalai2006.VoxelBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
public struct GreedyMeshJob : IJob
{
    [ReadOnly] public NativeArray<Voxel> ArrayVoxels;
    [ReadOnly] public NativeArray<VoxelColors> ArrayVoxelColors;
    public int3 Size; // Использование int3 вместо Vector3Int ускоряет Burst
    public float3 Offset; // Использование float3 вместо Vector3

    // Отключаем проверки безопасности для прямой и быстрой записи по индексам
    [NativeDisableContainerSafetyRestriction] public NativeList<Vector3> Vertices;
    [NativeDisableContainerSafetyRestriction] public NativeList<Color32> Colors;
    [NativeDisableContainerSafetyRestriction] public NativeList<int> OpaqueTriangles;
    [NativeDisableContainerSafetyRestriction] public NativeList<int> TransparentTriangles;

    public void Execute()
    {
        int totalCells = Size.x * Size.y * Size.z;
        NativeArray<uint> volumeGrid = new NativeArray<uint>(totalCells, Allocator.Temp, NativeArrayOptions.ClearMemory);

        // Быстрое заполнение сетки вокселей
        for (int i = 0; i < ArrayVoxels.Length; i++)
        {
            Voxel voxel = ArrayVoxels[i];
            if (voxel.type == VoxelType.Destroyed) continue;

            Vector3Int pos = voxel.position;
            if (pos.x >= 0 && pos.x < Size.x && pos.y >= 0 && pos.y < Size.y && pos.z >= 0 && pos.z < Size.z)
            {
                Color32 c = voxel.color;
                uint packedColor = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
                // Оптимизированный инлайн-индекс без вызова тяжелых функций
                volumeGrid[pos.x + Size.x * (pos.y + Size.y * pos.z)] = packedColor;
            }
        }

        int maxSliceSize = math.max(Size.x * Size.y, math.max(Size.y * Size.z, Size.x * Size.z));
        NativeArray<uint2> mask = new NativeArray<uint2>(maxSliceSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // Локальные указатели для записи в списки
        int vIdx = Vertices.Length;
        int opqTriIdx = OpaqueTriangles.Length;
        int transTriIdx = TransparentTriangles.Length;

        // Предварительно резервируем память с запасом, исключая Resize() внутри циклов
        int estimatedQuads = maxSliceSize * 3;
        Vertices.Resize(vIdx + estimatedQuads * 4, NativeArrayOptions.UninitializedMemory);
        Colors.Resize(vIdx + estimatedQuads * 4, NativeArrayOptions.UninitializedMemory);
        OpaqueTriangles.Resize(opqTriIdx + estimatedQuads * 6, NativeArrayOptions.UninitializedMemory);
        TransparentTriangles.Resize(transTriIdx + estimatedQuads * 6, NativeArrayOptions.UninitializedMemory);

        int sizeX = Size.x;
        int sizeY = Size.y;
        // Продолжение метода Execute...
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3; int v = (d + 2) % 3;
            int3 x = int3.zero; int3 q = int3.zero; q[d] = 1;

            int sizeU = Size[u]; int sizeV = Size[v]; int sizeD = Size[d];

            for (x[d] = -1; x[d] < sizeD;)
            {
                int n = 0;
                for (x[v] = 0; x[v] < sizeV; ++x[v])
                {
                    for (x[u] = 0; x[u] < sizeU; ++x[u], ++n)
                    {
                        uint colorCurrent = (0 <= x[d]) ? volumeGrid[x.x + sizeX * (x.y + sizeY * x.z)] : 0;
                        int3 nextCoord = x + q;
                        uint colorNext = (x[d] < sizeD - 1) ? volumeGrid[nextCoord.x + sizeX * (nextCoord.y + sizeY * nextCoord.z)] : 0;

                        uint alphaCurrent = colorCurrent & 0xFF;
                        uint alphaNext = colorNext & 0xFF;

                        if (colorCurrent != 0 && colorNext != 0 && alphaCurrent == alphaNext)
                            mask[n] = uint2.zero;
                        else if (colorCurrent != 0)
                            mask[n] = new uint2(colorCurrent, 1);
                        else if (colorNext != 0)
                            mask[n] = new uint2(colorNext, 2);
                        else
                            mask[n] = uint2.zero;
                    }
                }

                x[d]++; n = 0;

                for (int j = 0; j < sizeV; ++j)
                {
                    for (int i = 0; i < sizeU;)
                    {
                        uint2 maskValue = mask[n];
                        if (maskValue.y != 0)
                        {
                            bool isBackFace = maskValue.y == 2;
                            uint packedColor = maskValue.x;

                            int w;
                            for (w = 1; i + w < sizeU && mask[n + w].x == packedColor && mask[n + w].y == maskValue.y; ++w) { }

                            int h; bool done = false;
                            for (h = 1; j + h < sizeV; ++h)
                            {
                                int rowOffset = h * sizeU;
                                for (int k = 0; k < w; ++k)
                                {
                                    if (mask[n + k + rowOffset].x != packedColor || mask[n + k + rowOffset].y != maskValue.y)
                                    {
                                        done = true; break;
                                    }
                                }
                                if (done) break;
                            }

                            x[u] = i; x[v] = j;
                            int3 du = int3.zero; du[u] = w;
                            int3 dv = int3.zero; dv[v] = h;

                            if (vIdx + 4 > Vertices.Length)
                            {
                                int newSize = Vertices.Length * 2;
                                Vertices.Resize(newSize, NativeArrayOptions.UninitializedMemory);
                                Colors.Resize(newSize, NativeArrayOptions.UninitializedMemory);
                            }

                            // Прямая высокоскоростная запись по индексам
                            Vertices[vIdx] = new Vector3(x.x, x.y, x.z) + (Vector3)Offset;
                            Vertices[vIdx + 1] = new Vector3(x.x + du.x, x.y + du.y, x.z + du.z) + (Vector3)Offset;
                            Vertices[vIdx + 2] = new Vector3(x.x + du.x + dv.x, x.y + du.y + dv.y, x.z + du.z + dv.z) + (Vector3)Offset;
                            Vertices[vIdx + 3] = new Vector3(x.x + dv.x, x.y + dv.y, x.z + dv.z) + (Vector3)Offset;

                            Color32 voxelColor = new Color32(
                                (byte)((packedColor >> 24) & 0xFF), (byte)((packedColor >> 16) & 0xFF),
                                (byte)((packedColor >> 8) & 0xFF), (byte)(packedColor & 0xFF)
                            );

                            Colors[vIdx] = Colors[vIdx + 1] = Colors[vIdx + 2] = Colors[vIdx + 3] = voxelColor;

                            if (voxelColor.a < 255)
                            {
                                if (transTriIdx + 6 > TransparentTriangles.Length)
                                    TransparentTriangles.Resize(TransparentTriangles.Length * 2, NativeArrayOptions.UninitializedMemory);

                                TransparentTriangles[transTriIdx] = vIdx; TransparentTriangles[transTriIdx + 1] = isBackFace ? vIdx + 2 : vIdx + 1; TransparentTriangles[transTriIdx + 2] = isBackFace ? vIdx + 1 : vIdx + 2;
                                TransparentTriangles[transTriIdx + 3] = vIdx; TransparentTriangles[transTriIdx + 4] = isBackFace ? vIdx + 3 : vIdx + 2; TransparentTriangles[transTriIdx + 5] = isBackFace ? vIdx + 2 : vIdx + 3;
                                transTriIdx += 6;
                            }
                            else
                            {
                                if (opqTriIdx + 6 > OpaqueTriangles.Length)
                                    OpaqueTriangles.Resize(OpaqueTriangles.Length * 2, NativeArrayOptions.UninitializedMemory);

                                OpaqueTriangles[opqTriIdx] = vIdx; OpaqueTriangles[opqTriIdx + 1] = isBackFace ? vIdx + 2 : vIdx + 1; OpaqueTriangles[opqTriIdx + 2] = isBackFace ? vIdx + 1 : vIdx + 2;
                                OpaqueTriangles[opqTriIdx + 3] = vIdx; OpaqueTriangles[opqTriIdx + 4] = isBackFace ? vIdx + 3 : vIdx + 2; OpaqueTriangles[opqTriIdx + 5] = isBackFace ? vIdx + 2 : vIdx + 3;
                                opqTriIdx += 6;
                            }

                            vIdx += 4;

                            for (int l = 0; l < h; ++l)
                            {
                                int targetRow = n + l * sizeU;
                                for (int k = 0; k < w; ++k)
                                    mask[targetRow + k] = uint2.zero;
                            }

                            i += w; n += w;
                        }
                        else { i++; n++; }
                    }
                }
            }
        }

        // Обрезаем лишний зарезервированный хвост под фактический размер меша
        Vertices.Resize(vIdx, NativeArrayOptions.UninitializedMemory);
        Colors.Resize(vIdx, NativeArrayOptions.UninitializedMemory);
        OpaqueTriangles.Resize(opqTriIdx, NativeArrayOptions.UninitializedMemory);
        TransparentTriangles.Resize(transTriIdx, NativeArrayOptions.UninitializedMemory);

        mask.Dispose();
        volumeGrid.Dispose();
    }
}


//using Mikalai2006.VoxelBase;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;

//[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
//public struct GreedyMeshJob : IJob
//{
//    [ReadOnly] public NativeArray<Voxel> ArrayVoxels;
//    [ReadOnly] public NativeArray<VoxelColors> ArrayVoxelColors;
//    public Vector3Int Size;
//    public Vector3 Offset;

//    public NativeList<Vector3> Vertices;
//    public NativeList<Color32> Colors;
//    public NativeList<int> OpaqueTriangles;
//    public NativeList<int> TransparentTriangles;

//    private int GetIndex(int x, int y, int z) => x + Size.x * (y + Size.y * z);

//    public void Execute()
//    {
//        int totalCells = Size.x * Size.y * Size.z;
//        NativeArray<uint> volumeGrid = new NativeArray<uint>(totalCells, Allocator.Temp);

//        for (int i = 0; i < ArrayVoxels.Length; i++)
//        {
//            if (ArrayVoxels[i].type == VoxelType.Destroyed) continue;

//            Vector3Int pos = ArrayVoxels[i].position;
//            if (pos.x >= 0 && pos.x < Size.x && pos.y >= 0 && pos.y < Size.y && pos.z >= 0 && pos.z < Size.z)
//            {
//                Color32 c = ArrayVoxels[i].color;
//                // ИСПРАВЛЕНО: Теперь в конце строго c.a, данные не затираются
//                uint packedColor = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | (uint)c.a;
//                volumeGrid[GetIndex(pos.x, pos.y, pos.z)] = packedColor;
//            }
//        }

//        int maxSliceSize = Mathf.Max(Size.x * Size.y, Mathf.Max(Size.y * Size.z, Size.x * Size.z));
//        // Маска типа uint2: x = цвет, y = тип грани (0: нет, 1: наружу, 2: внутрь)
//        NativeArray<uint2> mask = new NativeArray<uint2>(maxSliceSize, Allocator.Temp);

//        for (int d = 0; d < 3; d++)
//        {
//            int u = (d + 1) % 3; int v = (d + 2) % 3;
//            int3 x = int3.zero; int3 q = int3.zero; q[d] = 1;

//            for (x[d] = -1; x[d] < Size[d];)
//            {
//                int n = 0;
//                for (x[v] = 0; x[v] < Size[v]; ++x[v])
//                {
//                    for (x[u] = 0; x[u] < Size[u]; ++x[u], ++n)
//                    {
//                        uint colorCurrent = (0 <= x[d]) ? volumeGrid[GetIndex(x.x, x.y, x.z)] : 0;
//                        int3 nextCoord = x + q;
//                        uint colorNext = (x[d] < Size[d] - 1) ? volumeGrid[GetIndex(nextCoord.x, nextCoord.y, nextCoord.z)] : 0;

//                        uint alphaCurrent = colorCurrent & 0xFF;
//                        uint alphaNext = colorNext & 0xFF;

//                        if (colorCurrent != 0 && colorNext != 0 && alphaCurrent == alphaNext)
//                        {
//                            mask[n] = uint2.zero; // Внутренняя невидимая грань
//                        }
//                        else if (colorCurrent != 0)
//                        {
//                            mask[n] = new uint2(colorCurrent, 1); // Грань наружу
//                        }
//                        else if (colorNext != 0)
//                        {
//                            mask[n] = new uint2(colorNext, 2); // Грань внутрь
//                        }
//                        else
//                        {
//                            mask[n] = uint2.zero;
//                        }
//                    }
//                }

//                x[d]++; n = 0;

//                for (int j = 0; j < Size[v]; ++j)
//                {
//                    for (int i = 0; i < Size[u];)
//                    {
//                        uint2 maskValue = mask[n];
//                        if (maskValue.y != 0)
//                        {
//                            bool isBackFace = maskValue.y == 2;
//                            uint packedColor = maskValue.x;

//                            int w;
//                            for (w = 1; i + w < Size[u] && mask[n + w].x == maskValue.x && mask[n + w].y == maskValue.y; ++w) { }

//                            int h; bool done = false;
//                            for (h = 1; j + h < Size[v]; ++h)
//                            {
//                                for (int k = 0; k < w; ++k)
//                                {
//                                    int checkIdx = n + k + h * Size[u];
//                                    if (mask[checkIdx].x != maskValue.x || mask[checkIdx].y != maskValue.y)
//                                    {
//                                        done = true; break;
//                                    }
//                                }
//                                if (done) break;
//                            }

//                            x[u] = i; x[v] = j;
//                            int3 du = int3.zero; du[u] = w;
//                            int3 dv = int3.zero; dv[v] = h;

//                            int vCount = Vertices.Length;
//                            Vertices.Add(new Vector3(x.x, x.y, x.z) + Offset);
//                            Vertices.Add(new Vector3(x.x + du.x, x.y + du.y, x.z + du.z) + Offset);
//                            Vertices.Add(new Vector3(x.x + du.x + dv.x, x.y + du.y + dv.y, x.z + du.z + dv.z) + Offset);
//                            Vertices.Add(new Vector3(x.x + dv.x, x.y + dv.y, x.z + dv.z) + Offset);

//                            // ИСПРАВЛЕНО: Распаковка теперь восстанавливает честную альфу
//                            Color32 voxelColor = new Color32(
//                                (byte)((packedColor >> 24) & 0xFF),
//                                (byte)((packedColor >> 16) & 0xFF),
//                                (byte)((packedColor >> 8) & 0xFF),
//                                (byte)(packedColor & 0xFF)
//                            );

//                            Colors.Add(voxelColor); Colors.Add(voxelColor);
//                            Colors.Add(voxelColor); Colors.Add(voxelColor);

//                            bool isTransparent = voxelColor.a < 255;

//                            if (isTransparent)
//                            {
//                                if (isBackFace)
//                                {
//                                    TransparentTriangles.Add(vCount); TransparentTriangles.Add(vCount + 2); TransparentTriangles.Add(vCount + 1);
//                                    TransparentTriangles.Add(vCount); TransparentTriangles.Add(vCount + 3); TransparentTriangles.Add(vCount + 2);
//                                }
//                                else
//                                {
//                                    TransparentTriangles.Add(vCount); TransparentTriangles.Add(vCount + 1); TransparentTriangles.Add(vCount + 2);
//                                    TransparentTriangles.Add(vCount); TransparentTriangles.Add(vCount + 2); TransparentTriangles.Add(vCount + 3);
//                                }
//                            }
//                            else
//                            {
//                                if (isBackFace)
//                                {
//                                    OpaqueTriangles.Add(vCount); OpaqueTriangles.Add(vCount + 2); OpaqueTriangles.Add(vCount + 1);
//                                    OpaqueTriangles.Add(vCount); OpaqueTriangles.Add(vCount + 3); OpaqueTriangles.Add(vCount + 2);
//                                }
//                                else
//                                {
//                                    OpaqueTriangles.Add(vCount); OpaqueTriangles.Add(vCount + 1); OpaqueTriangles.Add(vCount + 2);
//                                    OpaqueTriangles.Add(vCount); OpaqueTriangles.Add(vCount + 2); OpaqueTriangles.Add(vCount + 3);
//                                }
//                            }

//                            for (int l = 0; l < h; ++l)
//                                for (int k = 0; k < w; ++k)
//                                    mask[n + k + l * Size[u]] = uint2.zero;

//                            i += w; n += w;
//                        }
//                        else { i++; n++; }
//                    }
//                }
//            }
//        }
//        mask.Dispose();
//        volumeGrid.Dispose();
//    }
//}
