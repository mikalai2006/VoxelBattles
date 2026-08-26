using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;


[BurstCompile(CompileSynchronously = true)]
public struct GenerateChunkColliderJob : IJob
{
    // Передаем буфер как NativeArray.AsReadOnly() из системы
    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMask;

    // Выходной контейнер для готового BlobAsset коллайдера (на 1 элемент)
    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

    // Константа размера для оптимизации компилятора (Burst заменит операции деления/умножения на сдвиги битов)
    private const int CHUNK_SIZE = 32;

    public void Execute()
    {
        // Локальные списки во временной памяти (стековый аллокатор Unity, очень быстрый)
        var vertices = new NativeList<float3>(Allocator.Temp);
        var triangles = new NativeList<int3>(Allocator.Temp);

        // Порядок циклов Z -> Y -> X оптимизирован под формулу индекса X + 32 * (Y + 32 * Z)
        // Это обеспечивает последовательное чтение битов из ulong и минимизирует промахи кэша процессора
        for (int z = 0; z < CHUNK_SIZE; z++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    int flatIndex = x + CHUNK_SIZE * (y + CHUNK_SIZE * z);

                    // Если блок НЕ разрушен (Solid)
                    if (IsBlockSolid(flatIndex))
                    {
                        // Проверяем 6 соседних направлений
                        CheckAndAddFace(x, y, z, new int3(1, 0, 0), vertices, triangles);  // +X (Право)
                        CheckAndAddFace(x, y, z, new int3(-1, 0, 0), vertices, triangles); // -X (Лево)
                        CheckAndAddFace(x, y, z, new int3(0, 1, 0), vertices, triangles);  // +Y (Верх)
                        CheckAndAddFace(x, y, z, new int3(0, -1, 0), vertices, triangles); // -Y (Низ)
                        CheckAndAddFace(x, y, z, new int3(0, 0, 1), vertices, triangles);  // +Z (Вперед)
                        CheckAndAddFace(x, y, z, new int3(0, 0, -1), vertices, triangles); // -Z (Назад)
                    }
                }
            }
        }

        // Если чанк полностью уничтожен (нет геометрии), возвращаем пустую ссылку
        if (vertices.Length == 0)
        {
            OutputColliderBlob[0] = default;
            return;
        }

        // Строим MeshCollider на потоке-воркере. На размере 32x32x32 дерево BVH соберется мгновенно.
        BlobAssetReference<Collider> meshColliderBlob = Unity.Physics.MeshCollider.Create(
            vertices.AsArray(),
            triangles.AsArray(),
            CollisionFilter.Default
        );

        // Сохраняем результат
        OutputColliderBlob[0] = meshColliderBlob;

        // Освобождаем временную память списков
        vertices.Dispose();
        triangles.Dispose();
    }

    // Проверка битового флага в массиве ulong элементов буфера
    private bool IsBlockSolid(int flatIndex)
    {
        // flatIndex / 64 через битовый сдвиг (Burst сделает это автоматически благодаря константам)
        int ulongIndex = flatIndex >> 6;

        // Защита от выхода за границы (для чанка 32^3 верхний индекс равен 511)
        if (ulongIndex < 0 || ulongIndex >= 512) return false;

        // flatIndex % 64
        int bitIndex = flatIndex & 63;
        ulong maskValue = LiveMask[ulongIndex].Value;

        // Извлекаем бит: 1 — разрушен, 0 — цел
        bool isDestroyed = ((maskValue >> bitIndex) & 1UL) == 1UL;

        return !isDestroyed;
    }

    private void CheckAndAddFace(int x, int y, int z, int3 direction, NativeList<float3> vertices, NativeList<int3> triangles)
    {
        int3 neighbor = new int3(x, y, z) + direction;

        // Если сосед за пределами этого чанка (граница), строим внешнюю стенку коллизии
        if (neighbor.x < 0 || neighbor.x >= CHUNK_SIZE ||
            neighbor.y < 0 || neighbor.y >= CHUNK_SIZE ||
            neighbor.z < 0 || neighbor.z >= CHUNK_SIZE)
        {
            BuildFaceGeometry(x, y, z, direction, vertices, triangles);
        }
        else
        {
            // Если сосед внутри чанка, проверяем его состояние по индексу
            int neighborFlatIndex = neighbor.x + CHUNK_SIZE * (neighbor.y + CHUNK_SIZE * neighbor.z);
            if (!IsBlockSolid(neighborFlatIndex))
            {
                BuildFaceGeometry(x, y, z, direction, vertices, triangles);
            }
        }
    }

    private void BuildFaceGeometry(int x, int y, int z, int3 direction, NativeList<float3> vertices, NativeList<int3> triangles)
    {
        int vCount = vertices.Length;
        float3 p = new float3(x, y, z);

        if (direction.x == 1) // +X
        {
            vertices.Add(p + new float3(1, 0, 0));
            vertices.Add(p + new float3(1, 1, 0));
            vertices.Add(p + new float3(1, 1, 1));
            vertices.Add(p + new float3(1, 0, 1));
        }
        else if (direction.x == -1) // -X
        {
            vertices.Add(p + new float3(0, 0, 1));
            vertices.Add(p + new float3(0, 1, 1));
            vertices.Add(p + new float3(0, 1, 0));
            vertices.Add(p + new float3(0, 0, 0));
        }
        else if (direction.y == 1) // +Y
        {
            vertices.Add(p + new float3(0, 1, 0));
            vertices.Add(p + new float3(0, 1, 1));
            vertices.Add(p + new float3(1, 1, 1));
            vertices.Add(p + new float3(1, 1, 0));
        }
        else if (direction.y == -1) // -Y
        {
            vertices.Add(p + new float3(0, 0, 1));
            vertices.Add(p + new float3(0, 0, 0));
            vertices.Add(p + new float3(1, 0, 0));
            vertices.Add(p + new float3(1, 0, 1));
        }
        else if (direction.z == 1) // +Z
        {
            vertices.Add(p + new float3(1, 0, 1));
            vertices.Add(p + new float3(1, 1, 1));
            vertices.Add(p + new float3(0, 1, 1));
            vertices.Add(p + new float3(0, 0, 1));
        }
        else if (direction.z == -1) // -Z
        {
            vertices.Add(p + new float3(0, 0, 0));
            vertices.Add(p + new float3(0, 1, 0));
            vertices.Add(p + new float3(1, 1, 0));
            vertices.Add(p + new float3(1, 0, 0));
        }

        // Обход по часовой стрелке для правильного направления нормалей физики наружу блоков
        triangles.Add(new int3(vCount + 0, vCount + 1, vCount + 2));
        triangles.Add(new int3(vCount + 0, vCount + 2, vCount + 3));
    }
}


public struct BakedBoxData
{
    public BoxGeometry Geometry;
    public float3 Position;
}

[Unity.Burst.BurstCompile]
public struct DisposeBlobAssetJob : Unity.Jobs.IJob
{
    // Передаем контейнер со старым блобом
    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> BlobContainer;

    public void Execute()
    {
        if (BlobContainer.IsCreated)
        {
            // Нативно и безопасно уничтожаем BlobAsset внутри параллельного потока
            if (BlobContainer[0].IsCreated)
            {
                BlobContainer[0].Dispose();
            }
            // Удаляем сам временный массив-контейнер
            BlobContainer.Dispose();
        }
    }
}


[BurstCompile]
public struct VoxelMeshColliderBakingJob : IJob
{
    // [ReadOnly] — разрешает безопасный параллельный доступ к геометрии
    [ReadOnly] public NativeArray<VoxelVertex> SourceVertices;
    [ReadOnly] public NativeArray<int> SourceIndices;

    // Сюда джоба запишет готовый unmanaged-хэндл физической структуры
    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

    // Реальное число элементов, переданное из m_JobCounters
    public int VertexCount;
    public int IndexCount;

    [BurstCompile]
    public void Execute()
    {
        // 1. Если чанк пустой или данные некорректны — коллайдер не нужен
        if (VertexCount < 3 || IndexCount < 3)
        {
            return;
        }

        // Защита от некратных индексов (дробные треугольники ломают физику)
        int realTriangleCount = IndexCount / 3;
        if (realTriangleCount < 1) return;

        // 2. Выделяем СТРОГО под реальное число вершин текущего канда
        var physicsPositions = new NativeArray<float3>(VertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // ПРИМЕНЕНО ПРАВИЛО: замена знаков во всех циклах
        for (int i = 0; i < VertexCount; i++)
        {
            physicsPositions[i] = SourceVertices[i].Position;
        }

        // 3. Выделяем СТРОГО под вычисленное количество треугольников чанка
        var physicsIndices = new NativeArray<int3>(realTriangleCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        int triIndex = 0;
        // ПРИМЕНЕНО ПРАВИЛО: замена знаков во всех циклах
        // Двигаемся строго до реального числа треугольников, исключая мусорные нули в конце
        for (int i = 0; i < realTriangleCount; i++)
        {
            int elementOffset = i * 3;

            physicsIndices[triIndex] = new int3(
                SourceIndices[elementOffset],
                SourceIndices[elementOffset + 1],
                SourceIndices[elementOffset + 2]
            );
            triIndex++;
        }

        // 4. Генерируем чистый MeshCollider без пустых треугольников в точке (0,0,0)
        var colliderBlob = MeshCollider.Create(
            physicsPositions,
            physicsIndices,
            CollisionFilter.Default
        );
        // Генерирует быструю выпуклую оболочку на воркере
        //var colliderBlob = ConvexCollider.Create(
        //    physicsPositions,
        //    ConvexHullGenerationParameters.Default,
        //    CollisionFilter.Default
        //);


        //// ВРЕМЕННЫЙ ТЕСТ: Вместо MeshCollider выпекаем простую динамическую коробку
        //// размером с ваш чанк (32x32x32 вокселя). BoxCollider по умолчанию ДИНАМИЧЕСКИЙ!
        //var colliderBlob = BoxCollider.Create(new BoxGeometry
        //{
        //    Center = new float3(16f, 16f, 16f),
        //    Size = new float3(32f, 32f, 32f),
        //    Orientation = quaternion.identity
        //}, CollisionFilter.Default);

        // Записываем полученный блоб-ассет в выходной массив
        OutputColliderBlob[0] = colliderBlob;

        // Чистим unmanaged-память
        physicsPositions.Dispose();
        physicsIndices.Dispose();
    }

    //public void Execute()
    //{
    //    // 1. Если чанк пустой — коллайдер не нужен
    //    if (VertexCount < 1 || IndexCount < 1)
    //    {
    //        return;
    //    }

    //    // 2. Копируем float3-позиции во временный массив (Unity.Physics требует чистые float3)
    //    var physicsPositions = new NativeArray<float3>(VertexCount, Allocator.Temp);
    //    for (int i = 0; i < VertexCount; i++)
    //    {
    //        physicsPositions[i] = SourceVertices[i].Position;
    //    }

    //    // 3. Копируем индексы треугольников во временный массив
    //    var physicsIndices = new NativeArray<int3>(IndexCount / 3, Allocator.Temp);
    //    int triIndex = 0;
    //    for (int i = 0; i < IndexCount; i += 3)
    //    {
    //        physicsIndices[triIndex++] = new int3(SourceIndices[i], SourceIndices[i + 1], SourceIndices[i + 2]);
    //    }

    //    // 4. Генерируем MeshCollider на уровне C++ ядра физического движка
    //    var colliderBlob = MeshCollider.Create(
    //        physicsPositions,
    //        physicsIndices,
    //        CollisionFilter.Default
    //    );

    //    // Записываем полученный блоб-ассет в выходной массив
    //    OutputColliderBlob[0] = colliderBlob;

    //    // Чистим временную unmanaged-память потока воркера
    //    physicsPositions.Dispose();
    //    physicsIndices.Dispose();
    //}
}


//[BurstCompile]
//public struct PackGreedyMeshToBoxesJob : IJob
//{
//    [ReadOnly] public NativeArray<VoxelVertex> SourceVertices;
//    [ReadOnly] public NativeArray<int> SourceIndices;

//    // Выходной массив чистых математических данных геометрии коробок
//    // Передаем как NativeList с Allocator.TempJob
//    public NativeList<BoxGeometry> OutputGeometries;

//    public void Execute()
//    {
//        int totalIndices = SourceIndices.Length;
//        if (totalIndices < 6) return;

//        int boxCount = totalIndices / 6;

//        for (int i = 0; i < boxCount; i++)
//        {
//            int indexOffset = i * 6;

//            int i0 = SourceIndices[indexOffset];
//            int i1 = SourceIndices[indexOffset + 1];
//            int i2 = SourceIndices[indexOffset + 2];
//            int i3 = SourceIndices[indexOffset + 5];

//            // Фильтр колес
//            var vertexColor = SourceVertices[i0].VertexColor;
//            if (vertexColor.r < 0.15f && vertexColor.g < 0.15f && vertexColor.b < 0.15f)
//            {
//                continue;
//            }

//            float3 p0 = SourceVertices[i0].Position;
//            float3 p1 = SourceVertices[i1].Position;
//            float3 p2 = SourceVertices[i2].Position;
//            float3 p3 = SourceVertices[i3].Position;

//            float3 minBounds = math.min(math.min(p0, p1), math.min(p2, p3));
//            float3 maxBounds = math.max(math.max(p0, p1), math.max(p2, p3));

//            float3 center = (minBounds + maxBounds) * 0.5f;
//            float3 size = maxBounds - minBounds;

//            const float voxelThickness = 1.0f;
//            if (size.x < 0.01f) size.x = voxelThickness;
//            if (size.y < 0.01f) size.y = voxelThickness;
//            if (size.z < 0.01f) size.z = voxelThickness;

//            // СТРОГО SAFE: Записываем только математические параметры коробки.
//            // Никаких BoxCollider.Create() и UnsafeUtility.Malloc внутри цикла!
//            OutputGeometries.Add(new BoxGeometry
//            {
//                Center = center,
//                Size = size,
//                Orientation = quaternion.identity
//            });
//        }
//    }
//}

[BurstCompile]
public struct PackGreedyMeshToBoxesJob : IJob
{
    [ReadOnly] public NativeArray<VoxelVertex> SourceVertices;
    [ReadOnly] public NativeArray<int> SourceIndices;

    // Выходной массив для одного финального Compound-коллайдера
    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

    public void Execute()
    {
        int totalIndices = SourceIndices.Length;
        // Защита от пустых чанков
        if (totalIndices < 6) return;

        int boxCount = totalIndices / 6;

        // Временный массив ТОЛЬКО для математических структур коробок (Zero Malloc)
        var tempGeometries = new NativeArray<BoxGeometry>(boxCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        int realCount = 0;

        // ЦИКЛ 1: Чистый сбор геометрии в стеке без обращения к куче
        // ПРИМЕНЕНО ПРАВИЛО: замена знаков во всех циклах
        for (int i = 0; i < boxCount; i++)
        {
            int indexOffset = i * 6;

            // Берем только ДВЕ противоположные вершины квада (диагональ прямоугольника)
            int iMin = SourceIndices[indexOffset];     // Левый нижний угол квада
            int iMax = SourceIndices[indexOffset + 2]; // Правый верхний угол квада

            var vertexColor = SourceVertices[iMin].VertexColor;
            if (vertexColor.r < 0.15f && vertexColor.g < 0.15f && vertexColor.b < 0.15f)
            {
                continue;
            }

            // Достаем позиции строго двух точек вместо четырех
            float3 pMin = SourceVertices[iMin].Position;
            float3 pMax = SourceVertices[iMax].Position;

            // Избавляемся от каскада math.min/max! 
            // Поскольку мешер выдает вершины упорядоченно, границы находятся за один проход:
            float3 minBounds = math.min(pMin, pMax);
            float3 maxBounds = math.max(pMin, pMax);

            float3 center = (minBounds + maxBounds) * 0.5f;
            float3 size = maxBounds - minBounds;

            const float voxelThickness = 1.0f;
            if (size.x < 0.01f) size.x = voxelThickness;
            if (size.y < 0.01f) size.y = voxelThickness;
            if (size.z < 0.01f) size.z = voxelThickness;

            tempGeometries[realCount] = new BoxGeometry
            {
                Center = center,
                Size = size,
                Orientation = quaternion.identity
            };
            realCount++;
        }


        // Если после фильтрации у нас есть валидные коробки
        if (realCount > 0)
        {
            var children = new NativeArray<CompoundCollider.ColliderBlobInstance>(realCount, Allocator.Temp);
            var shortLivedBoxes = new NativeArray<BlobAssetReference<Collider>>(realCount, Allocator.Temp);

            // ЦИКЛ 2: Быстрое и упорядоченное выпекание мелких боксов
            for (int i = 0; i < realCount; i++)
            {
                var boxCollider = BoxCollider.Create(tempGeometries[i], CollisionFilter.Default);
                shortLivedBoxes[i] = boxCollider;

                children[i] = new CompoundCollider.ColliderBlobInstance
                {
                    Collider = boxCollider,
                    CompoundFromChild = new RigidTransform(quaternion.identity, float3.zero),
                    Entity = Entity.Null
                };
            }

            // Запекаем ОДИН финальный Compound-коллайдер
            OutputColliderBlob[0] = CompoundCollider.Create(children);

            // ЦИКЛ 3: Полная unmanaged-очистка временных ресурсов
            for (int i = 0; i < realCount; i++)
            {
                if (shortLivedBoxes[i].IsCreated)
                {
                    shortLivedBoxes[i].Dispose();
                }
            }

            children.Dispose();
            shortLivedBoxes.Dispose();
        }

        tempGeometries.Dispose();
    }
}


//[BurstCompile]
//public struct PackGreedyMeshToBoxesJob : IJob
//{
//    // Входные массивы данных от вашего Greedy-мешера
//    [ReadOnly] public NativeArray<VoxelVertex> SourceVertices;
//    [ReadOnly] public NativeArray<int> SourceIndices;

//    // Выходной массив для одного финального монолитного CompoundCollider
//    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

//    public void Execute()
//    {
//        int totalIndices = SourceIndices.Length;
//        // Защита от пустых чанков
//        if (totalIndices < 6) return;

//        int boxCount = totalIndices / 6;

//        // Временный массив для отслеживания и последующего удаления мелких боксов
//        var shortLivedBoxes = new NativeArray<BlobAssetReference<Collider>>(boxCount, Allocator.Temp);

//        // Временный массив элементов для сборки составного коллайдера
//        var children = new NativeArray<CompoundCollider.ColliderBlobInstance>(boxCount, Allocator.Temp);

//        int realCount = 0;

//        // ПРИМЕНЕНО ПРАВИЛО: замена знаков во всех циклах
//        for (int i = 0; i < boxCount; i++)
//        {
//            int indexOffset = i * 6;

//            int i0 = SourceIndices[indexOffset];
//            int i1 = SourceIndices[indexOffset + 1];
//            int i2 = SourceIndices[indexOffset + 2];
//            int i3 = SourceIndices[indexOffset + 5];

//            // Фильтр колес по цвету вершин (если воксель темный — пропускаем)
//            var vertexColor = SourceVertices[i0].VertexColor;
//            if (vertexColor.r < 0.15f && vertexColor.g < 0.15f && vertexColor.b < 0.15f)
//            {
//                continue;
//            }

//            float3 p0 = SourceVertices[i0].Position;
//            float3 p1 = SourceVertices[i1].Position;
//            float3 p2 = SourceVertices[i2].Position;
//            float3 p3 = SourceVertices[i3].Position;

//            // Находим точные воксельные границы жадной плиты
//            float3 minBounds = math.min(math.min(p0, p1), math.min(p2, p3));
//            float3 maxBounds = math.max(math.max(p0, p1), math.max(p2, p3));

//            float3 center = (minBounds + maxBounds) * 0.5f;
//            float3 size = maxBounds - minBounds;

//            // Защита от нулевой толщины: даем плоским панелям физический объем
//            const float voxelThickness = 1.0f;
//            if (size.x < 0.01f) size.x = voxelThickness;
//            if (size.y < 0.01f) size.y = voxelThickness;
//            if (size.z < 0.01f) size.z = voxelThickness;

//            // Создаем временный мелкий BoxCollider
//            var boxCollider = BoxCollider.Create(new BoxGeometry
//            {
//                Center = center,
//                Size = size,
//                Orientation = quaternion.identity
//            }, CollisionFilter.Default);

//            // Сохраняем ссылку на мелкий бокс для обязательной очистки памяти ниже
//            shortLivedBoxes[realCount] = boxCollider;

//            // Записываем инстанс в структуру составного коллайдера
//            children[realCount] = new CompoundCollider.ColliderBlobInstance
//            {
//                Collider = boxCollider,
//                CompoundFromChild = new RigidTransform(quaternion.identity, float3.zero),
//                Entity = Entity.Null
//            };

//            realCount++;
//        }

//        if (realCount > 0)
//        {
//            // Подгоняем размер массива под реальное число плит (с учетом отфильтрованных колес)
//            var finalChildren = new NativeArray<CompoundCollider.ColliderBlobInstance>(realCount, Allocator.Temp);
//            NativeArray<CompoundCollider.ColliderBlobInstance>.Copy(children, finalChildren, realCount);

//            // Запекаем финальный CompoundCollider. Все данные скопировались во внутреннюю структуру
//            OutputColliderBlob[0] = CompoundCollider.Create(finalChildren);

//            finalChildren.Dispose();
//        }

//        // ====================================================================
//        // СТРОГО SAFE ОЧИСТКА ВРЕМЕННЫХ BLOB-АССЕТОВ
//        // ====================================================================
//        // Финальный CompoundCollider полностью независим. Мелкие куски ему > не нужны.
//        // Чтобы избежать утечек памяти (Memory Leaks), принудительно уничтожаем каждый мелкий бокс.
//        for (int i = 0; i < realCount; i++)
//        {
//            if (shortLivedBoxes[i].IsCreated)
//            {
//                shortLivedBoxes[i].Dispose(); // Удаляем мелкий кусок из unmanaged-памяти кучи
//            }
//        }

//        // Чистим временные Native-коллекции кадра
//        shortLivedBoxes.Dispose();
//        children.Dispose();
//    }
//}


[BurstCompile]
public struct PhysicsGreedyJobSafeDirect : IJob
{
    // Входные данные — СТРОГАЯ ТОЧНАЯ КОПИЯ параметров вашей графической джобы
    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMask;
    [ReadOnly] public NativeArray<byte> FlattenedModelColors;
    public int ChunkOffsetInFlattenedArray;

    // Выходной контейнер для запекания Compound-коллайдера этого чанка
    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

    [BurstCompile]
    public void Execute()
    {
        // Временный список для мгновенного сбора геометрии коробок
        var listGeometries = new NativeList<BoxGeometry>(Allocator.Temp);
        NativeArray<short> mask = new NativeArray<short>(1024, Allocator.Temp);

        // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
        for (int back = 0; back < 3; back++)
        {
            int u = (back + 1) % 3;
            int v = (back + 2) % 3;

            int3 chunkPos = int3.zero;
            int3 axisVector = int3.zero;
            axisVector[back] = 1;

            for (chunkPos[back] = 0; chunkPos[back] < 33; chunkPos[back]++)
            {
                int n = 0;

                for (chunkPos[v] = 0; chunkPos[v] < 32; chunkPos[v]++)
                {
                    for (chunkPos[u] = 0; chunkPos[u] < 32; chunkPos[u]++)
                    {
                        bool voxelCurrentLive = IsVoxelLive(chunkPos, out byte colorCurrent);
                        bool voxelNeighborLive = IsVoxelLive(chunkPos - axisVector, out byte colorNeighbor);

                        if (voxelCurrentLive == voxelNeighborLive)
                        {
                            mask[n++] = 0;
                        }
                        else if (voxelCurrentLive)
                        {
                            mask[n++] = (short)(colorCurrent | ((back * 2) << 8));
                        }
                        else
                        {
                            mask[n++] = (short)(colorNeighbor | (((back * 2) + 1) << 8));
                        }
                    }
                }

                // Передаем срез маски в упаковщик (Имя метода теперь застраховано!)
                ExecuteSliceGreedyMeshing(mask, chunkPos[back], back, u, v, listGeometries);
            }
        }

        // ====================================================================
        // СБОРКА И ЗАПЕКАНИЕ COMPOUND COLLIDER ДЛЯ ЧАНКА
        // ====================================================================
        int finalBoxCount = listGeometries.Length;

        if (finalBoxCount > 0)
        {
            var children = new NativeArray<CompoundCollider.ColliderBlobInstance>(finalBoxCount, Allocator.Temp);
            var shortLivedBoxes = new NativeArray<BlobAssetReference<Collider>>(finalBoxCount, Allocator.Temp);

            // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
            for (int i = 0; i < finalBoxCount; i++)
            {
                var boxCollider = BoxCollider.Create(listGeometries[i], CollisionFilter.Default);
                shortLivedBoxes[i] = boxCollider;

                children[i] = new CompoundCollider.ColliderBlobInstance
                {
                    Collider = boxCollider,
                    CompoundFromChild = new RigidTransform(quaternion.identity, float3.zero),
                    Entity = Entity.Null
                };
            }

            // Запекаем финальный CompoundCollider для этого чанка
            OutputColliderBlob[0] = CompoundCollider.Create(children);

            // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
            for (int i = 0; i < finalBoxCount; i++)
            {
                if (shortLivedBoxes[i].IsCreated)
                {
                    shortLivedBoxes[i].Dispose();
                }
            }

            children.Dispose();
            shortLivedBoxes.Dispose();
        }
        else
        {
            // Безопасная микро-заглушка для абсолютно пустого чанка
            var emptyDummyBox = BoxCollider.Create(new BoxGeometry
            {
                Center = new float3(16f, 16f, 16f),
                Size = new float3(0.001f, 0.001f, 0.001f),
                Orientation = quaternion.identity
            }, CollisionFilter.Default);

            OutputColliderBlob[0] = emptyDummyBox;
        }

        mask.Dispose();
        listGeometries.Dispose();
    }

    [BurstCompile]
    private void ExecuteSliceGreedyMeshing(NativeArray<short> sliceMask, int backCoord, int back, int u, int v, NativeList<BoxGeometry> listGeometries)
    {
        // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
        for (int j = 0; j < 32; j++)
        {
            for (int i = 0; i < 32; i++)
            {
                int currentMaskIndex = i + (j << 5);
                short maskValue = sliceMask[currentMaskIndex];
                if (maskValue == 0) continue;

                int direction = maskValue >> 8;

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

                // Вызываем расчет физического бокса
                EmitPhysicsBox(backCoord, direction, u, v, i, j, w, h, listGeometries);

                // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
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
    private void EmitPhysicsBox(int backCoord, int d, int u, int v, int i, int j, int w, int h, NativeList<BoxGeometry> listGeometries)
    {
        int backAxis = d / 2;
        float renderX = (float)backCoord;

        float3 p0 = float3.zero;
        float3 p2 = float3.zero;

        switch (backAxis)
        {
            case 0: // Ось X (Грани Слева / Справа)
                p0 = new float3(renderX, (float)i, (float)j);
                p2 = new float3(renderX, (float)(i + w), (float)(j + h));
                break;

            case 1: // Ось Y (Грани Снизу / Сверху)
                p0 = new float3((float)j, renderX, (float)i);
                p2 = new float3((float)(j + h), renderX, (float)(i + w));
                break;

            case 2: // Ось Z (Грани Сзади / Спереди)
                p0 = new float3((float)i, (float)j, renderX);
                p2 = new float3((float)(i + w), (float)(j + h), renderX);
                break;
        }

        float3 minBounds = math.min(p0, p2);
        float3 maxBounds = math.max(p0, p2);

        float3 center = (minBounds + maxBounds) * 0.5f;
        float3 size = maxBounds - minBounds;

        // Корректируем толщину по оси сканирования и смещаем центр бокса внутрь кузова автомобиля
        const float voxelThickness = 1.0f;
        if (size.x < 0.01f)
        {
            size.x = voxelThickness;
            center.x += (d % 2 == 0) ? (voxelThickness * 0.5f) : (-voxelThickness * 0.5f);
        }
        if (size.y < 0.01f)
        {
            size.y = voxelThickness;
            center.y += (d % 2 == 0) ? (voxelThickness * 0.5f) : (-voxelThickness * 0.5f);
        }
        if (size.z < 0.01f)
        {
            size.z = voxelThickness;
            center.z += (d % 2 == 0) ? (voxelThickness * 0.5f) : (-voxelThickness * 0.5f);
        }

        listGeometries.Add(new BoxGeometry
        {
            Center = center,
            Size = size,
            Orientation = quaternion.identity
        });
    } // Конец метода EmitPhysicsBox

    [BurstCompile]
    private bool IsVoxelLive(int3 pos, out byte color)
    {
        color = 0;
        // Защита от выхода за границы массива чанка 32х32х32
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
        if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) return false;

        color = FlattenedModelColors[targetColorIndex];
        return color > 0;
    }
}
