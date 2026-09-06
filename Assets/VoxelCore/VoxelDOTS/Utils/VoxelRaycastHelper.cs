using Unity.Entities;
using Unity.Mathematics;

public struct RaycastVoxelResult
{
    public bool Hit;           // Был ли найден существующий блок
    public int3 VoxelIndex;    // Локальный индекс вокселя [0..31] внутри чанка
    public float Distance;     // Дистанция до точки попадания
}

public static class VoxelRaycastHelper
{
    /// <summary>
    /// Находит ближайший существующий воксель внутри чанка вдоль луча.
    /// </summary>
    public static RaycastVoxelResult RaycastChunk(
        DynamicBuffer<LocalChunkDestructionMask> destructionMask,
        float3 rayStart,
        float3 rayDir,
        float maxDistance)
    {
        RaycastVoxelResult result = new RaycastVoxelResult();

        // Нормализуем направление луча во избежание ошибок в расчетах расстояния
        rayDir = math.normalize(rayDir);

        // Текущие координаты вокселя (округляем вниз до ближайшего целого)
        int3 currentVoxel = (int3)math.floor(rayStart);

        // Направление шага по каждой оси (-1 или 1)
        int3 step = (int3)math.sign(rayDir);

        // Предотвращаем деление на 0, если луч параллелен какой-то из осей
        const float epsilon = 1e-6f;
        float3 safeDir = new float3(
            math.abs(rayDir.x) < epsilon ? epsilon * math.sign(rayDir.x) : rayDir.x,
            math.abs(rayDir.y) < epsilon ? epsilon * math.sign(rayDir.y) : rayDir.y,
            math.abs(rayDir.z) < epsilon ? epsilon * math.sign(rayDir.z) : rayDir.z
        );

        // Сколько расстояния по лучу нужно пройти, чтобы преодолеть размер одного вокселя (1.0 единиц)
        float3 tDelta = math.abs(1.0f / safeDir);

        // Расстояние до ближайшей границы вокселя по каждой из осей
        float3 tMax;
        tMax.x = (step.x > 0) ? (math.floor(rayStart.x) + 1.0f - rayStart.x) * tDelta.x : (rayStart.x - math.floor(rayStart.x)) * tDelta.x;
        tMax.y = (step.y > 0) ? (math.floor(rayStart.y) + 1.0f - rayStart.y) * tDelta.y : (rayStart.y - math.floor(rayStart.y)) * tDelta.y;
        tMax.z = (step.z > 0) ? (math.floor(rayStart.z) + 1.0f - rayStart.z) * tDelta.z : (rayStart.z - math.floor(rayStart.z)) * tDelta.z;

        float distance = 0.0f;

        // Основной цикл трассировки
        while (distance < maxDistance)
        {
            // 1. Проверяем, находится ли текущий воксель внутри границ чанка [0..31]
            if (currentVoxel.x >= 0 && currentVoxel.x < 32 &&
                currentVoxel.y >= 0 && currentVoxel.y < 32 &&
                currentVoxel.z >= 0 && currentVoxel.z < 32)
            {
                // Вычисляем плоский XYZ-индекс вокселя в чанке
                int flatIndex = currentVoxel.x + (currentVoxel.y << 5) + (currentVoxel.z << 10);

                int ulongIndex = flatIndex >> 6;  // Деление на 64
                int bitOffset = flatIndex & 63;   // Остаток от деления на 64
                ulong maskBit = 1UL << bitOffset;

                // 2. ПРОВЕРКА БИТА: Если бит равен 1 — воксель существует!
                if ((destructionMask[ulongIndex].Value & maskBit) != 0)
                {
                    result.Hit = true;
                    result.VoxelIndex = currentVoxel;
                    result.Distance = distance;
                    return result; // Нашли ближайший, мгновенно выходим
                }
            }
            else
            {
                // Если луч вышел за пределы чанка 32х32х32, останавливаем поиск
                break;
            }

            // 3. ШАГ АЛГОРИТМА: Выбираем ось, до границы которой расстояние по лучу наименьшее
            if (tMax.x < tMax.y)
            {
                if (tMax.x < tMax.z)
                {
                    distance = tMax.x;
                    tMax.x += tDelta.x;
                    currentVoxel.x += step.x;
                }
                else
                {
                    distance = tMax.z;
                    tMax.z += tDelta.z;
                    currentVoxel.z += step.z;
                }
            }
            else
            {
                if (tMax.y < tMax.z)
                {
                    distance = tMax.y;
                    tMax.y += tDelta.y;
                    currentVoxel.y += step.y;
                }
                else
                {
                    distance = tMax.z;
                    tMax.z += tDelta.z;
                    currentVoxel.z += step.z;
                }
            }
        }

        // Если луч прошел всю дистанцию и ничего не встретил
        result.Hit = false;
        return result;
    }
}
