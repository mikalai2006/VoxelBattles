//using System;
//using System.Runtime.CompilerServices;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Entities.Graphics;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Physics;
//using Unity.Rendering;
//using UnityEngine;
//using UnityEngine.Rendering;

//// ====================================================================
//// ЖЕЛЕЗОБЕТОННЫЙ СЕТЕВОЙ ФИКС: Переносим рендер вслед за расчетной системой!
//// Теперь managed-выгрузка в BRG будет просыпаться строго один раз в кадр,
//// когда фоновые воркеры полностью допекут геометрию без чехарды сетевых откатов.
//// ====================================================================
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(VoxelMeshAndPhysicsSystem))]
//public partial class VoxelMeshRenderSystem : SystemBase
//{
//    // 1. Объявляем приватные поля для кэширования запросов
//    private EntityQuery m_ConfigQuery;
//    private EntityQuery m_StorageQuery;
//    // Нативный реестр: Сетевая Сущность -> Её текущий Blob-коллайдер
//    private NativeParallelHashMap<Entity, VoxelColliderCleanupMarker> m_ColliderRegistry;


//    protected override void OnCreate()
//    {
//        // В SystemBase запросы создаются напрямую через GetEntityQuery
//        m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<VoxelGlobalConfigComponent>());
//        m_StorageQuery = GetEntityQuery(ComponentType.ReadOnly<ClientVoxelMeshFrameStorage>());

//        // Выделяем память один раз при старте игры. Настройки сети не затрагиваются.
//        m_ColliderRegistry = new NativeParallelHashMap<Entity, VoxelColliderCleanupMarker>(128, Allocator.Persistent);
//    }

//    protected override void OnDestroy()
//    {
//        // ====================================================================
//        // ОФИЦИАЛЬНЫЙ SAFE ФИКС КРАША dd6dcfaf ДЛЯ MANAGED СИСТЕМ:
//        // Если у тебя в полях класса были объявлены какие-то EntityQuery, 
//        // мы обязаны занулить их ссылки. Это полностью предотвратит конфликт 
//        // деаллокации между Клиентским и Серверным мирами при выходе из Play Mode!
//        // ====================================================================

//        // Пример (если у тебя есть поля запросов, пропиши их зануление сюда):
//        m_ConfigQuery = default;
//        m_StorageQuery = default;

//        // ГАРАНТИРОВАННАЯ ОЧИСТКА ПРИ ВЫХОДЕ ИЗ ИГРЫ (PLAY MODE)
//        // Этот метод вызывается ВСЕГДА, до полной деаллокации памяти мира.
//        if (m_ColliderRegistry.IsCreated)
//        {
//            // Извлекаем все оставшиеся в игре коллайдеры напрямую из нативной памяти
//            using var values = m_ColliderRegistry.GetValueArray(Allocator.Temp);
//            for (int i = 0; i < values.Length; i++)
//            {
//                if (values[i].ColliderBlob.IsCreated)
//                {
//                    values[i].ColliderBlob.Dispose(); // Принудительно выгружаем каждый компаунд
//                }
//            }

//            // Уничтожаем саму карту
//            m_ColliderRegistry.Dispose();
//        }

//        // Обязательно вызываем базовый метод уничтожения SystemBase!
//        base.OnDestroy();
//        // ====================================================================
//    }

//    protected override void OnUpdate()
//    {
//        // Проверяем, валидна ли наша карта в памяти (защита от Domain Reload в редакторе)
//        if (!m_ColliderRegistry.IsCreated)
//        {
//            UnityEngine.Debug.LogWarning($"m_ColliderRegistry IsCreated= NO!");
//            return;
//        }

//        // ====================================================================
//        // ФАЗА 1: УЛЬТРА-БЫСТРАЯ ПРОВЕРКА НАЛИЧИЯ ДАННЫХ В КАДРЕ
//        // Если ни один чанк вокселей в мире не находится в статусе выпекания графики,
//        // система мгновенно выходит, затрачивая ровно 0.00 мс времени Main Thread!
//        // ====================================================================
//        var flushQuery = SystemAPI.QueryBuilder().WithAll<ChunkGraphicsFlushTag>().Build();
//        if (flushQuery.IsEmpty) return;

//        // Выделяем временные контейнеры Mono-кадра для передачи в метод ExecuteManagedMeshAllocation
//        var chunksDataArray = new NativeList<NewChunkGraphicsData>(16, Allocator.Temp);
//        var childOffsetsList = new NativeList<float3>(16, Allocator.Temp);
//        Entity rootVehicleEntity = Entity.Null;



//        // ====================================================================
//        // ФАЗА 2: СБОРКА ЧАНКОВ, КОТОРЫЕ ПОЛНОСТЬЮ ДОПЕКЛИСЬ НА ЯДРАХ CPU
//        // ====================================================================
//        foreach (var (flushTag, chunkEntity) in SystemAPI.Query<RefRO<ChunkGraphicsFlushTag>>().WithEntityAccess())
//        {
//            // АТОМАРНЫЙ БЕЗОПАСНЫЙ ДЕТЕКТОР:
//            // Если фоновый воркер процессора ВСЁ ЕЩЕ ПИШЕТ данные в Persistent-массивы этого чанка —
//            // мы КАТЕГОРИЧЕСКИ пропускаем его в текущем кадре симуляции!
//            // Вызов .IsCompleted занимает 0.00 мс и НИКОГДА не фризит главный поток игры.
//            if (!flushTag.ValueRO.LastBakingJobHandle.IsCompleted || !flushTag.ValueRO.SafeVertices.IsCreated || !flushTag.ValueRO.SafeCounter.IsCreated)
//            {
//                continue;
//            }

//            // Так как С++ флаг готовности равен true, вызов Complete() закроет хэндл за 0.00 мс
//            // и официально откроет полные managed-права на безопасное чтение unmanaged-памяти!
//            var currentJobHandle = flushTag.ValueRO.LastBakingJobHandle;
//            currentJobHandle.Complete();

//            rootVehicleEntity = flushTag.ValueRO.RootVehicleEntity;

//            // Заполняем плоскую структуру NewChunkGraphicsData прямыми С++ ссылками на Persistent массивы
//            chunksDataArray.Add(new NewChunkGraphicsData
//            {
//                TargetEntity = chunkEntity,
//                HasGraphicsBefore = flushTag.ValueRO.HasGraphicsBefore,
//                LocalBounds = flushTag.ValueRO.LocalBounds,
//                WorldBounds = flushTag.ValueRO.WorldBounds,
//                SafeCounter = flushTag.ValueRO.SafeCounter,
//                SafeColliderBlob = flushTag.ValueRO.SafeColliderBlob,

//                // Передаем unmanaged-указатели на Persistent-массивы геометрии чанка
//                SafeVertices = flushTag.ValueRO.SafeVertices,
//                SafeIndices = flushTag.ValueRO.SafeIndices
//            });

//            //var isClient = World.IsClient();
//            //string textWorld = isClient ? "Client" : "Server";

//            //UnityEngine.Debug.Log($"[{textWorld}] Добавление NewChunkGraphicsData для {flushTag.ValueRO.index}");


//            childOffsetsList.Add(flushTag.ValueRO.LocalOffsetWithPivot);
//        }

//        // Если в текущем кадре ни один из чанков еще до конца не завершил фоновые вычисления —
//        // мгновенно закрываем контекст кадра, не нагружая процессор вхолостую!
//        if (chunksDataArray.Length == 0)
//        {
//            chunksDataArray.Dispose();
//            childOffsetsList.Dispose();
//            return;
//        }

//        // ====================================================================
//        // ФАЗА 3: СБОРКА МОНОЛИТНОГО COMPOUND COLLIDER АВТОМОБИЛЯ ИЗ ГОТОВЫХ ЧАНКОВ
//        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        // ====================================================================
//        int totalReadyCount = chunksDataArray.Length;
//        int validCollidersCount = 0;

//        for (int i = 0; i < totalReadyCount; i++)
//        {
//            int2 finalCounts = chunksDataArray[i].SafeCounter[0];
//            if (finalCounts.x > 0 && chunksDataArray[i].SafeColliderBlob.IsCreated)
//            {
//                validCollidersCount++;
//            }
//        }

//        BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

//        if (validCollidersCount > 0)
//        {
//            var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//            int currentInstanceIdx = 0;

//            for (int i = 0; i < totalReadyCount; i++)
//            {
//                int2 finalCounts = chunksDataArray[i].SafeCounter[0];
//                if (finalCounts.x > 0 && chunksDataArray[i].SafeColliderBlob.IsCreated)
//                {
//                    compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//                    {
//                        Collider = chunksDataArray[i].SafeColliderBlob[0],
//                        CompoundFromChild = new RigidTransform(quaternion.identity, childOffsetsList[i])
//                    };
//                    currentInstanceIdx++;
//                }
//            }

//            if (compoundInstances.Length > 0)
//            {
//                finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//            }

//            compoundInstances.Dispose();
//        }


//        //// ====================================================================
//        //// ОБЪЕДИНЕННЫЙ ШАГ 3: МГНОВЕННАЯ УТИЛИЗАЦИЯ И СБРОС ДАННЫХ ЧАНКОВ
//        //// Чистим С++ кучу и обнуляем оригиналы на сущностях в один проход
//        //// ====================================================================
//        //for (int i = 0; i < chunksDataArray.Length; i++)
//        //{
//        //    Entity chunkEntity = chunksDataArray[i].TargetEntity;

//        //    if (EntityManager.HasComponent<ChunkGraphicsFlushTag>(chunkEntity))
//        //    {
//        //        // 1. Получаем ИСТИННУЮ ссылку на компонент внутри чанка памяти ECS.
//        //        // Ключевое слово ref здесь КРИТИЧЕСКИ важно!
//        //        ref var originalFlushTag = ref SystemAPI.GetComponentRW<ChunkGraphicsFlushTag>(chunkEntity).ValueRW;

//        //        // 2. Уничтожаем нативный С++ BlobAsset меш-коллайдера чанка
//        //        if (originalFlushTag.SafeColliderBlob.IsCreated)
//        //        {
//        //            var chunkMeshColliderRef = originalFlushTag.SafeColliderBlob[0];
//        //            if (chunkMeshColliderRef.IsCreated)
//        //            {
//        //                chunkMeshColliderRef.Dispose(); // Удалили физическое BVH-дерево чанка из C++
//        //            }

//        //            // 3. Уничтожаем сам оригинальный Persistent-массив
//        //            originalFlushTag.SafeColliderBlob.Dispose(); // Стираем контейнер
//        //        }

//        //        // ====================================================================
//        //        // КЛЮЧЕВОЙ ШАГ: Поскольку мы работаем через ref и ValueRW, 
//        //        // эти обнуления запишутся СТРОГО в оригинальную память сущности!
//        //        // ====================================================================
//        //        originalFlushTag.SafeColliderBlob = default;
//        //        originalFlushTag.SafeVertices = default;
//        //        originalFlushTag.SafeIndices = default;
//        //        originalFlushTag.SafeCounter = default;
//        //    }
//        //}

//        childOffsetsList.Dispose();

//        // Извлекаем конфигурации глобального кэша и BRG-хранилища из ECS-мира
//        m_ConfigQuery = SystemAPI.QueryBuilder().WithAll<VoxelGlobalConfigComponent>().Build();
//        m_StorageQuery = SystemAPI.QueryBuilder().WithAll<ClientVoxelMeshFrameStorage>().Build();

//        // ====================================================================
//        // ФАЗА 4: ЗАПУСК ВАШЕГО СУЩЕСТВУЮЩЕГО МЕТОДА ВЫГРУЗКИ ГРАФИКИ НА GPU
//        // Копирует вершины через GetSubArray() и регистрирует BatchMeshID
//        // ====================================================================
//        // ====================================================================
//        // БРОНИРОВАННЫЙ AAA-ПРЕДOХРАНИТЕЛЬ СИСТЕМЫ (Safe Идеал)
//        // Обворачиваем вызов твоего метода в блок try!
//        // ====================================================================
//        try
//        {
//            ExecuteManagedMeshAllocation(
//                ref this.CheckedStateRef,
//                chunksDataArray,
//                rootVehicleEntity,
//                finalVehicleCompoundCollider,
//                m_ConfigQuery,
//                m_StorageQuery
//            );
//        }
//        // ====================================================================
//        // ЗАКРЫВАЕМ КАПКАН УТИЛИЗАЦИИ В БЛОКЕ FINALLY:
//        // Что бы ни произошло внутри ExecuteManagedMeshAllocation — этот блок 
//        // выполнится ЖЕЛЕЗНО! Системные утечки RewindableAllocator и краши 
//        // ObjectDisposedException будут уничтожены раз и навсегда!
//        // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//        // ====================================================================
//        finally
//        {
//            // ====================================================================
//            // ФАЗА 5: ТОТАЛЬНАЯ УТИЛИЗАЦИЯ И СТИРАНИЕ ПАМЯТИ ЧАНКОВ (Safe Финал)
//            // Полигоны улетели на GPU. Теперь мы ОДИН РАЗ за сессию кадра чисто удаляем
//            // unmanaged-массивы геометрии чанков, полностью предотвращая утечки ОЗУ!
//            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//            // ====================================================================
//            //var ecb = new EntityCommandBuffer(Allocator.Temp);

//            //for (int i = 0; i < totalReadyCount; i++)
//            //{
//            //    Entity chunkEntity = chunksDataArray[i].TargetEntity;

//            //    // Намертво вырезаем C++ массивы из Persistent-кучи памяти ОЗУ компьютера
//            //    if (chunksDataArray[i].SafeVertices.IsCreated) chunksDataArray[i].SafeVertices.Dispose();
//            //    if (chunksDataArray[i].SafeIndices.IsCreated) chunksDataArray[i].SafeIndices.Dispose();
//            //    if (chunksDataArray[i].SafeCounter.IsCreated) chunksDataArray[i].SafeCounter.Dispose();
//            //    if (chunksDataArray[i].SafeColliderBlob.IsCreated) chunksDataArray[i].SafeColliderBlob.Dispose();

//            //    // Безопасно снимаем маркер готовности и открываем чанк для будущих сетевых разрушений
//            //    ecb.RemoveComponent<ChunkGraphicsFlushTag>(chunkEntity);

//            //    // Снимаем блок повторного планирования Burst-системы, возвращая чанк в общий пул игры!
//            //    ecb.SetComponentEnabled<ChunkActiveState>(chunkEntity, false);
//            //}

//            //chunksDataArray.Dispose();
//            //ecb.Playback(EntityManager);
//            //ecb.Dispose();
//            // ====================================================================
//            // ФАЗА 5: ИСПРАВЛЕННАЯ AAA-УТИЛИЗАЦИЯ И СТИРАНИЕ ПАМЯТИ ЧАНКОВ (Safe)
//            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//            // ====================================================================
//            //var ecb = new EntityCommandBuffer(Allocator.Temp);

//            for (int i = 0; i < totalReadyCount; i++)
//            {
//                Entity chunkEntity = chunksDataArray[i].TargetEntity;

//                if (EntityManager.Exists(chunkEntity))
//                {
//                    //// Намертво вырезаем C++ массивы геометрии чанка из Persistent-кучи памяти
//                    //if (chunksDataArray[i].SafeVertices.IsCreated) chunksDataArray[i].SafeVertices.Dispose();
//                    //if (chunksDataArray[i].SafeIndices.IsCreated) chunksDataArray[i].SafeIndices.Dispose();
//                    //if (chunksDataArray[i].SafeCounter.IsCreated) chunksDataArray[i].SafeCounter.Dispose();
//                    //if (chunksDataArray[i].SafeColliderBlob.IsCreated) chunksDataArray[i].SafeColliderBlob.Dispose();

//                    // Снимаем маркер выгрузки графики с сущности чанка
//                    //ecb.RemoveComponent<ChunkGraphicsFlushTag>(chunkEntity);
//                    // ====================================================================

//                    // АБСОЛЮТНО АСИНХРОННЫЙ ФИНАЛ КАДРА:
//                    // Просто гасим стейты компонентов. 0% Structural Changes, 0% фризов,
//                    // и полная безопасность для карты гостов Netcode!
//                    // ====================================================================
//                    //EntityManager.SetComponentEnabled<ChunkActiveState>(chunkEntity, false);
//                    EntityManager.SetComponentEnabled<ChunkGraphicsFlushTag>(chunkEntity, false); // Выключили до следующего взрыва!

//                    //// Возвращаем чанк в общий пул симуляции игры
//                    //ecb.SetComponentEnabled<ChunkActiveState>(chunkEntity, false);
//                }
//            }

//            // Чистим передаточный список кадра
//            chunksDataArray.Dispose();
//            //// Воспроизводим и чисто уничтожаем буфер команд, закрывая системный лик!
//            //ecb.Playback(EntityManager);
//            //ecb.Dispose();

//            //// ====================================================================
//            //// ПЕРИОДИЧЕСКАЯ ОЧИСТКА УНИЧТОЖЕННЫХ В ИГРЕ МАШИН
//            //// ====================================================================
//            //if (!m_ColliderRegistry.IsEmpty)
//            //{
//            //    // Заменяем Allocator.TempJob на Allocator.Temp. 
//            //    // Память Temp живет ровно 1 кадр и уничтожается С++ ядром без логов утечек.
//            //    var keys = m_ColliderRegistry.GetKeyArray(Allocator.Temp);

//            //    for (int i = 0; i < keys.Length; i++)
//            //    {
//            //        var vehicleEntity = keys[i];

//            //        // Если сетевая сущность больше не существует в ECS-мире
//            //        if (!this.CheckedStateRef.EntityManager.Exists(vehicleEntity))
//            //        {
//            //            if (m_ColliderRegistry.TryGetValue(vehicleEntity, out var lostGroup))
//            //            {
//            //                if (lostGroup.ColliderBlob.IsCreated)
//            //                {
//            //                    lostGroup.ColliderBlob.Dispose(); // Выгружаем физику из RAM
//            //                }
//            //            }
//            //            m_ColliderRegistry.Remove(vehicleEntity); // Стираем запись без структурных изменений
//            //        }
//            //    }

//            //    // При использовании Allocator.Temp вызывать keys.Dispose() НЕОБЯЗАТЕЛЬНО,
//            //    // но явный вызов делает код более аккуратным и чистым.
//            //    keys.Dispose();
//            //}

//        }
//    }

//    //protected override void OnUpdate()
//    //{
//    //    // 1. ПРОВЕРКА НАЛИЧИЯ ДАННЫХ В КАДРЕ:
//    //    // Быстро проверяем, есть ли вообще в мире чанки с маркером выгрузки графики.
//    //    // Если перестраивать нечего — мы мгновенно выходим, тратя 0.00 мс!
//    //    var flushQuery = SystemAPI.QueryBuilder().WithAll<ChunkGraphicsFlushTag>().Build();
//    //    if (flushQuery.IsEmpty) return;

//    //    // Создаем временный список кадра для передачи в твой метод ExecuteManagedMeshAllocation
//    //    var chunksDataArray = new NativeList<NewChunkGraphicsData>(16, Allocator.Temp);
//    //    var childOffsetsList = new NativeList<float3>(16, Allocator.Temp);
//    //    Entity rootVehicleEntity = Entity.Null;

//    //    // ====================================================================
//    //    // КАНОНИЧНЫЙ ЗАПРОС ИЗВЛЕЧЕНИЯ ДАННЫХ ИЗ СУЩНОСТЕЙ ЧАНКОВ
//    //    // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//    //    // ====================================================================
//    //    foreach (var (flushTag, chunkEntity) in SystemAPI.Query<RefRO<ChunkGraphicsFlushTag>>().WithEntityAccess())
//    //    {
//    //        if (!flushTag.ValueRO.LastBakingJobHandle.IsCompleted) continue;

//    //        rootVehicleEntity = flushTag.ValueRO.RootVehicleEntity;

//    //        var vertexBuffer = CheckedStateRef.EntityManager.GetBuffer<ChunkVertexElement>(chunkEntity);
//    //        var indexBuffer = CheckedStateRef.EntityManager.GetBuffer<ChunkIndexElement>(chunkEntity);

//    //        // Заполняем структуру NewChunkGraphicsData прямо из компонента чанка!
//    //        chunksDataArray.Add(new NewChunkGraphicsData
//    //        {
//    //            TargetEntity = chunkEntity,
//    //            HasGraphicsBefore = flushTag.ValueRO.HasGraphicsBefore,
//    //            LocalBounds = flushTag.ValueRO.LocalBounds,
//    //            WorldBounds = flushTag.ValueRO.WorldBounds,

//    //            SafeVertices = vertexBuffer.Reinterpret<VoxelVertex>().AsNativeArray(),
//    //            SafeIndices = indexBuffer.Reinterpret<int>().AsNativeArray(),
//    //            SafeCounter = flushTag.ValueRO.SafeCounter,
//    //            SafeColliderBlob = flushTag.ValueRO.SafeColliderBlob
//    //        });

//    //        childOffsetsList.Add(flushTag.ValueRO.LocalOffsetWithPivot);
//    //    }

//    //    // ====================================================================
//    //    // СБОРКА ОБЩЕГО ФИЗИЧЕСКОГО КОЛЛАЙДЕРА МАШИНЫ В MANAGED СРЕДЕ
//    //    // ====================================================================
//    //    // На первой строчке легально и безопасно закрываем фоновые потоки CPU.
//    //    // Ошибки параллельного чтения полностью уничтожены!
//    //    this.Dependency.Complete();

//    //    int totalReadyCount = chunksDataArray.Length;
//    //    int validCollidersCount = 0;

//    //    // Отсекаем пустые чанки без полигонов
//    //    for (int i = 0; i < totalReadyCount; i++)
//    //    {
//    //        int2 finalCounts = chunksDataArray[i].SafeCounter[0];
//    //        if (finalCounts.x > 0 && chunksDataArray[i].SafeColliderBlob.IsCreated) validCollidersCount++;
//    //    }

//    //    BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider = default;

//    //    if (validCollidersCount > 0)
//    //    {
//    //        var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//    //        int currentInstanceIdx = 0;

//    //        for (int i = 0; i < totalReadyCount; i++)
//    //        {
//    //            int2 finalCounts = chunksDataArray[i].SafeCounter[0];
//    //            if (finalCounts.x > 0 && chunksDataArray[i].SafeColliderBlob.IsCreated)
//    //            {
//    //                compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//    //                {
//    //                    Collider = chunksDataArray[i].SafeColliderBlob[0],
//    //                    CompoundFromChild = new RigidTransform(quaternion.identity, childOffsetsList[i])
//    //                };
//    //                currentInstanceIdx++;
//    //            }
//    //        }

//    //        if (compoundInstances.Length > 0)
//    //        {
//    //            finalVehicleCompoundCollider = CompoundCollider.Create(compoundInstances);
//    //        }
//    //        compoundInstances.Dispose();
//    //    }
//    //    childOffsetsList.Dispose();

//    //    // Извлекаем стандартные квери для твоего метода
//    //    EntityQuery configQuery = SystemAPI.QueryBuilder().WithAll<VoxelGlobalConfigComponent>().Build();
//    //    EntityQuery storageQuery = SystemAPI.QueryBuilder().WithAll<ClientVoxelMeshFrameStorage>().Build();

//    //    // 2. БЕЗОПАСНЫЙ ВЫЗОВ ТВОЕГО ОРИГИНАЛЬНОГО МЕТОДА ОТРИСОВКИ
//    //    ExecuteManagedMeshAllocation(
//    //        ref this.CheckedStateRef,
//    //        chunksDataArray,
//    //        rootVehicleEntity,
//    //        finalVehicleCompoundCollider,
//    //        configQuery,
//    //        storageQuery
//    //    );

//    //    //// ====================================================================
//    //    //// ВЕЛИКИЙ АСИНХРОННЫЙ ФИНАЛ: АТОМАРНАЯ ОЧИСТКА ПАМЯТИ
//    //    //// Меши скопированы в видеокарту, CompoundCollider создан.
//    //    //// Теперь мы чисто стираем долговременные Persistent-буферы чанков 
//    //    //// и снимаем Cleanup-компонент, возвращая чанк в общий пул игры!
//    //    //// ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//    //    //// ====================================================================
//    //    //var ecb = new EntityCommandBuffer(Allocator.Temp);
//    //    //for (int i = 0; i < totalReadyCount; i++)
//    //    //{
//    //    //    Entity chunkEntity = chunksDataArray[i].TargetEntity;

//    //    //    // Физически удаляем C++ массивы из оперативной Persistent-памяти кадра
//    //    //    if (chunksDataArray[i].SafeVertices.IsCreated) chunksDataArray[i].SafeVertices.Dispose();
//    //    //    if (chunksDataArray[i].SafeIndices.IsCreated) chunksDataArray[i].SafeIndices.Dispose();
//    //    //    if (chunksDataArray[i].SafeCounter.IsCreated) chunksDataArray[i].SafeCounter.Dispose();
//    //    //    if (chunksDataArray[i].SafeColliderBlob.IsCreated) chunksDataArray[i].SafeColliderBlob.Dispose();

//    //    //    // Снимаем стейт-маркер. На следующем кадре чанк снова чист для новых деформаций!
//    //    //    ecb.RemoveComponent<ChunkGraphicsFlushTag>(chunkEntity);
//    //    //}
//    //    //chunksDataArray.Dispose();

//    //    // ====================================================================
//    //    // ИСПРАВЛЕННЫЙ ФИНАЛ: СНИМАЕМ ТОЛЬКО СТЕЙТ-МАРКЕР С ЧАНКОВ!
//    //    // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
//    //    // ====================================================================
//    //    var ecb = new EntityCommandBuffer(Allocator.Temp);
//    //    for (int i = 0; i < totalReadyCount; i++)
//    //    {
//    //        Entity chunkEntity = chunksDataArray[i].TargetEntity;

//    //        // ====================================================================
//    //        // ЖЕЛЕЗОБЕТОННЫЙ SAFE-ФИКС: Все строки .Dispose() для массивов чанка 
//    //        // ОТСЮДА ПОЛНОСТЬЮ УБРАНЫ, чтобы избежать двойного удаления памяти!
//    //        // ====================================================================

//    //        // Снимаем стейт-маркер Cleanup. 
//    //        // На следующем кадре чанк снова абсолютно чист для новых сетевых деформаций!
//    //        ecb.RemoveComponent<ChunkGraphicsFlushTag>(chunkEntity);
//    //    }

//    //    // ====================================================================
//    //    // ЖЕЛЕЗОБЕТОННЫЙ SAFE-ФИКС УТЕЧКИ TempJob/Persistent:
//    //    // Мы уничтожаем сам передаточный Persistent-список кадра, в котором 
//    //    // чанки прилетели из unmanaged Burst-системы! 
//    //    // Это полностью очистит ОЗУ от мусора, и варнинг профайлера исчезнет!
//    //    // ====================================================================
//    //    if (chunksDataArray.IsCreated)
//    //    {
//    //        chunksDataArray.Dispose();
//    //    }
//    //    // ====================================================================


//    //    ecb.Playback(EntityManager);
//    //    ecb.Dispose();
//    //}



//    // ЭТОТ МЕТОД ВЫПОЛНЯЕТСЯ В MANAGED РЕЖИМЕ БЕЗ КОНФЛИКТОВ С BURST
//    [MethodImpl(MethodImplOptions.NoInlining)]
//    private void ExecuteManagedMeshAllocation(
//            ref SystemState state,
//            NativeList<NewChunkGraphicsData> chunksData,
//            Entity rootVehicleEntity,
//            BlobAssetReference<Unity.Physics.Collider> finalVehicleCompoundCollider,
//            EntityQuery m_ConfigQuery,
//            EntityQuery m_StorageQuery)
//    {
//        var isClient = state.WorldUnmanaged.IsClient();
//        string textWorld = isClient ? "Client" : "Server";

//        // Извлекаем конфиг
//        var brgConfig = state.EntityManager.GetComponentObject<VoxelGlobalConfigComponent>(m_ConfigQuery.GetSingletonEntity());

//        // Извлекаем хранилище фреймов
//        var frameStorage = state.EntityManager.GetComponentObject<ClientVoxelMeshFrameStorage>(m_StorageQuery.GetSingletonEntity());

//        // Получаем графическую систему
//        var graphicsSystem = state.World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

//        // Буфер команд
//        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
//        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
//        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//        if (frameStorage != null)
//        {
//            frameStorage.RuntimeMeshes.Clear();
//            frameStorage.DataArrays.Clear();
//            frameStorage.TargetEntities.Clear();
//            //#if UNITY_EDITOR
//            //            UnityEngine.Debug.Log($"[{textWorld}]: ClientVoxelMeshFrameStorage found and clear!");
//            //#endif
//        }
//        else
//        {
//#if UNITY_EDITOR
//            UnityEngine.Debug.Log($"[{textWorld}]: ClientVoxelMeshFrameStorage not found!");
//#endif
//        }

//        bool isColliderAssignedToEntity = false;
//        try
//        {
//            // Проверка: если маркер есть, но физики (скорости или самого коллайдера) уже нет — 
//            // значит машина деспавнится прямо сейчас! Игнорируем её и сразу уничтожаем свежевыпеченный блоб.
//            //bool isEntityDying = state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity)
//            //                     && !state.EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity);
//            // Легальный и Burst-безопасный способ понять, что Netcode утилизирует машину:
//            bool isEntityDying = (!state.EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity)
//                                     || !state.EntityManager.HasComponent<Unity.NetCode.GhostInstance>(rootVehicleEntity));
//            //state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity)
//            //                     &&



//            //if (rootVehicleEntity != Entity.Null && state.EntityManager.Exists(rootVehicleEntity) && finalVehicleCompoundCollider.IsCreated && !isEntityDying)
//            //{
//            //    // 1. Создаем маркер и заполняем его в 100% безопасном управляемом коде
//            //    var newCleanupData = new VoxelColliderCleanupMarker
//            //    {
//            //        ColliderBlob = finalVehicleCompoundCollider,
//            //        ChildBlobs = new FixedList128Bytes<BlobAssetReference<Unity.Physics.Collider>>()
//            //    };

//            //    // Переносим ссылки на дочерние блобы из вашего m_ChunksToInitialize
//            //    for (int i = 0; i < chunksData.Length; i++)
//            //    {
//            //        var chunkData = chunksData[i];
//            //        if (chunkData.SafeColliderBlob.IsCreated)
//            //        {
//            //            // Сохраняем ссылку на дочерний чанк в маркер для будущей safe-очистки
//            //            newCleanupData.ChildBlobs.Add(chunkData.SafeColliderBlob[0]);
//            //        }
//            //    }

//            //    // 2. Если машина перепекается (маркер уже был) — сначала чистим СТАРЫЕ ассеты
//            //    if (state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
//            //    {
//            //        var oldCleanupData = state.EntityManager.GetComponentData<VoxelColliderCleanupMarker>(rootVehicleEntity);

//            //        // Safe-удаление старого корня машины
//            //        if (oldCleanupData.ColliderBlob.IsCreated) oldCleanupData.ColliderBlob.Dispose();

//            //        // Safe-удаление всех старых дочерних чанков
//            //        for (int c = 0; c < oldCleanupData.ChildBlobs.Length; c++)
//            //        {
//            //            if (oldCleanupData.ChildBlobs[c].IsCreated)
//            //            {
//            //                oldCleanupData.ChildBlobs[c].Dispose();
//            //            }
//            //        }

//            //        // Записываем новые данные поверх старых
//            //        state.EntityManager.SetComponentData(rootVehicleEntity, newCleanupData);
//            //        state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });
//            //    }
//            //    else
//            //    {
//            //        // Если машина только создалась
//            //        state.EntityManager.AddComponentData(rootVehicleEntity, new VoxelColliderCleanupMarker { ColliderBlob = finalVehicleCompoundCollider });
//            //        state.EntityManager.AddComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });
//            //    }

//            //    isColliderAssignedToEntity = true;

//            //    // Настройка массы и скорости
//            //    var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
//            //    dynamicMass.CenterOfMass = float3.zero;

//            //    if (state.EntityManager.HasComponent<PhysicsMass>(rootVehicleEntity))
//            //    {
//            //        state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);
//            //    }
//            //    else
//            //    {
//            //        state.EntityManager.AddComponentData(rootVehicleEntity, dynamicMass);
//            //        state.EntityManager.AddComponentData(rootVehicleEntity, new PhysicsVelocity());
//            //    }

//            //    // Add the physics world index via the command buffer
//            //    if (!state.EntityManager.HasComponent<PhysicsWorldIndex>(rootVehicleEntity))
//            //    {
//            //        state.EntityManager.AddSharedComponentManaged(rootVehicleEntity, new PhysicsWorldIndex
//            //        {
//            //            Value = 0
//            //        });
//            //    }

//            //    state.EntityManager.AddComponentData(rootVehicleEntity, new AAA_MovementComponent
//            //    {
//            //        MaxSpeed = 45f,
//            //        Acceleration = 25f,
//            //        Deceleration = 18f
//            //    });

//            //    //#if UNITY_EDITOR
//            //    //                UnityEngine.Debug.Log($"[{textWorld}] Найдена корневая сущность! Свежий коллайдер добавлен.");
//            //    //#endif
//            //}
//            if (rootVehicleEntity != Entity.Null && state.EntityManager.Exists(rootVehicleEntity) && finalVehicleCompoundCollider.IsCreated && !isEntityDying)
//            {



//                //// 1. Создаем маркер очистки блобов
//                //var newCleanupData = new VoxelColliderCleanupMarker
//                //{
//                //    ColliderBlob = finalVehicleCompoundCollider,
//                //    //ChildBlobs = new FixedList128Bytes<BlobAssetReference<Unity.Physics.Collider>>()
//                //};

//                ////for (int i = 0; i < chunksData.Length; i++)
//                ////{
//                ////    var chunkData = chunksData[i];
//                ////    if (chunkData.SafeColliderBlob.IsCreated)
//                ////    {
//                ////        newCleanupData.ChildBlobs.Add(chunkData.SafeColliderBlob[0]);
//                ////    }
//                ////}

//                //// 2. Если машина перепекается (маркер уже был) — сначала чистим СТАРЫЕ ассеты
//                //if (state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
//                //{
//                //    var oldCleanupData = state.EntityManager.GetComponentData<VoxelColliderCleanupMarker>(rootVehicleEntity);

//                //    // Safe-удаление старого корня машины
//                //    if (oldCleanupData.ColliderBlob.IsCreated) oldCleanupData.ColliderBlob.Dispose();

//                //    // Safe-удаление всех старых дочерних чанков
//                //    for (int c = 0; c < oldCleanupData.ChildBlobs.Length; c++)
//                //    {
//                //        if (oldCleanupData.ChildBlobs[c].IsCreated)
//                //        {
//                //            oldCleanupData.ChildBlobs[c].Dispose();
//                //        }
//                //    }
//                //}
//                //// 3. ДОЧЕРНИЕ БЛОБЫ НЕ ТРОГАЕМ ЧЕРЕЗ DISPOSE!
//                //// Цикл удаления ChildBlobs[c].Dispose() полностью УДАЛЯЕМ из этого места, 
//                //// так как корень их уже успешно удалил.
//                //// Мы просто очищаем массив ссылок в компоненте, чтобы не хранить "битые" указатели:
//                //if (!oldCleanupData.ChildBlobs.IsEmpty)
//                //{
//                //    oldCleanupData.ChildBlobs.Clear(); // Просто сбрасываем Native-список/массив в ноль без Dispose
//                //}
//                /////// ====================================================================
//                //// ШАГ 1. ПОЛНОЕ УНИЧТОЖЕНИЕ ДАННЫХ ПРОШЛОГО ПРОХОДА (ОТ ДЕТЕЙ К КОРНЮ)
//                //// ====================================================================
//                //if (EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
//                //{
//                //    // Получаем оригинальный маркер с сущности машины для записи через SystemAPI
//                //    var oldCleanupDataRef = SystemAPI.GetComponentRW<VoxelColliderCleanupMarker>(rootVehicleEntity);
//                //    // 1. СНАЧАЛА стираем С++ блобы мешей чанков ПРОШЛОГО кадра. 
//                //    // Поскольку старая машина в ШАГЕ 2 сейчас будет полностью обнулена в PhysicsCollider,
//                //    // эти вызовы .Dispose() РЕАЛЬНО и беспрепятственно очистят RAM!
//                //    if (!oldCleanupDataRef.ValueRW.ChildBlobs.IsEmpty)
//                //    {
//                //        for (int i = 0; i < oldCleanupDataRef.ValueRW.ChildBlobs.Length; i++)
//                //        {
//                //            var oldChildBlob = oldCleanupDataRef.ValueRW.ChildBlobs[i];
//                //            if (oldChildBlob.IsCreated)
//                //            {
//                //                oldChildBlob.Dispose(); // Удалили старый меш-коллайдер чанка из нативной памяти
//                //            }
//                //        }
//                //        oldCleanupDataRef.ValueRW.ChildBlobs.Clear();
//                //    }

//                //    // 2. ТЕПЕРЬ уничтожаем сам корневой компаунд старой машины
//                //    if (oldCleanupDataRef.ValueRW.ColliderBlob.IsCreated)
//                //    {
//                //        oldCleanupDataRef.ValueRW.ColliderBlob.Dispose();
//                //        oldCleanupDataRef.ValueRW.ColliderBlob = default;
//                //    }
//                //}

//                // Если для этой сетевой сущности УЖЕ был коллайдер — выгружаем его из RAM
//                // Это происходит мгновенно на С++ уровне без структурных изменений!
//                if (m_ColliderRegistry.TryGetValue(rootVehicleEntity, out var oldGroup))
//                {
//                    if (oldGroup.ColliderBlob.IsCreated)
//                    {
//                        oldGroup.ColliderBlob.Dispose();
//                    }
//                }

//                // Записываем новый сгенерированный коллайдер
//                var newGroup = new VoxelColliderCleanupMarker
//                {
//                    ColliderBlob = finalVehicleCompoundCollider
//                };

//                m_ColliderRegistry[rootVehicleEntity] = newGroup;

//                // ====================================================================
//                // ШАГ 2. ОБНУЛЯЕМ СВЯЗЬ С ФИЗИКОЙ ДЛЯ СТАРОГО КОЛЛАЙДЕРА
//                // ====================================================================
//                if (EntityManager.HasComponent<PhysicsCollider>(rootVehicleEntity))
//                {
//                    var currentColliderRef = SystemAPI.GetComponentRW<PhysicsCollider>(rootVehicleEntity);
//                    currentColliderRef.ValueRW.Value = default;
//                }

//                // 3. ЗАПИСЫВАЕМ НОВЫЕ ВАЛИДНЫЕ ДАННЫЕ
//                //state.EntityManager.SetComponentData(rootVehicleEntity, newCleanupData);
//                //state.EntityManager.AddComponentData(rootVehicleEntity, new VoxelColliderCleanupMarker { ColliderBlob = finalVehicleCompoundCollider });
//                state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

//                // 4. ЗАЩИТА МАССЫ ОТ NaN (Деления на ноль)
//                var massProperties = finalVehicleCompoundCollider.Value.MassProperties;

//                // ИСПРАВЛЕНИЕ: Извлекаем тензор инерции через InertiaTensorWithOrientation
//                float3 inertia = massProperties.MassDistribution.InertiaTensor;

//                // Проверяем тензор инерции, если он сломан/нулевой (при пустом или недостроенном воксельном меше)
//                if (inertia.x <= 0f || inertia.y <= 0f || inertia.z <= 0f)
//                {
//                    // Подставляем безопасную единичную сферу, чтобы физика Unity не выдала NaN
//                    massProperties = MassProperties.UnitSphere;
//                }

//                var dynamicMass = PhysicsMass.CreateDynamic(massProperties, 1000.0f);
//                dynamicMass.CenterOfMass = float3.zero;
//                state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);


//                state.EntityManager.SetComponentData(rootVehicleEntity, new AAA_MovementComponent
//                {
//                    MaxSpeed = 45f,
//                    Acceleration = 25f,
//                    Deceleration = 18f
//                });


//                //// 2. РАЗДЕЛЯЕМ ЛОГИКУ: Перепекание (существующая машина) ИЛИ Новый спавн
//                //if (state.EntityManager.HasComponent<VoxelColliderCleanupMarker>(rootVehicleEntity))
//                //{
//                //    // ЕСЛИ МАШИНА УЖЕ СУЩЕСТВУЕТ (Перепекание):
//                //    // Структура сущности НЕ меняется. Изменение данных через SetComponentData БЕЗОПАСНО делать прямо на месте.
//                //    var oldCleanupData = state.EntityManager.GetComponentData<VoxelColliderCleanupMarker>(rootVehicleEntity);

//                //    if (oldCleanupData.ColliderBlob.IsCreated) oldCleanupData.ColliderBlob.Dispose();
//                //    for (int c = 0; c < oldCleanupData.ChildBlobs.Length; c++)
//                //    {
//                //        if (oldCleanupData.ChildBlobs[c].IsCreated) oldCleanupData.ChildBlobs[c].Dispose();
//                //    }

//                //    state.EntityManager.SetComponentData(rootVehicleEntity, newCleanupData);
//                //    state.EntityManager.SetComponentData(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

//                //    // Массу тоже обновляем на месте, если компонент уже есть
//                //    var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
//                //    dynamicMass.CenterOfMass = float3.zero;
//                //    state.EntityManager.SetComponentData(rootVehicleEntity, dynamicMass);
//                //}
//                //else
//                //{
//                //    // ЕСЛИ МАШИНА ТОЛЬКО ЧТО ЗАСПАВНИЛАСЬ (Новая):
//                //    // Все операции AddComponent делаем СТРОГО через ecb! Прямой EntityManager здесь ЗАПРЕЩЕН.
//                //    ecb.AddComponent(rootVehicleEntity, new VoxelColliderCleanupMarker { ColliderBlob = finalVehicleCompoundCollider });
//                //    ecb.AddComponent(rootVehicleEntity, new PhysicsCollider { Value = finalVehicleCompoundCollider });

//                //    var dynamicMass = PhysicsMass.CreateDynamic(finalVehicleCompoundCollider.Value.MassProperties, 1000.0f);
//                //    dynamicMass.CenterOfMass = float3.zero;
//                //    ecb.AddComponent(rootVehicleEntity, dynamicMass);
//                //    ecb.AddComponent(rootVehicleEntity, new PhysicsVelocity());

//                //    // Главный триггер краша Ghost Map (Shared-компонент) уходит в буфер команд
//                //    ecb.AddSharedComponent(rootVehicleEntity, new PhysicsWorldIndex { Value = 0 });

//                //    ecb.AddComponent(rootVehicleEntity, new AAA_MovementComponent
//                //    {
//                //        MaxSpeed = 45f,
//                //        Acceleration = 25f,
//                //        Deceleration = 18f
//                //    });
//                //}

//                isColliderAssignedToEntity = true;

//                //UnityEngine.Debug.LogWarning($"[{textWorld}]: ExecuteManagedMeshAllocation isColliderAssignedToEntity={isColliderAssignedToEntity}, {rootVehicleEntity.Index}, isEntityDying={isEntityDying}!");
//            }
//            else
//            {
//                // Сущности нет — просто удаляем только что созданный коллайдер
//                if (finalVehicleCompoundCollider.IsCreated)
//                {
//                    finalVehicleCompoundCollider.Dispose();
//                }
//#if UNITY_EDITOR
//                UnityEngine.Debug.LogWarning($"[{textWorld}] Не найдена корневая сущность или она уничтожена! Свежий коллайдер удален.");
//#endif
//            }
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogError($"[{textWorld}] Error: {ex.Message}"); ;

//            // Если что-то пошло не так во время AddComponent/SetComponent (например, OutOfMemory или Burst-abort)
//            if (!isColliderAssignedToEntity && finalVehicleCompoundCollider.IsCreated)
//            {
//                finalVehicleCompoundCollider.Dispose();
//#if UNITY_EDITOR
//                UnityEngine.Debug.LogWarning($"[{textWorld}] Критический сбой! Коллайдер принудительно стерт в блоке finally.");
//#endif
//            }
//        }
//        // ====================================================================

//        var renderMeshDescription = new RenderMeshDescription
//        {
//            FilterSettings = new RenderFilterSettings { ShadowCastingMode = ShadowCastingMode.Off, ReceiveShadows = false, Layer = 0, RenderingLayerMask = 1, MotionMode = MotionVectorGenerationMode.Object, StaticShadowCaster = false },
//            LightProbeUsage = LightProbeUsage.Off
//        };


//        // ====================================================================
//        // ЭТАП 2: БЕЗОПАСНАЯ ФИКСАЦИЯ НА GPU И СБОРКА КОМПОНЕНТОВ BRG
//        // ====================================================================
//        for (int i = 0; i < chunksData.Length; i++)
//        {
//            var chunkData = chunksData[i];

//            int2 finalCounts = chunkData.SafeCounter[0];
//            int vertexCount = finalCounts.x;
//            int indexCount = finalCounts.y;

//            // Вручную и легально чистим буфер счетчика (Safe)
//            if (chunkData.SafeCounter.IsCreated) chunkData.SafeCounter.Dispose();

//            if (vertexCount == 0)
//            {
//                if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//                if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//                if (chunkData.HasGraphicsBefore)
//                {
//                    var emptyInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
//                    emptyInfo.MeshID = brgConfig.EmptyMeshID;
//                    ecb.SetComponent(chunkData.TargetEntity, emptyInfo);
//                }
//                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);

//                // 3. Стираем С++ блоб меш-коллайдера и сам персональный массив
//                if (chunkData.SafeColliderBlob.IsCreated)
//                {
//                    var chunkMeshColliderRef = chunkData.SafeColliderBlob[0];
//                    if (chunkMeshColliderRef.IsCreated)
//                    {
//                        chunkMeshColliderRef.Dispose();
//                    }
//                    chunkData.SafeColliderBlob.Dispose();
//                }
//                continue;
//            }


//            if (isClient)
//            {
//                // Выделяем С++ контейнеры под меш строго под вычисленный в джобе размер
//                var meshDataArray = Mesh.AllocateWritableMeshData(1);
//                var meshData = meshDataArray[0];

//                var attributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
//                attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
//                attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
//                meshData.SetVertexBufferParams(vertexCount, attributes);
//                meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
//                attributes.Dispose();

//                // Создаем безопасные sub-array окна видимости одинаковой длины
//                var activeVerticesSubArray = chunkData.SafeVertices.GetSubArray(0, vertexCount);
//                var activeIndicesSubArray = chunkData.SafeIndices.GetSubArray(0, indexCount);

//                // Копируем массивы напрямую в MeshData
//                meshData.GetVertexData<VoxelVertex>(0).CopyFrom(activeVerticesSubArray);
//                meshData.GetIndexData<int>().CopyFrom(activeIndicesSubArray);

//                // Вручную освобождаем временные массивы (Safe-пайплайн)
//                if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//                if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//                meshData.subMeshCount = 1;
//                meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { topology = MeshTopology.Triangles, vertexCount = vertexCount }, MeshUpdateFlags.DontRecalculateBounds);

//                Mesh runtimeMesh = new Mesh();
//                runtimeMesh.name = "VoxelChunk_SafeDirect_BurstOptimized";

//                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, runtimeMesh);
//                runtimeMesh.RecalculateBounds();

//                BatchMeshID runtimeMeshId = graphicsSystem.RegisterMesh(runtimeMesh);
//                var finalMaterialMeshInfo = new MaterialMeshInfo(brgConfig.OpaqueMaterialRuntimeID, runtimeMeshId);

//                bool currentHasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(chunkData.TargetEntity);

//                if (!currentHasGraphics)
//                {
//                    RenderMeshUtility.AddComponents(chunkData.TargetEntity, state.EntityManager, renderMeshDescription, finalMaterialMeshInfo);
//                    state.EntityManager.SetComponentData(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
//                    state.EntityManager.SetComponentData(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
//                }
//                else
//                {
//                    var oldInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
//                    if (oldInfo.MeshID != BatchMeshID.Null && oldInfo.MeshID != brgConfig.EmptyMeshID) graphicsSystem.UnregisterMesh(oldInfo.MeshID);

//                    ecb.SetComponent(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
//                    ecb.SetComponent(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
//                    ecb.SetComponent(chunkData.TargetEntity, finalMaterialMeshInfo);
//                }

//                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
//            }
//            else
//            {
//                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
//                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
//            }

//            // ====================================================================
//            // ЕДИНАЯ ТОЧКА РУЧНОЙ SAFE-УТИЛИЗАЦИИ ВСЕХ МАССИВОВ ЧАНКА В КАДРЕ
//            // Память гарантированно очистится ровно ОДИН РАЗ, исключая ObjectDisposedException!
//            // ====================================================================
//            // 1. Стираем фоновые массивы вершин и индексов
//            if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
//            if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

//            // 2. Стираем буфер unmanaged-счетчиков
//            if (chunkData.SafeCounter.IsCreated) chunkData.SafeCounter.Dispose();

//            // 3. Стираем С++ блоб меш-коллайдера и сам персональный массив
//            if (chunkData.SafeColliderBlob.IsCreated)
//            {
//                var chunkMeshColliderRef = chunkData.SafeColliderBlob[0];
//                if (chunkMeshColliderRef.IsCreated)
//                {
//                    chunkMeshColliderRef.Dispose();
//                }
//                chunkData.SafeColliderBlob.Dispose();
//            }
//            // ====================================================================
//        }
//    }
//}



////using System.Collections.Generic;
////using Unity.Collections;
////using Unity.Entities;
////using Unity.Physics.Systems;
////using UnityEngine;

////// 1. УПРАВЛЯЕМЫЙ КОМПОНЕНТ ДАННЫХ ДЛЯ ХРАНЕНИЯ ССЫЛОК В КЭШЕ КАДРА
////public class ClientVoxelMeshFrameStorage : IComponentData
////{
////    public List<Mesh> RuntimeMeshes = new List<Mesh>();
////    public List<Mesh.MeshDataArray> DataArrays = new List<Mesh.MeshDataArray>();
////    public List<Entity> TargetEntities = new List<Entity>();
////}

////// Запускаем графическую выгрузку в этой же физической группе, 
////// строго вслед за завершением работы воксельной Burst-системы!
////[UpdateInGroup(typeof(PhysicsSystemGroup))]
////[UpdateAfter(typeof(VoxelMeshAndPhysicsSystem))]
////[UpdateBefore(typeof(PhysicsSimulationGroup))] // Успеваем выгрузить данные до симуляции физики кадра

////public partial class VoxelMeshRenderFlusherSystem : SystemBase
////{
////    protected override void OnUpdate()
////    {
////        // Создаем локальный буфер команд managed-мира для структурных изменений кадра
////        var ecb = new EntityCommandBuffer(Allocator.Temp);

////        // Извлекаем синглтоны для вашего метода выгрузки графики (Config и Storage)
////        EntityQuery configQuery = SystemAPI.QueryBuilder().WithAll<VoxelGlobalConfigComponent>().Build();
////        EntityQuery storageQuery = SystemAPI.QueryBuilder().WithAll<ClientVoxelMeshFrameStorage>().Build();

////        // ====================================================================
////        // КАНОНИЧНАЯ ЗАМЕНА Entities.ForEach ДЛЯ СОВРЕМЕННОГО UNITY DOTS 1.4:
////        // Используем чистый SystemAPI.Query поверх unmanaged-структуры задачи!
////        // ====================================================================
////        foreach (var (renderTask, noticeEntity) in SystemAPI.Query<RefRO<ChunksReadyToRenderTag>>()
////                     .WithEntityAccess())
////        {
////            // 1. БЕЗОПАСНЫЙ MANAGED-ВЫЗОВ ОТРИСОВКИ ЧАНКОВ:
////            // Мы находимся внутри SystemBase! Никаких BurstCompile здесь нет.
////            // Шейдерные теги, RenderMeshUtility и меши Unity скомпилируются идеально без BC1091!
////            VoxelGraphicsExtensions.ExecuteManagedMeshAllocation(
////                ref this.CheckedStateRef, // Передаем SystemState из managed-оболочки
////                renderTask.ValueRO.ChunksData,
////                renderTask.ValueRO.RootVehicleEntity,
////                renderTask.ValueRO.FinalVehicleCompoundCollider,
////                configQuery,
////                storageQuery,
////                ecb // Передаем буфер команд
////            );
////            // ====================================================================
////            // ПЕРЕНЕСЕННЫЙ ВЕЛИКИЙ АСИНХРОННЫЙ ФИНАЛ: АТОМАРНАЯ ОЧИСТКА ПАМЯТИ
////            // Меши скопированы в видеокарту, CompoundCollider создан.
////            // Теперь мы чисто стираем долговременные Persistent-буферы чанков 
////            // и снимаем Cleanup-компонент, возвращая чанк в общий пул игры!
////            // ПРИМЕНЕНО ПРАВИЛО: замена знаков отношений на слова
////            // ====================================================================
////            int totalReadyCount = renderTask.ValueRO.ChunksData.Length;

////            // ПРИМЕНЕНО ПРАВИЛО: замена знака отношений на слово
////            for (int i = 0; i < totalReadyCount; i++)
////            {
////                var chunkData = renderTask.ValueRO.ChunksData[i];
////                Entity chunkEntity = chunkData.TargetEntity;

////                // Физически удаляем C++ массивы из оперативной Persistent-памяти
////                //if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
////                //if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();
////                //if (chunkData.SafeCounter.IsCreated) chunkData.SafeCounter.Dispose();
////                if (chunkData.SafeColliderBlob.IsCreated) chunkData.SafeColliderBlob.Dispose();

////                // Снимаем стейт-маркер Cleanup. На следующем кадре чанк снова чист для новых деформаций!
////                ecb.RemoveComponent<ChunkBakingState>(chunkEntity);
////            }
////            // ====================================================================

////            // Уничтожаем саму сущность-задачу оповещения рендера, кадр полностью отработал!
////            ecb.DestroyEntity(noticeEntity);
////        }

////        // Воспроизводим команды кадра и очищаем буфер команд
////        ecb.Playback(EntityManager);
////        ecb.Dispose();
////    }
////}
