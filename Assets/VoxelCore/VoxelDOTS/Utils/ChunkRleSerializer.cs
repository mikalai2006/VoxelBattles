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

    /// <summary>
    /// Сжимает маску уничтожения чанка в компактный RLE список байт (Безопасно, работает в Burst)
    /// </summary>
    public static void CompressToRle(NativeArray<LocalChunkDestructionMask> maskArray, ref FixedList512Bytes<byte> outputList)
    {
        outputList.Clear();

        // Безопасно переинтерпретируем массив структур ulong в плоский срез байт
        NativeSlice<byte> byteSlice = maskArray.Reinterpret<byte>(sizeof(ulong));

        byte currentByte = byteSlice[0];
        byte runLength = 1;

        for (int i = 1; i < TotalBytesInChunk; i++)
        {
            byte nextByte = byteSlice[i];

            // Если байты совпадают и счетчик не переполнен (max 255)
            if (nextByte == currentByte && runLength < 255)
            {
                runLength++;
            }
            else
            {
                // Если FixedList переполнен (аномально хаотичный чанк) — страховка
                if (outputList.Length + 2 > outputList.Capacity) break;

                outputList.Add(runLength);
                outputList.Add(currentByte);

                currentByte = nextByte;
                runLength = 1;
            }
        }

        // Записываем финальную пару
        if (outputList.Length + 2 <= outputList.Capacity)
        {
            outputList.Add(runLength);
            outputList.Add(currentByte);
        }
    }

    /// <summary>
    /// Распаковывает RLE байты обратно в маску чанка (Безопасно, работает в Burst)
    /// </summary>
    public static void DecompressFromRle(ref FixedList512Bytes<byte> rleData, NativeArray<LocalChunkDestructionMask> targetMaskArray)
    {
        // Безопасно переинтерпретируем целевой массив в байты для записи
        NativeSlice<byte> byteSlice = targetMaskArray.Reinterpret<byte>(sizeof(ulong));

        int byteIndex = 0;
        int rleLength = rleData.Length;

        // Итерируемся по парам [счетчик, значение]
        for (int i = 0; i < rleLength; i += 2)
        {
            byte count = rleData[i];
            byte value = rleData[i + 1];

            for (int c = 0; c < count; c++)
            {
                // Защита от выхода за границы массива (на случай поврежденных сетевых пакетов)
                if (byteIndex >= TotalBytesInChunk) return;

                byteSlice[byteIndex++] = value;
            }
        }
    }
}
