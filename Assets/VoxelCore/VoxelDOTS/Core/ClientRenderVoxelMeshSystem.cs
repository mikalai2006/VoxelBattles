using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

//// 1. УПРАВЛЯЕМЫЙ КОМПОНЕНТ ДАННЫХ ДЛЯ ХРАНЕНИЯ ССЫЛОК В КЭШЕ КАДРА
//public class ClientVoxelMeshFrameStorage : IComponentData
//{
//    public List<Mesh> RuntimeMeshes = new List<Mesh>();
//    public List<Mesh.MeshDataArray> DataArrays = new List<Mesh.MeshDataArray>();
//    public List<Entity> TargetEntities = new List<Entity>();
//}


public struct NewChunkMeshData // : IComponentData, IEnableableComponent
{
    public Entity TargetEntity;
    public int JobIndex;
    public MinMaxAABB LocalBounds;
    public MinMaxAABB WorldBounds;
    public bool HasGraphicsBefore;

    // ДОБАВЛЯЕМ: Ссылки на массивы для безопасного С++ копирования
    public NativeArray<VoxelVertex> SafeVertices;
    public NativeArray<int> SafeIndices;

    // ДОБАВЛЯЕМ: Ссылка на персональный массив счетчика
    public NativeArray<int3> SafeCounter;
}



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
//[UpdateAfter(typeof(ClientCreateVoxelMeshSystem))]
public partial class ClientRenderVoxelMeshSystem : SystemBase
{
    // 1. Объявляем приватные поля для кэширования запросов
    private EntityQuery m_ConfigQuery;
    //private EntityQuery m_StorageQuery;


    protected override void OnCreate()
    {
        // В SystemBase запросы создаются напрямую через GetEntityQuery
        m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<VoxelGlobalConfigComponent>());
        //m_StorageQuery = GetEntityQuery(ComponentType.ReadOnly<ClientVoxelMeshFrameStorage>());

        //// Регистрируем managed-хранилище в ECS-мире один раз при старте
        //Entity storageEntity = EntityManager.CreateEntity();
        //EntityManager.AddComponentObject(storageEntity, new ClientVoxelMeshFrameStorage());

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        // Пример (если у тебя есть поля запросов, пропиши их зануление сюда):
        m_ConfigQuery = default;
        //m_StorageQuery = default;

        // Обязательно вызываем базовый метод уничтожения SystemBase!
        base.OnDestroy();
        // ====================================================================
    }

    protected override void OnUpdate()
    {
        //// Получаем lookup тяжелых данных чанков СТРОГО в режиме ReadOnly (на чтение)
        //// Флаг 'true' отключает часть скрытых блокировок движка
        //var chunkDataLookup = SystemAPI.GetComponentLookup<ChunkMeshData>(true);

        // ====================================================================
        // ФАЗА 1: УЛЬТРА-БЫСТРАЯ ПРОВЕРКА НАЛИЧИЯ ДАННЫХ В КАДРЕ
        // Если ни один чанк вокселей в мире не находится в статусе выпекания графики,
        // система мгновенно выходит, затрачивая ровно 0.00 мс времени Main Thread!
        // ====================================================================
        var flushQuery = SystemAPI.QueryBuilder().WithAll<ChunkMeshData>().Build();
        if (flushQuery.IsEmpty) return;

        // Выделяем временные контейнеры Mono-кадра для передачи в метод ExecuteManagedMeshAllocation
        var chunksDataArray = new NativeList<NewChunkMeshData>(16, Allocator.Temp);
        var childOffsetsList = new NativeList<float3>(16, Allocator.Temp);
        Entity rootVehicleEntity = Entity.Null;

        // ====================================================================
        // ФАЗА 2: СБОРКА ЧАНКОВ, КОТОРЫЕ ПОЛНОСТЬЮ ДОПЕКЛИСЬ НА ЯДРАХ CPU
        // ====================================================================
        foreach (var (chunkData, chunkEntity) in SystemAPI.Query<RefRO<ChunkMeshData>>()
            .WithAll<ChunkMeshData>()
            .WithEntityAccess()
        )
        {
            // Проверяем флаг готовности (z координата нашего нативного счетчика)
            // Если массив не создан или флаг равен 0 — воркер CPU еще пишет данные. Мгновенный пропуск!
            if (!chunkData.ValueRO.SafeCounter.IsCreated || chunkData.ValueRO.SafeCounter[0].z != 1)
            {
                continue;
            }

            rootVehicleEntity = chunkData.ValueRO.RootVehicleEntity;

            // Заполняем плоскую структуру NewChunkGraphicsData прямыми С++ ссылками на Persistent массивы
            chunksDataArray.Add(new NewChunkMeshData
            {
                TargetEntity = chunkEntity,
                HasGraphicsBefore = chunkData.ValueRO.HasGraphicsBefore,
                LocalBounds = chunkData.ValueRO.LocalBounds,
                WorldBounds = chunkData.ValueRO.WorldBounds,
                SafeCounter = chunkData.ValueRO.SafeCounter,

                // Передаем unmanaged-указатели на Persistent-массивы геометрии чанка
                SafeVertices = chunkData.ValueRO.SafeVertices,
                SafeIndices = chunkData.ValueRO.SafeIndices
            });

            //UnityEngine.Debug.Log($"[{textWorld}] Добавление NewChunkGraphicsData для {flushTag.ValueRO.index}");

            childOffsetsList.Add(chunkData.ValueRO.LocalOffsetWithPivot);
        }

        // Если в текущем кадре ни один из чанков еще до конца не завершил фоновые вычисления —
        // мгновенно закрываем контекст кадра, не нагружая процессор вхолостую!
        if (chunksDataArray.Length == 0)
        {
            chunksDataArray.Dispose();
            childOffsetsList.Dispose();
            return;
        }

        // ====================================================================
        // ФАЗА 3: СБОРКА МОНОЛИТНОГО COMPOUND COLLIDER АВТОМОБИЛЯ ИЗ ГОТОВЫХ ЧАНКОВ
        // ====================================================================
        int totalReadyCount = chunksDataArray.Length;

        childOffsetsList.Dispose();

        // Извлекаем конфигурации глобального кэша и BRG-хранилища из ECS-мира
        m_ConfigQuery = SystemAPI.QueryBuilder().WithAll<VoxelGlobalConfigComponent>().Build();
        //m_StorageQuery = SystemAPI.QueryBuilder().WithAll<ClientVoxelMeshFrameStorage>().Build();

        try
        {
            // ====================================================================
            // Отключаем чанки
            // ====================================================================
            for (int i = 0; i < chunksDataArray.Length; i++)
            {
                Entity chunkEntity = chunksDataArray[i].TargetEntity;

                if (EntityManager.Exists(chunkEntity))
                {
                    EntityManager.SetComponentEnabled<ChunkMeshData>(chunkEntity, false); // Выключили до следующего взрыва!
                }
            }

            ExecuteManagedMeshAllocation(
                ref this.CheckedStateRef,
                chunksDataArray,
                rootVehicleEntity,
                m_ConfigQuery
            //m_StorageQuery
            );
        }
        // ====================================================================
        // ЗАКРЫВАЕМ КАПКАН УТИЛИЗАЦИИ В БЛОКЕ FINALLY:
        // Что бы ни произошло внутри ExecuteManagedMeshAllocation — этот блок 
        // выполнится ЖЕЛЕЗНО! Системные утечки RewindableAllocator и краши 
        // ObjectDisposedException будут уничтожены раз и навсегда!
        // ====================================================================
        finally
        {

            // ====================================================================
            // ЕДИНАЯ ТОЧКА РУЧНОЙ SAFE-УТИЛИЗАЦИИ ВСЕХ МАССИВОВ ЧАНКА В КАДРЕ
            // Память гарантированно очистится ровно ОДИН РАЗ, исключая ObjectDisposedException!
            // ====================================================================
            for (int i = 0; i < chunksDataArray.Length; i++)
            {
                // 1. Стираем фоновые массивы вершин и индексов
                if (chunksDataArray[i].SafeVertices.IsCreated) chunksDataArray[i].SafeVertices.Dispose();
                if (chunksDataArray[i].SafeIndices.IsCreated) chunksDataArray[i].SafeIndices.Dispose();

                // 2. Стираем буфер unmanaged-счетчиков
                if (chunksDataArray[i].SafeCounter.IsCreated) chunksDataArray[i].SafeCounter.Dispose();

                // ====================================================================
            }

            // Чистим передаточный список кадра
            chunksDataArray.Dispose();
        }
    }

    // ЭТОТ МЕТОД ВЫПОЛНЯЕТСЯ В MANAGED РЕЖИМЕ БЕЗ КОНФЛИКТОВ С BURST
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteManagedMeshAllocation(
            ref SystemState state,
            NativeList<NewChunkMeshData> chunksData,
            Entity rootVehicleEntity,
            EntityQuery m_ConfigQuery
        //EntityQuery m_StorageQuery
        )
    {
        // Извлекаем конфиг
        var brgConfig = state.EntityManager.GetComponentObject<VoxelGlobalConfigComponent>(m_ConfigQuery.GetSingletonEntity());

        //// Извлекаем хранилище фреймов
        //var frameStorage = state.EntityManager.GetComponentObject<ClientVoxelMeshFrameStorage>(m_StorageQuery.GetSingletonEntity());

        // Получаем графическую систему
        var graphicsSystem = state.World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

        // Буфер команд
        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

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
        //            UnityEngine.Debug.Log($"[Client]: ClientVoxelMeshFrameStorage not found!");
        //#endif
        //        }

        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = new RenderFilterSettings { ShadowCastingMode = ShadowCastingMode.Off, ReceiveShadows = false, Layer = 0, RenderingLayerMask = 1, MotionMode = MotionVectorGenerationMode.Object, StaticShadowCaster = false },
            LightProbeUsage = LightProbeUsage.Off
        };

        // ====================================================================
        // ЭТАП 2: БЕЗОПАСНАЯ ФИКСАЦИЯ НА GPU И СБОРКА КОМПОНЕНТОВ BRG
        // ====================================================================
        //#if UNITY_EDITOR
        //        UnityEngine.Debug.Log($"[Client]: Запуск создания меша!");
        //#endif

        for (int i = 0; i < chunksData.Length; i++)
        {
            //var chunkData = chunksData[i];
            // ПРАВИЛЬНО: берем данные строго по ссылке, без побитового копирования!
            ref readonly var chunkData = ref chunksData.ElementAt(i);
            int3 finalCounts = chunkData.SafeCounter[0];
            int vertexCount = finalCounts.x;
            int indexCount = finalCounts.y;

            if (vertexCount == 0)
            {
                //if (chunkData.SafeVertices.IsCreated) chunkData.SafeVertices.Dispose();
                //if (chunkData.SafeIndices.IsCreated) chunkData.SafeIndices.Dispose();

                if (chunkData.HasGraphicsBefore)
                {
                    var emptyInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
                    emptyInfo.MeshID = brgConfig.EmptyMeshID;
                    ecb.SetComponent(chunkData.TargetEntity, emptyInfo);
                }
                ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
                ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);

                continue;
            }

            // Выделяем С++ контейнеры под меш строго под вычисленный в джобе размер
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];

            var attributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
            attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
            attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
            meshData.SetVertexBufferParams(vertexCount, attributes);
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            attributes.Dispose();

            // Создаем безопасные sub-array окна видимости одинаковой длины
            var activeVerticesSubArray = chunkData.SafeVertices.GetSubArray(0, vertexCount);
            var activeIndicesSubArray = chunkData.SafeIndices.GetSubArray(0, indexCount);

            // Копируем массивы напрямую в MeshData
            meshData.GetVertexData<VoxelVertex>(0).CopyFrom(activeVerticesSubArray);
            meshData.GetIndexData<int>().CopyFrom(activeIndicesSubArray);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { topology = MeshTopology.Triangles, vertexCount = vertexCount }, MeshUpdateFlags.DontRecalculateBounds);

            //Mesh runtimeMesh = new Mesh();
            //Mesh runtimeMesh = VoxelNetMeshPoolSystem.Get(this.World).GetMesh(out meshId);
            //runtimeMesh.name = "VoxelChunk_SafeDirect_BurstOptimized";

            // Получаем меш и сетевой ID из пула
            int activeMeshId = -1;
            Mesh runtimeMesh = null;

            // 1. Получаем систему пула текущего ECS-мира
            var poolSystem = ClientMeshPoolSystem.Get(this.World);

            // 2. Проверяем, существует ли целевая сущность и есть ли у неё уже привязанный меш
            if (EntityManager.Exists(chunkData.TargetEntity) && EntityManager.HasComponent<ChunkMeshLink>(chunkData.TargetEntity))
            {
                // Извлекаем текущий ID меша, который закреплен за чанком
                ChunkMeshLink currentLink = EntityManager.GetComponentData<ChunkMeshLink>(chunkData.TargetEntity);
                int currentId = currentLink.PoolInstanceId;

                // Пытаемся достать этот ЖИВОЙ меш из словаря активных мешей пула
                // (Для этого добавьте в класс VoxelNetMeshPoolSystem публичный метод или свойство для доступа к _activeMeshes)
                if (poolSystem.TryGetActiveMesh(currentId, out Mesh existingMesh))
                {
                    activeMeshId = currentId;
                    runtimeMesh = existingMesh;
                }
            }

            // 3. Если меша еще не было (первая генерация чанка), ТОЛЬКО ТОГДА берем новый из пула
            if (runtimeMesh == null)
            {
                runtimeMesh = poolSystem.GetMesh(out activeMeshId);

                // Безопасно для сети добавляем компонент-ссылку (так как его точно не было)
                if (EntityManager.Exists(chunkData.TargetEntity))
                {
                    EntityManager.AddComponentData(chunkData.TargetEntity, new ChunkMeshLink { PoolInstanceId = activeMeshId });
                }
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, runtimeMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontValidateLodRanges | MeshUpdateFlags.DontRecalculateBounds);
            //runtimeMesh.RecalculateBounds();


            BatchMeshID runtimeMeshId = graphicsSystem.RegisterMesh(runtimeMesh);
            var finalMaterialMeshInfo = new MaterialMeshInfo(brgConfig.OpaqueMaterialRuntimeID, runtimeMeshId);

            bool currentHasGraphics = state.EntityManager.HasComponent<MaterialMeshInfo>(chunkData.TargetEntity);

            if (!currentHasGraphics)
            {
                RenderMeshUtility.AddComponents(chunkData.TargetEntity, state.EntityManager, renderMeshDescription, finalMaterialMeshInfo);
                state.EntityManager.SetComponentData(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
                state.EntityManager.SetComponentData(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
            }
            else
            {
                var oldInfo = state.EntityManager.GetComponentData<MaterialMeshInfo>(chunkData.TargetEntity);
                if (oldInfo.MeshID != BatchMeshID.Null && oldInfo.MeshID != brgConfig.EmptyMeshID) graphicsSystem.UnregisterMesh(oldInfo.MeshID);

                ecb.SetComponent(chunkData.TargetEntity, new RenderBounds { Value = chunkData.LocalBounds });
                ecb.SetComponent(chunkData.TargetEntity, new WorldRenderBounds { Value = chunkData.WorldBounds });
                ecb.SetComponent(chunkData.TargetEntity, finalMaterialMeshInfo);
            }

            ecb.SetComponentEnabled<ChunkActiveState>(chunkData.TargetEntity, true);
            ecb.SetComponentEnabled<ChunkPhysicsActiveState>(chunkData.TargetEntity, true);
        }
    }
}