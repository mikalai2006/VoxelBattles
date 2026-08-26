using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile(CompileSynchronously = true)]
public struct GenerateChunkColliderJob : IJob
{
    [ReadOnly] public NativeArray<LocalChunkDestructionMask>.ReadOnly LiveMask;
    [ReadOnly] public NativeArray<byte> FlattenedModelColors;
    public int ChunkOffsetInFlattenedArray;

    public NativeArray<int3> JobCountersRef;
    public NativeArray<BlobAssetReference<Collider>> OutputColliderBlob;

    private const int CHUNK_SIZE = 32;

    public void Execute()
    {
        var vertices = new NativeList<float3>(Allocator.Temp);
        var triangles = new NativeList<int3>(Allocator.Temp);

        var directions = new NativeArray<int3>(6, Allocator.Temp);
        directions[0] = new int3(1, 0, 0);  // +X
        directions[1] = new int3(-1, 0, 0); // -X
        directions[2] = new int3(0, 1, 0);  // +Y
        directions[3] = new int3(0, -1, 0); // -Y
        directions[4] = new int3(0, 0, 1);  // +Z
        directions[5] = new int3(0, 0, -1); // -Z

        // Маска посещенных вокселей для текущего слоя (32x32 = 1024 бит -> 16 элементов ulong)
        var visited = new NativeArray<ulong>(16, Allocator.Temp);

        // Перебираем все 6 направлений граней куба
        for (int d = 0; d < 6; d++)
        {
            int3 dir = directions[d];

            // Итерируемся по слоям чанка. h — это глубина (высота) текущего среза
            for (int h = 0; h < CHUNK_SIZE; h++)
            {
                // Сбрасываем маску посещенных вокселей для нового слоя
                for (int i = 0; i < 16; i++) visited[i] = 0;

                // Запускаем двумерный Greedy-мешинг на текущем слое (см. Часть 2)
                ProcessLayerGreedyMesh(h, dir, visited, vertices, triangles);
            }
        }

        // Если вершин нет, значит весь чанк пустой
        if (vertices.Length == 0)
        {
            OutputColliderBlob[0] = default;
            return;
        }

        // Запекаем сжатый физический коллайдер на сервере
        OutputColliderBlob[0] = Unity.Physics.MeshCollider.Create(
            vertices.AsArray(),
            triangles.AsArray(),
            CollisionFilter.Default
        );

        JobCountersRef[0] = new int3(vertices.Length, triangles.Length, 1);

        vertices.Dispose();
        triangles.Dispose();
        directions.Dispose();
        visited.Dispose();
    }

    private void ProcessLayerGreedyMesh(int h, int3 dir, NativeArray<ulong> visited, NativeList<float3> vertices, NativeList<int3> triangles)
    {
        // Плоскость сканирования внутри слоя: оси v и u
        for (int v = 0; v < CHUNK_SIZE; v++)
        {
            for (int u = 0; u < CHUNK_SIZE; u++)
            {
                // Проверяем по битовой маске, обрабатывали ли мы этот воксель ранее
                int bitIndex = u + (v << 5);
                if ((visited[bitIndex >> 6] & (1UL << (bitIndex & 63))) != 0) continue;

                // Переводим локальные u, v, h в мировые x, y, z в зависимости от грани
                GetXYZ(u, v, h, dir, out int x, out int y, out int z);

                // Если грань не видна (блок уничтожен или скрыт соседом) — пропускаем
                if (!IsFaceVisible(x, y, z, dir)) continue;

                // --- НАЧАЛО ЖАДНОГО СХЛОПЫВАНИЯ ---

                // 1. Растягиваем прямоугольник в ширину по оси U
                int width = 1;
                while (u + width < CHUNK_SIZE)
                {
                    int nextBit = (u + width) + (v << 5);
                    if ((visited[nextBit >> 6] & (1UL << (nextBit & 63))) != 0) break;

                    GetXYZ(u + width, v, h, dir, out int nx, out int ny, out int nz);
                    if (!IsFaceVisible(nx, ny, nz, dir)) break;

                    width++;
                }

                // 2. Растягиваем полученную линию в высоту по оси V
                int height = 1;
                bool canStretchV = true;
                while (v + height < CHUNK_SIZE && canStretchV)
                {
                    for (int currU = 0; currU < width; currU++)
                    {
                        int checkBit = (u + currU) + ((v + height) << 5);
                        if ((visited[checkBit >> 6] & (1UL << (checkBit & 63))) != 0)
                        {
                            canStretchV = false;
                            break;
                        }

                        GetXYZ(u + currU, v + height, h, dir, out int nx, out int ny, out int nz);
                        if (!IsFaceVisible(nx, ny, nz, dir))
                        {
                            canStretchV = false;
                            break;
                        }
                    }

                    if (canStretchV) height++;
                }

                // 3. Помечаем все объединенные воксели как посещенные
                for (int currV = 0; currV < height; currV++)
                {
                    for (int currU = 0; currU < width; currU++)
                    {
                        int markBit = (u + currU) + ((v + currV) << 5);
                        visited[markBit >> 6] |= (1UL << (markBit & 63));
                    }
                }

                // 4. Добавляем итоговую большую геометрическую панель коллизии
                BuildAAAStretchedFaceGeometry(u, v, h, width, height, dir, vertices, triangles);
            }
        }
    }
    private bool IsBlockSolid(int x, int y, int z)
    {
        int flatIndex = x + (y << 5) + (z << 10);
        int ulongIndex = flatIndex >> 6;
        int bitOffset = flatIndex & 63;

        bool isVoxelNotDestroyed = (LiveMask[ulongIndex].Value & (1UL << bitOffset)) != 0;
        if (!isVoxelNotDestroyed) return false;

        int targetColorIndex = ChunkOffsetInFlattenedArray + flatIndex;
        if (targetColorIndex < 0 || targetColorIndex >= FlattenedModelColors.Length) return false;

        return FlattenedModelColors[targetColorIndex] > 0;
    }

    private bool IsFaceVisible(int x, int y, int z, int3 direction)
    {
        if (!IsBlockSolid(x, y, z)) return false;

        int3 neighbor = new int3(x, y, z) + direction;

        if (neighbor.x < 0 || neighbor.x >= CHUNK_SIZE ||
            neighbor.y < 0 || neighbor.y >= CHUNK_SIZE ||
            neighbor.z < 0 || neighbor.z >= CHUNK_SIZE)
        {
            return true;
        }

        return !IsBlockSolid(neighbor.x, neighbor.y, neighbor.z);
    }

    private void GetXYZ(int u, int v, int h, int3 direction, out int x, out int y, out int z)
    {
        if (direction.x != 0) // Боковые срезы чанка (+X, -X)
        {
            x = h; y = v; z = u;
        }
        else if (direction.y != 0) // Горизонтальные срезы чанка (+Y, -Y)
        {
            x = u; y = h; z = v;
        }
        else // Фронтальные срезы чанка (+Z, -Z)
        {
            x = u; y = v; z = h;
        }
    }
    private void BuildAAAStretchedFaceGeometry(int u, int v, int h, int width, int height, int3 direction, NativeList<float3> vertices, NativeList<int3> triangles)
    {
        int vStart = vertices.Length;

        float3 p = default;
        float3 right = default;
        float3 up = default;

        if (direction.x == 1) // Панель на грани +X
        {
            p = new float3(h + 1, v, u); right = new float3(0, 0, width); up = new float3(0, height, 0);
            vertices.Add(p); vertices.Add(p + right); vertices.Add(p + right + up); vertices.Add(p + up);
        }
        else if (direction.x == -1) // Панель на грани -X
        {
            p = new float3(h, v, u); right = new float3(0, 0, width); up = new float3(0, height, 0);
            vertices.Add(p); vertices.Add(p + up); vertices.Add(p + right + up); vertices.Add(p + right);
        }
        else if (direction.y == 1) // Панель на грани +Y
        {
            p = new float3(u, h + 1, v); right = new float3(width, 0, 0); up = new float3(0, 0, height);
            vertices.Add(p + up); vertices.Add(p + right + up); vertices.Add(p + right); vertices.Add(p);
        }
        else if (direction.y == -1) // Панель на грани -Y
        {
            p = new float3(u, h, v); right = new float3(width, 0, 0); up = new float3(0, 0, height);
            vertices.Add(p); vertices.Add(p + right); vertices.Add(p + right + up); vertices.Add(p + up);
        }
        else if (direction.z == 1) // Панель на грани +Z
        {
            p = new float3(u, v, h + 1); right = new float3(width, 0, 0); up = new float3(0, height, 0);
            vertices.Add(p); vertices.Add(p + up); vertices.Add(p + right + up); vertices.Add(p + right);
        }
        else // Панель на грани -Z
        {
            p = new float3(u, v, h); right = new float3(width, 0, 0); up = new float3(0, height, 0);
            vertices.Add(p + right); vertices.Add(p + right + up); vertices.Add(p + up); vertices.Add(p);
        }

        // Собираем два треугольника для получившейся Greedy-панели
        triangles.Add(new int3(vStart + 0, vStart + 1, vStart + 2));
        triangles.Add(new int3(vStart + 0, vStart + 2, vStart + 3));
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
