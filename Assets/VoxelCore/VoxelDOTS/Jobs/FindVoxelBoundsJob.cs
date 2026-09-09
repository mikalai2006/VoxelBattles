using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile]
public struct FindVoxelModelBoundsJob : IJob
{
    // Габариты модели из ModelRuntimeTemplate
    //public int3 SizeModel;
    //[ReadOnly] public NativeParallelHashMap<int3, int> ChunkCoordToOrderIndexMap;
    [ReadOnly] public NativeArray<byte> FlattenedModelColors;

    // Плоский массив ВСЕХ масок машины (размер = Количество_Чанков * 512)
    [ReadOnly] public NativeArray<AAA_ChunkDestructionMask>.ReadOnly LiveMask;

    public NativeArray<int3> JobStatusRef;
    public float VoxelScale;
    public int ChunkOffsetInFlattenedArray;

    // Выходной контейнер для геометрии
    public NativeArray<BoxGeometry> OutputBoxGeometry;

    public void Execute()
    {
        // Инициализируем индексы слоев "вывернутыми" значениями
        int minX = 32, maxX = -1;
        int minY = 32, maxY = -1;
        int minZ = 32, maxZ = -1;

        int aliveCount = 0;

        // Послойный обход чанка по вашему каноническому индексу
        for (int z = 0; z < 32; z++)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int flatIndex = x + (y << 5) + (z << 10);

                    // 1. Проверяем, существовал ли воксель в этой точке ИЗНАЧАЛЬНО при запекании модели
                    int targetColorIndex = ChunkOffsetInFlattenedArray + flatIndex;
                    if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) continue;

                    byte bakedColor = FlattenedModelColors[targetColorIndex];
                    if (bakedColor == 0) continue; // Если тут изначально был воздух фабрики — этот индекс нас вообще не интересует, идем дальше!

                    // 2. Если воксель изначально БЫЛ, проверяем его ЖИВУЮ МАСКУ (не уничтожен ли он сейчас)
                    int ulongIndex = flatIndex >> 6;
                    int bitOffset = flatIndex & 63;
                    ulong maskValue = LiveMask[ulongIndex].Value;

                    bool isVoxelNotDestroyed = (maskValue & (1UL << bitOffset)) != 0;

                    // Если воксель изначально был в модели, но СЕЙЧАС его уничтожила пуля — ПРОПУСКАЕМ ЕГО!
                    if (!isVoxelNotDestroyed) continue;

                    // --- ВОТ ТЕПЕРЬ СЮДА ДОХОДЯТ ТОЛЬКО РЕАЛЬНО УЦЕЛЕВШИЕ ВОКСЕЛИ ---
                    aliveCount++;

                    // Фиксируем реальные индексы сжатия
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    if (z < minZ) minZ = z;
                    if (z > maxZ) maxZ = z;
                }
            }
        }

        if (aliveCount == 0)
        {
            OutputBoxGeometry[0] = new BoxGeometry { Size = float3.zero };
        }
        else
        {
            // Считаем чистый размер куба в количестве вокселей
            float3 voxelSize = new float3(
                (maxX - minX) + 1,
                (maxY - minY) + 1,
                (maxZ - minZ) + 1
            );

            // Считаем точный геометрический центр оставшихся кубиков
            float3 voxelCenter = new float3(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f,
                (minZ + maxZ) * 0.5f
            );

            // Собираем BoxGeometry с учетом масштаба (VoxelScale = 1)
            OutputBoxGeometry[0] = new BoxGeometry
            {
                Center = voxelCenter * VoxelScale, // Центр теперь будет СДВИГАТЬСЯ вслед за разрушением!
                Size = voxelSize * VoxelScale,     // Размер будет четко равен количеству вокселей
                Orientation = quaternion.identity,
                BevelRadius = 0.005f
            };
        }

        // Возвращаем реальный статус в систему
        JobStatusRef[0] = new int3(minZ, maxZ, 1);

        //float3 min = new float3(float.MaxValue);
        //float3 max = new float3(float.MinValue);
        //bool hasAnyVoxels = false;

        //const int chunkVolume = 32768; // 32^3 вокселей в одном чанке

        //// Проходим по всем 32^3 вокселям ВНУТРИ ОДНОГО этого чанка
        //for (int i = 0; i < chunkVolume; i++)
        //{
        //    // 1. Проверяем вашу битовую маску разрушений чанка
        //    int ulongIndex = i >> 6;      // Индекс ulong (0..511)
        //    int bitOffset = i & 63;       // Смещение бита (0..63)

        //    // Читаем маску (индекс ulongIndex строго от 0 до 511, никакой OutOfRange невозможен)
        //    ulong maskValue = LiveMask[ulongIndex].Value;

        //    bool isVoxelNotDestroyed = (maskValue & (1UL << bitOffset)) != 0;
        //    if (!isVoxelNotDestroyed) continue;

        //    // 2. Проверяем исходный цвет в запеченном массиве
        //    int targetColorIndex = ChunkOffsetInFlattenedArray + i;

        //    // Безопасная проверка выхода за границы плоского массива цветов
        //    if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) continue;

        //    byte color = FlattenedModelColors[targetColorIndex];
        //    if (color == 0) continue; // Воздух

        //    hasAnyVoxels = true;

        //    // 3. Восстанавливаем локальные координаты X, Y, Z вокселя внутри чанка (0..31)
        //    int lx = i & 31;
        //    int ly = (i >> 5) & 31;
        //    int lz = (i >> 10) & 31;

        //    // Позиция СТРОГО ОТНОСИТЕЛЬНО НУЛЯ ЧАНКА в метрах!
        //    // Никаких глобальных координат модели сюда не добавляем, чтобы убрать баг с двойным сдвигом
        //    float3 localVoxelPosInMeters = new float3(lx, ly, lz) * VoxelScale;

        //    min = math.min(min, localVoxelPosInMeters);
        //    max = math.max(max, localVoxelPosInMeters);
        //}

        //// Записываем итоговую геометрию для этого чанка
        //if (!hasAnyVoxels)
        //{
        //    OutputBoxGeometry[0] = new BoxGeometry { Size = float3.zero };
        //}
        //else
        //{
        //    // Корректируем min/max на половину вокселя для плотного облегания внешних граней
        //    float3 halfVoxel = new float3(VoxelScale * 0.5f);
        //    float3 finalMin = min - halfVoxel;
        //    float3 finalMax = max + halfVoxel;

        //    // Собираем чистую Box-геометрию в локальном пространстве чанка
        //    OutputBoxGeometry[0] = new BoxGeometry
        //    {
        //        Center = (finalMin + finalMax) * 0.5f,
        //        Size = finalMax - finalMin,
        //        Orientation = quaternion.identity,
        //        BevelRadius = 0.005f // Небольшой скос для плавной физики столкновений
        //    };
        //}

        //// возвращаем статусы / z = 1 - джоба завершена
        //JobStatusRef[0] = new int3(0, 0, 1);
    }
}


//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Physics;

//[BurstCompile]
//public struct FindVoxelModelBoundsJob : IJob
//{
//    // Входные данные структуры ModelRuntimeTemplate
//    public int3 SizeModel;
//    [ReadOnly] public NativeParallelHashMap<int3, int> ChunkCoordToOrderIndexMap;
//    [ReadOnly] public NativeArray<byte> FlattenedModelColors;

//    // Вся живая маска машины, полученная из DynamicBuffer<LocalChunkDestructionMask>.ToNativeArray
//    // Размер этого массива равен: Количество_Активных_Чанков * 512
//    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMasksBufferArray;

//    public NativeArray<int3> JobStatusRef;
//    public float VoxelScale;
//    public NativeArray<BoxGeometry> OutputBoxGeometry;

//    //// Выходные данные для BoxGeometry
//    //public NativeReference<float3> OutMinBounds;
//    //public NativeReference<float3> OutMaxBounds;

//    public void Execute()
//    {
//        float3 min = new float3(float.MaxValue);
//        float3 max = new float3(float.MinValue);
//        bool hasAnyVoxels = false;

//        const int chunkVolume = 32768; // 32^3
//        const int ulongsPerChunk = 512; // 32768 / 64 бит в одном ulong

//        for (int cz = 0; cz < SizeModel.z; cz++)
//        {
//            for (int cy = 0; cy < SizeModel.y; cy++)
//            {
//                for (int cx = 0; cx < SizeModel.x; cx++)
//                {
//                    int3 chunkCoord = new int3(cx, cy, cz);

//                    // 1. Проверяем, существует ли чанк в проекте машины
//                    if (!ChunkCoordToOrderIndexMap.TryGetValue(chunkCoord, out int orderIndex))
//                    {
//                        continue;
//                    }

//                    int chunkOffset = orderIndex * chunkVolume;

//                    // Находим начальный индекс ulong-маски для данного чанка в плоском буфере
//                    int maskOffset = orderIndex * ulongsPerChunk;

//                    int3 chunkOrigin = chunkCoord * 32;

//                    for (int i = 0; i < chunkVolume; i++)
//                    {
//                        // 2. Быстрая проверка битовой маски (адаптировано под ваш DynamicBuffer)
//                        int ulongIndex = i >> 6;      // i / 64 (индекс внутри чанка от 0 до 511)
//                        int bitOffset = i & 63;       // i % 64 (сдвиг бита от 0 до 63)

//                        // Читаем конкретное ulong-значение из общего плоского массива масок
//                        ulong maskValue = LiveMasksBufferArray[maskOffset + ulongIndex].Value;

//                        bool isVoxelNotDestroyed = (maskValue & (1UL << bitOffset)) != 0;
//                        if (!isVoxelNotDestroyed) continue;

//                        // 3. Проверка исходного цвета из фабрики
//                        byte color = FlattenedModelColors[chunkOffset + i];
//                        if (color == 0) continue;

//                        hasAnyVoxels = true;

//                        // 4. Восстановление локальных координат вокселя в чанке
//                        int lx = i & 31;
//                        int ly = (i >> 5) & 31;
//                        int lz = (i >> 10) & 31;

//                        // 5. Расчет глобальной позиции в метрах для физики Unity
//                        int3 globalVoxelPos = chunkOrigin + new int3(lx, ly, lz);
//                        float3 voxelPosInMeters = new float3(globalVoxelPos) * VoxelScale;

//                        min = math.min(min, voxelPosInMeters);
//                        max = math.max(max, voxelPosInMeters);
//                    }
//                }
//            }
//        }

//        if (!hasAnyVoxels)
//        {
//            //OutMinBounds.Value = float3.zero;
//            //OutMaxBounds.Value = float3.zero;
//            OutputBoxGeometry[0] = new BoxGeometry { Size = float3.zero };
//        }
//        else
//        {
//            // Смещение на половину вокселя, чтобы куб закрывал внешние грани, а не пивоты вокселей
//            float3 halfVoxel = new float3(VoxelScale * 0.5f);
//            float3 finalMin = min - halfVoxel;
//            float3 finalMax = max + halfVoxel;

//            // Прямо в джобе собираем финальные параметры куба
//            OutputBoxGeometry[0] = new BoxGeometry
//            {
//                Center = (finalMin + finalMax) * 0.5f,
//                Size = finalMax - finalMin,
//                Orientation = quaternion.identity,
//                BevelRadius = 0.005f // Сглаживание углов
//            };
//        }

//        //// Запекаем сжатый физический коллайдер на сервере
//        //var boxGeometry = new BoxGeometry
//        //{
//        //    Center = new float3(16, 16, 16),
//        //    Size = new float3(32, 32, 32),
//        //    Orientation = quaternion.identity
//        //};
//        //OutputColliderBlob[0] = Unity.Physics.BoxCollider.Create(boxGeometry);

//        // возвращаем статусы / z = 1 - джоба завершена
//        JobStatusRef[0] = new int3(0, 0, 1);
//    }
//}
