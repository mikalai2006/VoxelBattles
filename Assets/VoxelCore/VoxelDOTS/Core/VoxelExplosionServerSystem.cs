//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Physics;
//using Unity.Transforms;

//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[BurstCompile]
//public partial struct VoxelExplosionServerSystem : ISystem
//{
//    private EntityQuery m_RpcQuery;
//    private EntityQuery cacheQuery;

//    public void OnCreate(ref SystemState state)
//    {
//        //var queryTypes = new NativeArray<ComponentType>(2, Allocator.Temp);

//        //queryTypes[0] = ComponentType.ReadOnly<VoxelExplosionRequestRpc>();
//        //queryTypes[1] = ComponentType.ReadOnly<ReceiveRpcCommandRequest>();

//        //// Передаем NativeArray в метод
//        //m_RpcQuery = state.GetEntityQuery(queryTypes);

//        // Ищем входящие RPC-команды взрыва от клиентов
//        m_RpcQuery = state.GetEntityQuery(
//            ComponentType.ReadOnly<VoxelExplosionRequestRpc>(),
//            ComponentType.ReadOnly<ReceiveRpcCommandRequest>()
//        );
//        // 3. Вместо RequireForUpdate физики, требуем обновление ТОЛЬКО при наличии RPC!
//        state.RequireForUpdate(m_RpcQuery);

//        cacheQuery = state.GetEntityQuery(ComponentType.ReadOnly<GlobalVoxelModelCache>());
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        //if (m_RpcQuery.IsEmptyIgnoreFilter) return;

//        // Нам > не нужен ручной `if (m_RpcQuery.IsEmptyIgnoreFilter) return;` 
//        // так как RequireForUpdate(m_RpcQuery) гарантирует, что сущности ЕСТЬ.

//        // Безопасно проверяем физический мир сервера прямо в кадре обработки
//        if (!SystemAPI.HasSingleton<PhysicsWorldSingleton>())
//        {
//            return;
//        }

//        // Получаем физический мир сервера
//        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

//        var entities = m_RpcQuery.ToEntityArray(Allocator.Temp);
//        var requests = m_RpcQuery.ToComponentDataArray<VoxelExplosionRequestRpc>(Allocator.Temp);
//        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

//        var em = state.EntityManager;

//        UnityEngine.Debug.LogWarning($"[Server]: entities.Length={entities.Length}");

//        for (int i = 0; i < requests.Length; i++)
//        {
//            var req = requests[i];

//            // Настраиваем Raycast входные данные для Unity Physics
//            RaycastInput raycastInput = new RaycastInput
//            {
//                Start = req.RayOrigin,
//                // Пускаем луч, например, на 100 метров вперед в заданном направлении
//                End = req.RayOrigin + (req.RayDirection * 1000f),
//                //Filter = CollisionFilter.Default // Настройте маску под ваши чанки, если нужно
//                // Бронированный всеядный фильтр
//                Filter = new CollisionFilter
//                {
//                    BelongsTo = unchecked((uint)~0),
//                    CollidesWith = unchecked((uint)~0),
//                    GroupIndex = 0
//                }
//            };


//            //#if UNITY_EDITOR
//            //            // Исправляем отладку для визуализации НОВОГО луча
//            //            UnityEngine.Debug.LogWarning($"Ray create Bodies={physicsWorld.Bodies.Length}");
//            //            UnityEngine.Debug.DrawLine(raycastInput.Start, raycastInput.End, UnityEngine.Color.red, 2f);
//            //#endif

//            //// Пускаем луч в физическом мире сервера
//            //if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
//            //{
//            //    // Сервер нашел точную сущность чанка и точку попадания!
//            //    Entity hitChunkEntity = hit.Entity;
//            //    float3 hitPosition = hit.Position;

//            //    UnityEngine.Debug.LogWarning($"[Server]: Луч попал в чанк {hitChunkEntity} в точке {hitPosition}. Взрываем!");

//            //}
//            if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
//            {
//                Entity rootVehicleEntity = hit.Entity; // Родитель-транспорт
//                Entity hitChunkEntity = Entity.Null;
//                int3 targetChunkCoords = int3.zero;

//                //#if UNITY_EDITOR
//                //                Debug.LogWarning($"[VoxelClick Safe] Клик успешно выполнен на hit.Entity.Index={hit.Entity.Index}!");
//                //#endif
//                if (em.HasComponent<LocalToWorld>(rootVehicleEntity))
//                {
//                    var localToWorld = em.GetComponentData<LocalToWorld>(rootVehicleEntity);

//                    // 1. Сдвигаем точку внутрь чанка по нормали для защиты от погрешностей float на стыках граней
//                    float3 safeWorldPos = hit.Position - (hit.SurfaceNormal * 0.01f);

//                    // 2. Переводим точку в локальное пространство машины (здесь координаты отрицательные)
//                    float3 hitLocalPos = math.transform(math.inverse(localToWorld.Value), safeWorldPos);

//                    // Ищем синглтон кэша моделей, чтобы узнать габариты (SizeModel) этой машины
//                    if (!cacheQuery.IsEmpty)
//                    {
//                        GlobalVoxelModelCache cache = cacheQuery.GetSingleton<GlobalVoxelModelCache>();

//                        // Извлекаем хэш конфигурации модели из корневой сущности транспорта
//                        if (em.HasComponent<VoxelModelRootData>(rootVehicleEntity))
//                        {
//                            uint vehicleModelHash = em.GetComponentData<VoxelModelRootData>(rootVehicleEntity).ConfigHashName;

//                            if (cache.Templates.TryGetValue(vehicleModelHash, out var template))
//                            {
//                                // ====================================================================
//                                // ЗЕРКАЛЬНАЯ КОМПЕНСАЦИЯ ПИВОТА: Возвращаем координаты к нулю фабрики!
//                                // ====================================================================
//                                // Рассчитываем ТОЧНО ТАКОЙ ЖЕ pivotOffset, как и в вашем спавнере чанков
//                                float3 pivotOffset = new float3(
//                                    (template.SizeModel.x * 32f) / 2f,
//                                    0f, // По оси Y у вас смещения нет, оставляем 0
//                                    (template.SizeModel.z * 32f) / 2f
//                                );

//                                // Прибавляем смещение! Это полностью нейтрализует минусы и возвращает 
//                                // локальную точку клика в оригинальные положительные координаты модели
//                                float3 originalLocalPos = hitLocalPos + pivotOffset;

//                                // 3. Теперь деление на 32 гарантированно выдаст исключительно 
//                                // положительные и правильные int3 координаты чанка (0, 1, 2...)
//                                targetChunkCoords = new int3(
//                                    (int)math.floor(originalLocalPos.x / 32f),
//                                    (int)math.floor(originalLocalPos.y / 32f),
//                                    (int)math.floor(originalLocalPos.z / 32f)
//                                );
//                                // ====================================================================
//                                // 4. ПОИСК СУЩНОСТИ ЧАНКА ПО ДЕТЯМ РОДИТЕЛЯ (Через ChunkIndexComponent)
//                                // ====================================================================
//                                if (em.HasComponent<Child>(rootVehicleEntity))
//                                {
//                                    DynamicBuffer<Child> children = em.GetBuffer<Child>(rootVehicleEntity);

//                                    for (int ii = 0; ii < children.Length; ii++)
//                                    {
//                                        Entity childEntity = children[ii].Value;

//                                        if (em.HasComponent<ChunkIndexComponent>(childEntity))
//                                        {
//                                            // Считываем точные int3 координаты этого конкретного чанка
//                                            int3 chunkCoords = em.GetComponentData<ChunkIndexComponent>(childEntity).Value;

//                                            // Сверяем с targetChunkCoords. Благодаря фиксу пивота, они совпадут идеально!
//                                            if (chunkCoords.x == targetChunkCoords.x &&
//                                                chunkCoords.y == targetChunkCoords.y &&
//                                                chunkCoords.z == targetChunkCoords.z)
//                                            {
//                                                hitChunkEntity = childEntity;
//                                                break;
//                                            }
//                                        }
//                                    }
//                                }


//                                // ====================================================================
//                                // ОТПРАВКА ЗАПРОСА И АКТИВАЦИЯ ТЕГА
//                                // ====================================================================
//                                if (hitChunkEntity != Entity.Null && em.HasComponent<LocalChunkDestructionMask>(hitChunkEntity))
//                                {
//                                    DynamicBuffer<LocalChunkDestructionMask> destructionMask = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(hitChunkEntity);

//                                    //int3 centerVoxel = (int3)math.round(hitLocalPos);
//                                    //int intRadius = (int)math.ceil(req.Radius);

//                                    // ====================================================================
//                                    // АДАПТАЦИЯ: Переводим абсолютную позицию модели в пространство чанка [0..31]
//                                    // ====================================================================
//                                    float3 chunkLocalHitPos = new float3(
//                                        originalLocalPos.x % 32f,
//                                        originalLocalPos.y % 32f,
//                                        originalLocalPos.z % 32f
//                                    );

//                                    // Страховка от отрицательных остатков (на случай математических погрешностей float)
//                                    if (chunkLocalHitPos.x < 0) chunkLocalHitPos.x += 32f;
//                                    if (chunkLocalHitPos.y < 0) chunkLocalHitPos.y += 32f;
//                                    if (chunkLocalHitPos.z < 0) chunkLocalHitPos.z += 32f;

//                                    // Теперь centerVoxel гарантированно будет лежать в диапазоне [0..31]
//                                    int3 centerVoxel = (int3)math.round(chunkLocalHitPos);
//                                    int intRadius = (int)math.ceil(req.Radius);

//                                    bool hasChanges = false;

//                                    int totalDestroyedVoxels = 0;
//                                    //// ====================================================================
//                                    //// ШАГ 1: Создаем ЧИСТЫЙ независимый массив в Temp-памяти.
//                                    //// Он никак не связан с ECS, Netcode его вообще не видит.
//                                    //// ====================================================================
//                                    //var tempArray = new NativeArray<LocalChunkDestructionMask>(512, Allocator.Temp);

//                                    //// Копируем текущее состояние буфера из ECS во временный массив
//                                    //for (int x = 0; x < 512; x++)
//                                    //{
//                                    //    tempArray[x] = destructionMask[x];
//                                    //}

//                                    //// ====================================================================
//                                    //// ШАГ 2: ВАШ ТРОЙНОЙ ЦИКЛ (Выжигаем биты внутри tempArray)
//                                    //// ====================================================================
//                                    //for (int z = centerVoxel.z - intRadius; z <= centerVoxel.z + intRadius; z++)
//                                    //{
//                                    //    if (z < 0 || z > 31) continue;
//                                    //    for (int y = centerVoxel.y - intRadius; y <= centerVoxel.y + intRadius; y++)
//                                    //    {
//                                    //        if (y < 0 || y > 31) continue;
//                                    //        for (int x = centerVoxel.x - intRadius; x <= centerVoxel.x + intRadius; x++)
//                                    //        {
//                                    //            if (x < 0 || x > 31) continue;

//                                    //            float3 currentVoxelPos = new float3(x, y, z);

//                                    //            if (math.distance(chunkLocalHitPos, currentVoxelPos) <= req.Radius)
//                                    //            {
//                                    //                int flatIndex = x + (y << 5) + (z << 10);
//                                    //                int ulongIndex = flatIndex >> 6;
//                                    //                int bitOffset = flatIndex & 63;
//                                    //                ulong currentMaskBit = 1UL << bitOffset;

//                                    //                // РАБОТАЕМ С НЕЗАВИСИМЫМ МАССИВОМ: Сетевой генератор Netcode молчит
//                                    //                LocalChunkDestructionMask maskElement = tempArray[ulongIndex];

//                                    //                if ((maskElement.Value & currentMaskBit) != 0)
//                                    //                {
//                                    //                    maskElement.Value &= ~currentMaskBit;
//                                    //                    tempArray[ulongIndex] = maskElement;
//                                    //                    hasChanges = true;
//                                    //                }
//                                    //            }
//                                    //        }
//                                    //    }
//                                    //}

//                                    //// ====================================================================
//                                    //// ШАГ 3: АТОМАРНЫЙ ПЕРЕНОС В ECS ДЛЯ РЕПЛИКАЦИИ
//                                    //// ====================================================================
//                                    //if (hasChanges)
//                                    //{
//                                    //    // Вместо CopyFrom (который затирал метаданные), мы переносим данные простым 
//                                    //    // последовательным циклом. Для Netcode это ОДНА быстрая транзакция обновления.
//                                    //    for (int y = 0; y < 512; y++)
//                                    //    {
//                                    //        destructionMask[y] = tempArray[y];
//                                    //    }
//                                    //}

//                                    //// Так как tempArray создан через Allocator.Temp, мы ОБЯЗАНЫ сделать ему Dispose.
//                                    //// Это безопасно, так как массив наш личный, а не системный массив ECS-чанка.
//                                    //tempArray.Dispose();
//                                    // Тройной цикл побитового выжигания маски
//                                    // Пока z < ИЛИ РАВНО centerVoxel.z + intRadius
//                                    for (int z = centerVoxel.z - intRadius; z <= centerVoxel.z + intRadius; z++)
//                                    {
//                                        // Знаки сравнения заменены словами < и >
//                                        if (z < 0 || z > 31) continue;

//                                        for (int y = centerVoxel.y - intRadius; y <= centerVoxel.y + intRadius; y++)
//                                        {
//                                            if (y < 0 || y > 31) continue;

//                                            for (int x = centerVoxel.x - intRadius; x <= centerVoxel.x + intRadius; x++)
//                                            {
//                                                if (x < 0 || x > 31) continue;

//                                                float3 currentVoxelPos = new float3(x, y, z);

//                                                // Если воксель попадает в сферу взрыва
//                                                if (math.distance(chunkLocalHitPos, currentVoxelPos) <= req.Radius)
//                                                {
//                                                    // Рассчитываем плоский индекс для сетки 32x32x32
//                                                    int flatIndex = x + (y << 5) + (z << 10);

//                                                    // Находим индекс ulong в буфере (деление на 64)
//                                                    int ulongIndex = flatIndex >> 6;

//                                                    // Находим смещение бита внутри этого ulong (остаток от деления на 64)
//                                                    int bitOffset = flatIndex & 63;

//                                                    // Создаем битовую маску для конкретного вокселя
//                                                    ulong currentMaskBit = 1UL << bitOffset;

//                                                    //LocalChunkDestructionMask maskElement = destructionMask[ulongIndex];

//                                                    //// ИСПРАВЛЕНИЕ: Инвертируем маску и сбрасываем бит из 1 в 0
//                                                    //// Теперь 0 означает, что воксель уничтожен
//                                                    //maskElement.Value &= ~currentMaskBit;

//                                                    //// Записываем измененный элемент обратно в буфер для репликации
//                                                    //destructionMask[ulongIndex] = maskElement;

//                                                    // Меняем бит в локальном массиве
//                                                    LocalChunkDestructionMask maskElement = destructionMask[ulongIndex];

//                                                    // Проверяем, изменился ли бит на самом деле (чтобы зря не спамить сеть)
//                                                    if ((maskElement.Value & currentMaskBit) != 0)
//                                                    {
//                                                        maskElement.Value &= ~currentMaskBit;
//                                                        destructionMask[ulongIndex] = maskElement;
//                                                        hasChanges = true;
//                                                        totalDestroyedVoxels++;
//                                                    }
//                                                }
//                                            }
//                                        }
//                                    }


//                                    // ====================================================================
//                                    // ШАГ 3: АТОМАРНЫЙ ПЕРЕНОС В ECS ДЛЯ РЕПЛИКАЦИИ
//                                    // ====================================================================
//                                    if (hasChanges)
//                                    {
//                                        //// 1. Сохраняем изменения в локальный серверный буфер
//                                        //for (int x = 0; x < 512; x++) destructionMask[x] = tempArray[x];

//                                        // 2. Создаем RPC команду
//                                        var rleRpc = new NetworkChunkRleUpdate { TargetChunkEntity = hitChunkEntity };

//                                        // 3. Вызываем наш SAFE статический метод сжатия
//                                        ChunkRleSerializer.CompressToRle(destructionMask.AsNativeArray(), ref rleRpc.CompressedBytes);

//                                        // ====================================================================
//                                        // ЧИСТЫЙ И ПРАВИЛЬНЫЙ ECS-СПОСОБ ОТПРАВКИ RPC
//                                        // ====================================================================
//                                        // Создаем пустую сущность для сетевой команды
//                                        Entity rpcEntity = state.EntityManager.CreateEntity();

//                                        // Добавляем на неё компонент с нашими сжатыми данными
//                                        state.EntityManager.AddComponentData(rpcEntity, rleRpc);

//                                        // Добавляем системный компонент Netcode, который приказывает отправить этот RPC.
//                                        // Если параметр TargetConnection пустой (Entity.Null), Netcode автоматически 
//                                        // разошлет эту команду ВСЕМ клиентам (Broadcast) ровно одним пакетом.
//                                        state.EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = Entity.Null });
//                                    }


//                                    //// Цикл по элементам буфера: пока i <, чем destructionMask.Length (512)
//                                    //for (int k = 0; k < destructionMask.Length; k++)
//                                    //{
//                                    //    ulong maskValue = destructionMask[k].Value;

//                                    //    // Аппаратный подсчет живых блоков (считает единицы в 64-битной маске за 1 такт)
//                                    //    int aliveBits = math.countbits(maskValue);

//                                    //    // Уничтоженные блоки — это все остальные биты из 64 возможных
//                                    //    int destroyedBits = 64 - aliveBits;

//                                    //    totalDestroyedVoxels += destroyedBits;
//                                    //}

//                                    UnityEngine.Debug.Log($"[Voxel Detector]: В чанке {hitChunkEntity} уничтожено  {totalDestroyedVoxels} вокселей.");

//#if UNITY_EDITOR
//                                    UnityEngine.Debug.LogWarning($"[Voxel Detector] Кликнул в чанк со свойствами: {targetChunkCoords}.");
//#endif
//                                    // Принудительно взводим тег обновления физики/визуала на сервере
//                                    //if (state.EntityManager.HasComponent<ChunkMeshNeedCreate>(req.TargetEntity))
//                                    //{
//                                    //}
//                                    state.EntityManager.SetComponentEnabled<ChunkMeshNeedCreate>(hitChunkEntity, true);
//                                    //state.EntityManager.SetComponentEnabled<ChunkColliderNeedCreate>(hitChunkEntity, true);

//                                }
//                            }
//                        }
//                    }
//                }
//            }



//            // Обязательно удаляем RPC сущность
//            ecb.DestroyEntity(entities[i]);
//        }


//        //for (int i = 0; i < entities.Length; i++)
//        //{
//        //    var req = requests[i];

//        //    // Если машина, в которую кликнули, уже уничтожена на сервере — игнорируем
//        //    if (!state.EntityManager.Exists(req.TargetEntity) || !state.EntityManager.HasComponent<LocalTransform>(req.TargetEntity))
//        //    {
//        //        ecb.DestroyEntity(entities[i]);
//        //        continue;
//        //    }

//        //    LocalTransform vehicleTransform = state.EntityManager.GetComponentData<LocalTransform>(req.TargetEntity);
//        //    float3 localHitPos = math.transform(math.inverse(vehicleTransform.ToMatrix()), req.WorldPosition);

//        //    DynamicBuffer<LocalChunkDestructionMask> destructionMask = state.EntityManager.GetBuffer<LocalChunkDestructionMask>(req.TargetEntity);

//        //    int3 centerVoxel = (int3)math.round(localHitPos);
//        //    int intRadius = (int)math.ceil(req.Radius);

//        //    // Тройной цикл побитового выжигания маски
//        //    for (int z = centerVoxel.z - intRadius; z <= centerVoxel.z + intRadius; z++)
//        //    {
//        //        if (z < 0 || z > 31) continue;
//        //        for (int y = centerVoxel.y - intRadius; y <= centerVoxel.y + intRadius; y++)
//        //        {
//        //            if (y < 0 || y > 31) continue;
//        //            for (int x = centerVoxel.x - intRadius; x <= centerVoxel.x + intRadius; x++)
//        //            {
//        //                if (x < 0 || x > 31) continue;

//        //                float3 currentVoxelPos = new float3(x, y, z);
//        //                if (math.distance(localHitPos, currentVoxelPos) <= req.Radius)
//        //                {
//        //                    int flatIndex = x + (y << 5) + (z << 10);
//        //                    int ulongIndex = flatIndex >> 6;
//        //                    int bitOffset = flatIndex & 63;

//        //                    ulong currentMaskBit = 1UL << bitOffset;

//        //                    LocalChunkDestructionMask maskElement = destructionMask[ulongIndex];
//        //                    maskElement.Value |= currentMaskBit; // Маска реплицируется автоматически!
//        //                    destructionMask[ulongIndex] = maskElement;
//        //                }
//        //            }
//        //        }
//        //    }


//        //    UnityEngine.Debug.LogWarning("Принудительно взводим тег обновления физики/визуала на сервере");
//        //    // Принудительно взводим тег обновления физики/визуала на сервере
//        //    //if (state.EntityManager.HasComponent<ChunkMeshNeedCreate>(req.TargetEntity))
//        //    //{
//        //    //}
//        //    state.EntityManager.SetComponentEnabled<ChunkMeshNeedCreate>(req.TargetEntity, true);
//        //    state.EntityManager.SetComponentEnabled<ChunkColliderNeedCreate>(req.TargetEntity, true);

//        //    // Удаляем сущность входящего RPC-пакета, она обработана
//        //    ecb.DestroyEntity(entities[i]);
//        //}

//        entities.Dispose();
//        requests.Dispose();
//    }
//}
