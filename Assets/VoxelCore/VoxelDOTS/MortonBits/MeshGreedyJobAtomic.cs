//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;

////[StructLayout(LayoutKind.Sequential, Pack = 1)]
////public struct VoxelVertex
////{
////    // Честные float-метры для GPU (выровненные по 16 байт вместе с цветом)
////    public float3 Position;
////    public Color32 VertexColor;
////}

//[BurstCompile]
//public struct MeshGreedyJobAtomic : IJob
//{
//    [ReadOnly] public NativeArray<LocalChunkDestructionMask> LiveMask;
//    [ReadOnly] public NativeArray<byte> FlattenedModelColors;
//    [ReadOnly] public NativeArray<Color32> GlobalPaletteColors;
//    public int ChunkOffsetInFlattenedArray;

//    public NativeList<VoxelVertex> OutputVertices;
//    public NativeList<int> OutputIndices;

//    [BurstCompile]
//    public void Execute()
//    {
//        // Временная маска среза 32x32 на unmanaged-стеке потока
//        NativeArray<short> mask = new NativeArray<short>(1024, Allocator.Temp);

//        // ====================================================================
//        // СИНХРОНИЗИРОВАННЫЙ ТРЕХОСЕВОЙ ЦИКЛ (БЕЗ МУТАЦИИ ОБЩИХ ВЕКТОРОВ)
//        // ====================================================================
//        for (int back = 0; back < 3; back++)
//        {
//            int u = (back + 1) % 3;
//            int v = (back + 2) % 3;

//            int3 axisVector = int3.zero;
//            axisVector[back] = 1;

//            // Двигаемся по глубине среза
//            for (int backCoord = -1; backCoord < 32; backCoord++)
//            {
//                int n = 0;

//                // ----------------------------------------------------------------
//                // ШАГ A: СБОРКА МАСКИ ИЗ ЛОКАЛЬНЫХ ИЗОЛИРОВАННЫХ КООРДИНАТ
//                // ----------------------------------------------------------------
//                for (int vCoord = 0; vCoord < 32; vCoord++)
//                {
//                    for (int uCoord = 0; uCoord < 32; uCoord++)
//                    {
//                        // Собираем точную 3D-позицию текущего вокселя строго на стеке
//                        int3 currentPos = int3.zero;
//                        currentPos[back] = backCoord;
//                        currentPos[u] = uCoord;
//                        currentPos[v] = vCoord;

//                        // Читаем текущий воксель и его соседа вперед
//                        bool voxelCurrentLive = IsVoxelLive(currentPos, out byte colorCurrent);
//                        bool voxelNeighborLive = IsVoxelLive(currentPos + axisVector, out byte colorNeighbor);

//                        if (voxelCurrentLive == voxelNeighborLive)
//                        {
//                            mask[n++] = 0;
//                        }
//                        else if (voxelCurrentLive)
//                        {
//                            mask[n++] = (short)(colorCurrent | ((back * 2) << 8)); // Лицевая
//                        }
//                        else
//                        {
//                            mask[n++] = (short)(colorNeighbor | (((back * 2) + 1) << 8)); // Обратная
//                        }
//                    }
//                }

//                // ----------------------------------------------------------------
//                // ШАГ Б: МАТЕМАТИЧЕСКИ СТРОГОЕ ЖАДНОЕ СЛИЯНИЕ (БЕЗ СДВИГА ИНДЕКСА n)
//                // ----------------------------------------------------------------
//                // Убираем n++ из объявления цикла, будем считать его жестко внутри!
//                for (int j = 0; j < 32; j++)
//                {
//                    for (int i = 0; i < 32; i++)
//                    {
//                        // Вычисляем линейный индекс на плоскости маски строго от координат
//                        int currentMaskIndex = i + (j << 5); // i + j * 32

//                        short maskValue = mask[currentMaskIndex];
//                        if (maskValue == 0) continue; // Пропускаем воздух/пустоту

//                        int direction = maskValue >> 8;
//                        byte colorIndex = (byte)(maskValue & 0xFF);

//                        // 1. Рассчитываем ширину (w)
//                        int w;
//                        for (w = 1; i + w < 32; w++)
//                        {
//                            if (mask[currentMaskIndex + w] != maskValue) break;
//                        }

//                        // 2. Рассчитываем высоту (h)
//                        int h;
//                        bool canGrowHeight = true;
//                        for (h = 1; j + h < 32; h++)
//                        {
//                            for (int k = 0; k < w; k++)
//                            {
//                                if (mask[currentMaskIndex + k + (h << 5)] != maskValue)
//                                {
//                                    canGrowHeight = false;
//                                    break;
//                                }
//                            }
//                            if (!canGrowHeight) break;
//                        }

//                        // 3. Передаем точные параметры в EmitQuad
//                        EmitQuad(backCoord, direction, u, v, i, j, w, h, colorIndex);

//                        // 4. Очищаем маску строго по вычисленному индексу
//                        for (int l = 0; l < h; l++)
//                        {
//                            for (int k = 0; k < w; k++)
//                            {
//                                mask[currentMaskIndex + k + (l << 5)] = 0;
//                            }
//                        }

//                        // Смещаем координату цикла по ширине
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

//        // ====================================================================
//        // ЖЕЛЕЗОБЕТОННЫЙ ФИКС БАХРОМЫ: ЖЕСТКОЕ ОТСЕЧЕНИЕ ДО ПОБИТОВЫХ СДВИГОВ
//        // Если координата равна -1 или 32, мы МГНОВЕННО возвращаем false (воздух),
//        // не позволяя процессору сломать flatIndex и прочитать мусор!
//        // ====================================================================
//        if (pos.x < 0 || pos.x > 31 || pos.y < 0 || pos.y > 31 || pos.z < 0 || pos.z > 31)
//        {
//            return false;
//        }
//        // ====================================================================

//        // Только теперь, когда координаты в полной безопасности, считаем индекс
//        int flatIndex = pos.x + (pos.y << 5) + (pos.z << 10);

//        // Проверка нативной маски разрушений Netcode
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
//    private void EmitQuad(int backCoord, int d, int u, int v, int i, int j, int w, int h, byte colorIndex)
//    {
//        // Безопасное извлечение Color32 из локальной палитры модели
//        Color32 realColor = new Color32(255, 255, 255, 255);
//        if (colorIndex < GlobalPaletteColors.Length)
//        {
//            realColor = GlobalPaletteColors[colorIndex];
//        }

//        // Послойный сдвиг для соседа вперед
//        int offset = (d % 2 == 1) ? 1 : 0;
//        float renderX = (float)(backCoord + offset);

//        int backAxis = d / 2;

//        float3 p0 = float3.zero; float3 p1 = float3.zero; float3 p2 = float3.zero; float3 p3 = float3.zero;

//        // ====================================================================
//        // МАТЕМАТИЧЕСКИЙ switch ПО ФИЗИЧЕСКИМ ОСЯМ ДЛЯ ЛИНЕЙНОГО МАССИВА
//        // Синхронизирует u/v плоскость с реальными пространственными координатами Unity.
//        // Полностью схлопывает слои и убирает «эффект взорванной схемы».
//        // ====================================================================
//        switch (backAxis)
//        {
//            case 0: // Сканируем вдоль X (Грани Слева/Справа). i -> Y, j -> Z
//                p0 = new float3(renderX, (float)i, (float)j);
//                p1 = new float3(renderX, (float)i, (float)(j + h));
//                p2 = new float3(renderX, (float)(i + w), (float)(j + h));
//                p3 = new float3(renderX, (float)(i + w), (float)j);
//                break;

//            case 1: // Сканируем вдоль Y (Грани Снизу/Сверху). i -> Z, j -> X
//                p0 = new float3((float)j, renderX, (float)i);
//                p1 = new float3((float)j, renderX, (float)(i + w));
//                p2 = new float3((float)(j + h), renderX, (float)(i + w));
//                p3 = new float3((float)(j + h), renderX, (float)i);
//                break;

//            case 2: // Сканируем вдоль Z (Грани Сзади/Спереди). i -> X, j -> Y
//                p0 = new float3((float)i, (float)j, renderX);
//                p1 = new float3((float)i, (float)(j + h), renderX);
//                p2 = new float3((float)(i + w), (float)(j + h), renderX);
//                p3 = new float3((float)(i + w), (float)j, renderX);
//                break;
//        }
//        // ====================================================================

//        OutputVertices.Add(new VoxelVertex { Position = p0, VertexColor = realColor });
//        OutputVertices.Add(new VoxelVertex { Position = p1, VertexColor = realColor });
//        OutputVertices.Add(new VoxelVertex { Position = p2, VertexColor = realColor });
//        OutputVertices.Add(new VoxelVertex { Position = p3, VertexColor = realColor });

//        int vIndex = OutputVertices.Length - 4;

//        // Симметричный Culling для лицевых и обратных сторон кузова
//        if (d % 2 == 0) // Лицевые (0, 2, 4)
//        {
//            OutputIndices.Add(vIndex + 0); OutputIndices.Add(vIndex + 1); OutputIndices.Add(vIndex + 2);
//            OutputIndices.Add(vIndex + 0); OutputIndices.Add(vIndex + 2); OutputIndices.Add(vIndex + 3);
//        }
//        else // Обратные (1, 3, 5)
//        {
//            OutputIndices.Add(vIndex + 0); OutputIndices.Add(vIndex + 2); OutputIndices.Add(vIndex + 1);
//            OutputIndices.Add(vIndex + 0); OutputIndices.Add(vIndex + 3); OutputIndices.Add(vIndex + 2);
//        }
//    }
//}
