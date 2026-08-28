//using Unity.Collections;

//public static class ChunkRleSerializer
//{
//    private const int TotalBytesInChunk = 4096; // Новая сетка изначально байтовая

//    /// <summary>
//    /// Сжимает байтовую маску уничтожения чанка в компактный RLE список байт (Безопасно, работает в Burst)
//    /// </summary>
//    public static void CompressToRle(ref FixedList4096Bytes<byte> sourceBytes, ref FixedList512Bytes<byte> outputList)
//    {
//        outputList.Clear();

//        // Работаем напрямую по индексу фиксированного списка
//        byte currentByte = sourceBytes[0];
//        byte runLength = 1;

//        for (int i = 1; i < TotalBytesInChunk; i++)
//        {
//            byte nextByte = sourceBytes[i];

//            // Если байты совпадают и счетчик не переполнен (max 255)
//            if (nextByte == currentByte && runLength < 255)
//            {
//                runLength++;
//            }
//            else
//            {
//                // Если FixedList переполнен (аномально хаотичный чанк) — страховка
//                if (outputList.Length + 2 > outputList.Capacity) break;

//                outputList.Add(runLength);
//                outputList.Add(currentByte);

//                currentByte = nextByte;
//                runLength = 1;
//            }
//        }

//        // Записываем финальную пару
//        if (outputList.Length + 2 <= outputList.Capacity)
//        {
//            outputList.Add(runLength);
//            outputList.Add(currentByte);
//        }
//    }

//    /// <summary>
//    /// Распаковывает RLE байты обратно в фиксированную маску компонента чанка (Безопасно, работает в Burst)
//    /// </summary>
//    public static void DecompressFromRle(ref FixedList512Bytes<byte> rleData, ref FixedList4096Bytes<byte> targetBytes)
//    {
//        int byteIndex = 0;
//        int rleLength = rleData.Length;

//        // Итерируемся по пришедшим парам [счетчик, значение]
//        for (int i = 0; i < rleLength; i += 2)
//        {
//            byte count = rleData[i];
//            byte value = rleData[i + 1];

//            for (int c = 0; c < count; c++)
//            {
//                // Защита от выхода за границы массива (на случай поврежденных сетевых пакетов)
//                if (byteIndex >= TotalBytesInChunk) return;

//                // Пишем байт напрямую по индексу в FixedList компонента
//                targetBytes[byteIndex++] = value;
//            }
//        }
//    }
//}


using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public static class VoxelInputPackingUtility
{
    // Упаковка float2 направления в 1 байт битовой маски
    public static byte PackFloat2ToBits(float2 moveInput)
    {
        byte mask = 0;
        if (moveInput.y > 0.1f) mask |= 1;  // Вперед (Бит 0)
        if (moveInput.y < -0.1f) mask |= 2;  // Назад  (Бит 1)
        if (moveInput.x < -0.1f) mask |= 4;  // Влево  (Бит 2)
        if (moveInput.x > 0.1f) mask |= 8;  // Вправо (Бит 3)
        return mask;
    }

    // Распаковка 1 байта обратно в чистый float2 для использования в физике машины
    public static float2 UnpackBitsToFloat2(byte buttonsMask)
    {
        float2 move = float2.zero;
        if ((buttonsMask & 1) != 0) move.y += 1f;
        if ((buttonsMask & 2) != 0) move.y -= 1f;
        if ((buttonsMask & 4) != 0) move.x -= 1f;
        if ((buttonsMask & 8) != 0) move.x += 1f;
        return move;
    }
}



public static class ChunkRleSerializer
{
    private const int TotalBytesInChunk = 512 * 8; // 512 ulong * 8 байт = 4096 байт

    //public static void CompressToRle(NativeArray<LocalChunkDestructionMask> maskArray, ref FixedList512Bytes<int> outputList)
    //{
    //    outputList.Clear();

    //    ulong currentUlong = maskArray[0].Value;
    //    int runLength = 1;

    //    for (int i = 1; i < 512; i++)
    //    {
    //        ulong nextUlong = maskArray[i].Value;

    //        if (nextUlong == currentUlong && runLength < 65535)
    //        {
    //            runLength++;
    //        }
    //        else
    //        {
    //            if (outputList.Length + 3 > outputList.Capacity) break;

    //            // Разбиваем 64-битный ulong на два 32-битных int для безопасной передачи
    //            int high = (int)(currentUlong >> 32);
    //            int low = (int)(currentUlong & 0xFFFFFFFF);

    //            outputList.Add(runLength);
    //            outputList.Add(high);
    //            outputList.Add(low);

    //            currentUlong = nextUlong;
    //            runLength = 1;
    //        }
    //    }

    //    if (outputList.Length + 3 <= outputList.Capacity)
    //    {
    //        int high = (int)(currentUlong >> 32);
    //        int low = (int)(currentUlong & 0xFFFFFFFF);

    //        outputList.Add(runLength);
    //        outputList.Add(high);
    //        outputList.Add(low);
    //    }
    //}

    //public static void DecompressFromRle(in FixedList512Bytes<int> rleData, DynamicBuffer<LocalChunkDestructionMask> targetBuffer)
    //{
    //    if (targetBuffer.Length < 512) targetBuffer.ResizeUninitialized(512);

    //    int ulongIndex = 0;
    //    int rleLength = rleData.Length;

    //    for (int i = 0; i < rleLength; i += 3)
    //    {
    //        int count = rleData[i];
    //        long high = (uint)rleData[i + 1];
    //        long low = (uint)rleData[i + 2];

    //        // Собираем обратно 64-битный ulong из двух int без потерь и сдвига байт процессора
    //        ulong ulongValue = (ulong)((high << 32) | low);

    //        for (int c = 0; c < count; c++)
    //        {
    //            if (ulongIndex >= 512) return;

    //            // Пишем ulong целиком напрямую в память буфера клиента!
    //            targetBuffer[ulongIndex++] = new LocalChunkDestructionMask { Value = ulongValue };
    //        }
    //    }
    //}
    /// <summary>
    /// Сжимает маску уничтожения чанка в компактный RLE список байт (Безопасно, работает в Burst)
    /// </summary>
    //public static void CompressToRle(NativeArray<LocalChunkDestructionMask> maskArray, ref FixedList512Bytes<byte> outputList)
    //{
    //    outputList.Clear();

    //    // Безопасно переинтерпретируем массив структур ulong в плоский срез байт
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    byte currentByte = byteSlice[0];
    //    byte runLength = 1;

    //    for (int i = 1; i < TotalBytesInChunk; i++)
    //    {
    //        byte nextByte = byteSlice[i];

    //        // Если байты совпадают и счетчик не переполнен (max 255)
    //        if (nextByte == currentByte && runLength < 255)
    //        {
    //            runLength++;
    //        }
    //        else
    //        {
    //            // Если FixedList переполнен (аномально хаотичный чанк) — страховка
    //            if (outputList.Length + 2 > outputList.Capacity) break;

    //            outputList.Add(runLength);
    //            outputList.Add(currentByte);

    //            currentByte = nextByte;
    //            runLength = 1;
    //        }
    //    }

    //    // Записываем финальную пару
    //    if (outputList.Length + 2 <= outputList.Capacity)
    //    {
    //        outputList.Add(runLength);
    //        outputList.Add(currentByte);
    //    }
    //    UnityEngine.Debug.Log($"[Voxel Network] Размер RLE пакета: {outputList.Length} байт из {outputList.Capacity}");
    //}

    ///// <summary>
    ///// Распаковывает RLE байты обратно в маску чанка (Безопасно, работает в Burst)
    ///// </summary>
    //public static void DecompressFromRle(ref FixedList512Bytes<byte> rleData, NativeArray<LocalChunkDestructionMask> targetMaskArray)
    //{
    //    // Безопасно переинтерпретируем целевой массив в байты для записи
    //    NativeSlice<byte> byteSlice = targetMaskArray.Reinterpret<byte>(sizeof(ulong));

    //    int byteIndex = 0;
    //    int rleLength = rleData.Length;

    //    // Итерируемся по парам [счетчик, значение]
    //    for (int i = 0; i < rleLength; i += 2)
    //    {
    //        byte count = rleData[i];
    //        byte value = rleData[i + 1];

    //        for (int c = 0; c < count; c++)
    //        {
    //            // Защита от выхода за границы массива (на случай поврежденных сетевых пакетов)
    //            if (byteIndex >= TotalBytesInChunk) return;

    //            byteSlice[byteIndex++] = value;
    //        }
    //    }
    //}
    public static void CompressToRle(NativeArray<LocalChunkDestructionMask> maskArray, ref FixedList512Bytes<byte> outputList)
    {
        outputList.Clear();

        // Временный плоский массив типов для 512 элементов ulong чанка
        NativeArray<byte> ulongTypes = new NativeArray<byte>(512, Allocator.Temp);
        // Временный список для хранения компактных байт дельты частично задетых ulong
        NativeList<byte> customBytes = new NativeList<byte>(Allocator.Temp);

        for (int i = 0; i < 512; i++)
        {
            ulong val = maskArray[i].Value;

            if (val == 0xFFFFFFFFFFFFFFFFUL)
            {
                ulongTypes[i] = 1; // Целая пачка вокселей
            }
            else if (val == 0UL)
            {
                ulongTypes[i] = 0; // Полностью уничтоженная пачка
            }
            else
            {
                ulongTypes[i] = 2; // Частично задетая пачка (край взрыва)

                // Вместо 8 байт ulong, сжимаем его побайтово и берем только те байты, 
                // которые изменились, либо компактно укладываем структуру.
                // Для простоты и 100% точности Safe-кода, сохраняем 8 байт этого ulong
                for (int b = 0; b < 8; b++)
                {
                    customBytes.Add((byte)((val >> (b * 8)) & 0xFF));
                }
            }
        }

        // Сжимаем массив типов с помощью RLE
        byte currentType = ulongTypes[0];
        byte runLength = 1;

        for (int i = 1; i < 512; i++)
        {
            byte nextType = ulongTypes[i];

            if (nextType == currentType && runLength < 255)
            {
                runLength++;
            }
            else
            {
                if (outputList.Length + 2 > outputList.Capacity) break;
                outputList.Add(runLength);
                outputList.Add(currentType);

                currentType = nextType;
                runLength = 1;
            }
        }
        if (outputList.Length + 2 <= outputList.Capacity)
        {
            outputList.Add(runLength);
            outputList.Add(currentType);
        }

        // Дописываем в конец RPC-пакета сырые байты краев взрыва.
        // Так как краев у сферы мало, эти байты займут от силы 100-200 байт.
        int maxCustomBytes = math.min(customBytes.Length, outputList.Capacity - outputList.Length);
        for (int i = 0; i < maxCustomBytes; i++)
        {
            outputList.Add(customBytes[i]);
        }

        ulongTypes.Dispose();
        customBytes.Dispose();
    }

    public static void DecompressFromRle(in FixedList512Bytes<byte> rleData, DynamicBuffer<LocalChunkDestructionMask> targetBuffer)
    {
        if (targetBuffer.Length < 512) targetBuffer.ResizeUninitialized(512);

        NativeArray<byte> ulongTypes = new NativeArray<byte>(512, Allocator.Temp);
        int typeIndex = 0;
        int rleLength = rleData.Length;
        int i = 0;

        // 1. Распаковываем RLE карту типов (счетчики повторений)
        // Мы знаем, что должны восстановить строго 512 типов
        while (typeIndex < 512 && i < rleLength)
        {
            byte count = rleData[i++];
            byte type = rleData[i++];

            for (int c = 0; c < count; c++)
            {
                if (typeIndex >= 512) break;
                ulongTypes[typeIndex++] = type;
            }
        }

        // Переменная 'i' сейчас указывает ровно на начало сырых байт краев взрыва!
        // 2. Восстанавливаем ulong-маску чанка на основе карты типов
        for (int c = 0; c < 512; c++)
        {
            byte type = ulongTypes[c];
            ulong ulongValue = 0UL;

            if (type == 1)
            {
                ulongValue = 0xFFFFFFFFFFFFFFFFUL; // Полностью целый
            }
            else if (type == 2)
            {
                // Собираем ulong обратно из 8 последовательных байт пакета RPC
                ulongValue = 0UL;
                if (i + 8 <= rleLength)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        ulong byteVal = rleData[i++];
                        ulongValue |= (byteVal << (b * 8));
                    }
                }
            }
            // Если type == 0, то ulongValue остается равным 0UL (полностью уничтожен)

            // Пишем готовую пачку из 64 вокселей строго по индексу памяти на клиенте
            targetBuffer[c] = new LocalChunkDestructionMask { Value = ulongValue };
        }

        ulongTypes.Dispose();
    }


    //public static void CompressToRle(NativeArray<LocalChunkDestructionMask> maskArray, ref FixedList512Bytes<byte> outputList)
    //{
    //    outputList.Clear();

    //    // 1. Извлекаем состояние самого первого вокселя (flatIndex = 0) строго по вашей XYZ схеме
    //    int firstUlongIdx = 0; // 0 >> 6
    //    int firstBitOffset = 0; // 0 & 63
    //    byte currentBitState = (byte)((maskArray[firstUlongIdx].Value >> firstBitOffset) & 1UL);

    //    byte runLength = 1;
    //    const int TotalVoxels = 32768; // 32x32x32

    //    // 2. Линейно перебираем абсолютно все воксели по порядку их flatIndex в памяти
    //    for (int flatIndex = 1; flatIndex < TotalVoxels; flatIndex++)
    //    {
    //        // Находим адрес бита строго так же, как это делает ваш IsVoxelLive
    //        int ulongIndex = flatIndex >> 6;  // Деление на 64
    //        int bitOffset = flatIndex & 63;   // Остаток от деления на 64

    //        // Получаем чистый бит вокселя: 1 (жив) или 0 (уничтожен)
    //        byte bitState = (byte)((maskArray[ulongIndex].Value >> bitOffset) & 1UL);

    //        // Если состояние вокселя совпадает и счетчик не переполнен — просто инкрементируем
    //        if (bitState == currentBitState && runLength < 255)
    //        {
    //            runLength++;
    //        }
    //        else
    //        {
    //            // Страховка от переполнения FixedList512Bytes
    //            if (outputList.Length + 2 > outputList.Capacity) break;

    //            // Записываем честную воксельную пару: [сколько штук, состояние 0 или 1]
    //            outputList.Add(runLength);
    //            outputList.Add(currentBitState);

    //            currentBitState = bitState;
    //            runLength = 1;
    //        }
    //    }

    //    // Записываем финальную воксельную пару
    //    if (outputList.Length + 2 <= outputList.Capacity)
    //    {
    //        outputList.Add(runLength);
    //        outputList.Add(currentBitState);
    //    }
    //}
    //public static void DecompressFromRle(in FixedList512Bytes<byte> rleData, DynamicBuffer<LocalChunkDestructionMask> targetBuffer)
    //{
    //    // Гарантируем правильный размер маски чанка на клиенте (512 ulong элементов)
    //    if (targetBuffer.Length < 512) targetBuffer.ResizeUninitialized(512);

    //    // Перед распаковкой принудительно забиваем весь буфер единицами (машина целая)
    //    // Чтобы если пакет вдруг оборвется, остаток чанка остался целым, а не обнулился!
    //    for (int i = 0; i < 512; i++) targetBuffer[i] = new LocalChunkDestructionMask { Value = 0xFFFFFFFFFFFFFFFFUL };

    //    int flatIndex = 0;
    //    int rleLength = rleData.Length;

    //    // Распаковываем воксельные пары [count, state] строго обратно по flatIndex
    //    for (int i = 0; i < rleLength; i += 2)
    //    {
    //        byte count = rleData[i];
    //        byte state = rleData[i + 1]; // 1 — блок есть, 0 — пусто

    //        for (int c = 0; c < count; c++)
    //        {
    //            // Защита от выхода за пределы объема чанка (32768)
    //            if (flatIndex >= 32768) return;

    //            int ulongIndex = flatIndex >> 6;
    //            int bitOffset = flatIndex & 63;

    //            LocalChunkDestructionMask maskElement = targetBuffer[ulongIndex];

    //            if (state == 1)
    //            {
    //                maskElement.Value |= (1UL << bitOffset);  // Взводим бит в 1
    //            }
    //            else
    //            {
    //                maskElement.Value &= ~(1UL << bitOffset); // Гасим бит в 0 (пустота)
    //            }

    //            targetBuffer[ulongIndex] = maskElement;
    //            flatIndex++;
    //        }
    //    }
    //}








    //[BurstCompile]
    //public static void CompressToNativeArray(NativeArray<LocalChunkDestructionMask> maskArray, NativeArray<byte> outputArray, out int writtenBytes)
    //{
    //    // Безопасно переинтерпретируем ulong-массив в плоский срез байт вручную или через встроенный метод
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    byte currentByte = byteSlice[0];
    //    byte runLength = 1;
    //    int index = 0;

    //    // В чанке строго 4096 байт (512 элементов ulong * 8 байт)
    //    for (int i = 1; i < 4096; i++)
    //    {
    //        byte nextByte = byteSlice[i];

    //        if (nextByte == currentByte && runLength < 255)
    //        {
    //            runLength++;
    //        }
    //        else
    //        {
    //            // Записываем пару в NativeArray — места здесь с огромным запасом (4096)
    //            outputArray[index++] = runLength;
    //            outputArray[index++] = currentByte;

    //            currentByte = nextByte;
    //            runLength = 1;
    //        }
    //    }

    //    // Записываем финальную пару
    //    outputArray[index++] = runLength;
    //    outputArray[index++] = currentByte;

    //    writtenBytes = index; // Возвращаем реальное количество записанных байт
    //}

    //[BurstCompile]
    //public static void DecompressFromNativeArray(NativeArray<byte> rleData, int rleLength, DynamicBuffer<LocalChunkDestructionMask> targetBuffer)
    //{
    //    if (targetBuffer.Length < 512)
    //    {
    //        targetBuffer.ResizeUninitialized(512);
    //    }

    //    NativeArray<LocalChunkDestructionMask> maskArray = targetBuffer.AsNativeArray();
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    int byteIndex = 0;

    //    // Итерируемся по собранному плоскому массиву байт парами
    //    for (int i = 0; i < rleLength; i += 2)
    //    {
    //        byte count = rleData[i];
    //        byte value = rleData[i + 1];

    //        for (int c = 0; c < count; c++)
    //        {
    //            if (byteIndex >= 4096) return; // Жесткая защита границ
    //            byteSlice[byteIndex++] = value;
    //        }
    //    }
    //}



    //[BurstCompile]
    //public static void DecompressFromRle(
    //ref FixedList512Bytes<byte> rleData,
    //DynamicBuffer<LocalChunkDestructionMask> targetBuffer) // Передаем живой буфер чанка!
    //{
    //    // Гарантируем, что буфер клиента инициализирован (32х32х32 = 512 элементов ulong)
    //    if (targetBuffer.Length < 512)
    //    {
    //        targetBuffer.ResizeUninitialized(512);
    //    }

    //    // Получаем ПРЯМОЙ доступ к памяти буфера без копирования!
    //    NativeArray<LocalChunkDestructionMask> maskArray = targetBuffer.AsNativeArray();

    //    // Безопасно переинтерпретируем целевой массив в байты (размер чанка строго 4096 байт)
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    int byteIndex = 0;
    //    int rleLength = rleData.Length;

    //    // Итерируемся по парам [счетчик, значение]
    //    for (int i = 0; i < rleLength; i += 2)
    //    {
    //        byte count = rleData[i];
    //        byte value = rleData[i + 1];

    //        for (int c = 0; c < count; c++)
    //        {
    //            // Защита от выхода за границы (512 * 8 = 4096 байт)
    //            if (byteIndex >= 4096) return;

    //            // Пишем байт напрямую в память DynamicBuffer!
    //            byteSlice[byteIndex++] = value;
    //        }
    //    }
    //}

    //public static void CompressToDynamicBuffer(NativeArray<LocalChunkDestructionMask> maskArray, DynamicBuffer<ChunkNetworkRleMaskUpdate> outputBuffer)
    //{
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    byte currentByte = byteSlice[0];
    //    byte runLength = 1;

    //    for (int i = 1; i < 4096; i++)
    //    {
    //        byte nextByte = byteSlice[i];

    //        if (nextByte == currentByte && runLength < 255)
    //        {
    //            runLength++;
    //        }
    //        else
    //        {
    //            outputBuffer.Add(new ChunkNetworkRleMaskUpdate { ByteValue = runLength });
    //            outputBuffer.Add(new ChunkNetworkRleMaskUpdate { ByteValue = currentByte });

    //            currentByte = nextByte;
    //            runLength = 1;
    //        }
    //    }
    //    outputBuffer.Add(new ChunkNetworkRleMaskUpdate { ByteValue = runLength });
    //    outputBuffer.Add(new ChunkNetworkRleMaskUpdate { ByteValue = currentByte });
    //}

    //public static void DecompressFromDynamicBuffer(
    //DynamicBuffer<ChunkNetworkRleMaskUpdate> rleBuffer,
    //DynamicBuffer<LocalChunkDestructionMask> targetMask)
    //{
    //    if (targetMask.Length < 512) targetMask.ResizeUninitialized(512);

    //    NativeArray<LocalChunkDestructionMask> maskArray = targetMask.AsNativeArray();
    //    NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

    //    int byteIndex = 0;
    //    int rleLength = rleBuffer.Length;

    //    for (int i = 0; i < rleLength; i += 2)
    //    {
    //        byte count = rleBuffer[i].ByteValue;
    //        byte value = rleBuffer[i + 1].ByteValue;

    //        for (int c = 0; c < count; c++)
    //        {
    //            if (byteIndex >= 4096) return;
    //            byteSlice[byteIndex++] = value;
    //        }
    //    }
    //}

}
