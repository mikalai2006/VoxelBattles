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
    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMask;

    public NativeArray<int3> JobStatusRef;
    public float VoxelScale;
    public int ChunkOffsetInFlattenedArray;

    // Выходной контейнер для геометрии
    public NativeArray<BoxGeometry> OutputBoxGeometry;

    public void Execute()
    {
        float3 min = new float3(float.MaxValue);
        float3 max = new float3(float.MinValue);
        bool hasAnyVoxels = false;

        const int chunkVolume = 32768; // 32^3 вокселей в одном чанке

        // Проходим по всем 32^3 вокселям ВНУТРИ ОДНОГО этого чанка
        for (int i = 0; i < chunkVolume; i++)
        {
            // 1. Проверяем вашу битовую маску разрушений чанка
            int ulongIndex = i >> 6;      // Индекс ulong (0..511)
            int bitOffset = i & 63;       // Смещение бита (0..63)

            // Читаем маску (индекс ulongIndex строго от 0 до 511, никакой OutOfRange невозможен)
            ulong maskValue = LiveMask[ulongIndex].Value;

            bool isVoxelNotDestroyed = (maskValue & (1UL << bitOffset)) != 0;
            if (!isVoxelNotDestroyed) continue;

            // 2. Проверяем исходный цвет в запеченном массиве
            int targetColorIndex = ChunkOffsetInFlattenedArray + i;

            // Безопасная проверка выхода за границы плоского массива цветов
            if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) continue;

            byte color = FlattenedModelColors[targetColorIndex];
            if (color == 0) continue; // Воздух

            hasAnyVoxels = true;

            // 3. Восстанавливаем локальные координаты X, Y, Z вокселя внутри чанка (0..31)
            int lx = i & 31;
            int ly = (i >> 5) & 31;
            int lz = (i >> 10) & 31;

            // Позиция СТРОГО ОТНОСИТЕЛЬНО НУЛЯ ЧАНКА в метрах!
            // Никаких глобальных координат модели сюда не добавляем, чтобы убрать баг с двойным сдвигом
            float3 localVoxelPosInMeters = new float3(lx, ly, lz) * VoxelScale;

            min = math.min(min, localVoxelPosInMeters);
            max = math.max(max, localVoxelPosInMeters);
        }

        // Записываем итоговую геометрию для этого чанка
        if (!hasAnyVoxels)
        {
            OutputBoxGeometry[0] = new BoxGeometry { Size = float3.zero };
        }
        else
        {
            // Корректируем min/max на половину вокселя для плотного облегания внешних граней
            float3 halfVoxel = new float3(VoxelScale * 0.5f);
            float3 finalMin = min - halfVoxel;
            float3 finalMax = max + halfVoxel;

            // Собираем чистую Box-геометрию в локальном пространстве чанка
            OutputBoxGeometry[0] = new BoxGeometry
            {
                Center = (finalMin + finalMax) * 0.5f,
                Size = finalMax - finalMin,
                Orientation = quaternion.identity,
                BevelRadius = 0.005f // Небольшой скос для плавной физики столкновений
            };
        }

        // возвращаем статусы / z = 1 - джоба завершена
        JobStatusRef[0] = new int3(0, 0, 1);
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
