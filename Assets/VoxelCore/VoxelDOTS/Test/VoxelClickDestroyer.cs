using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelClickDestroyer : MonoBehaviour
{
    [Header("Настройки разрушения")]
    [SerializeField] private float destructionRadius = 2.5f; // Радиус в вокселях
    //[SerializeField] private float maxRayDistance = 100f;

    [SerializeField] private Camera _mainCamera;
    private World _clientWorld;
    private EntityQuery _physicsQuery;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        }

        // Ищем мир, который отфильтрован как Клиентский (Client Simulation)
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                _clientWorld = world;
                break;
            }
        }

        // Инициализируйте её в Start() строго ОДИН раз:
        if (_clientWorld != null && _clientWorld.IsCreated)
        {
            _physicsQuery = _clientWorld.EntityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        }
    }

    private void Update()
    {
        if (_clientWorld == null || _clientWorld.IsCreated == false) return;

        // ====================================================================
        // ФИКС ДЛЯ НОВОГО INPUT SYSTEM (Вместо старого UnityEngine.Input)
        // ====================================================================
        // 1. Проверяем нажатие Левой Кнопки Мыши (ЛКМ) через Pointer / Mouse API
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        // 2. Безопасно считываем текущую координату курсора на экране
        //Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 pointerScreenPosition = Pointer.current.position.ReadValue();

        // 3. Пускаем стандартный луч из камеры на основе новой позиции мыши
        UnityEngine.Ray ray = _mainCamera.ScreenPointToRay(pointerScreenPosition);
        // ====================================================================

        // Дальше ваш код получения CollisionWorld и CastRay остается БЕЗ ИЗМЕНЕНИЙ:
        EntityManager em = _clientWorld.EntityManager;


        using (var connectionQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()))
        {
            if (!connectionQuery.IsEmptyIgnoreFilter)
            {
                Entity connectionEntity = connectionQuery.GetSingletonEntity();
                Entity rpcEntity = em.CreateEntity();

                // 2. Записываем только параметры луча
                em.AddComponentData(rpcEntity, new VoxelExplosionRequestRpc
                {
                    RayOrigin = ray.origin,
                    RayDirection = ray.direction,
                    Radius = destructionRadius
                });

                em.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
            }
        }


        //        if (_physicsQuery.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
        //        {
        //            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        //            // 1. Четко разделяем Старт и Направление
        //            float3 rayStart = ray.origin;
        //            float3 rayDir = ray.direction; // Это нормализованный вектор (длина = 1)

        //            var raycastInput = new RaycastInput
        //            {
        //                // Старт — мировая точка начала (глаза камеры)
        //                Start = rayStart,

        //                // КОНЕЦ — это СТАРТ + НАПРАВЛЕНИЕ, умноженное на дистанцию!
        //                End = rayStart + (rayDir * maxRayDistance),

        //                // Бронированный всеядный фильтр
        //                Filter = new CollisionFilter
        //                {
        //                    BelongsTo = unchecked((uint)~0),
        //                    CollidesWith = unchecked((uint)~0),
        //                    GroupIndex = 0
        //                }
        //            };

        //#if UNITY_EDITOR
        //            // Исправляем отладку для визуализации НОВОГО луча
        //            Debug.LogWarning($"Ray create Bodies={collisionWorld.Bodies.Length}");
        //            Debug.DrawLine(raycastInput.Start, raycastInput.End, Color.red, 2f);
        //#endif

        //            //// 1. Создаем запрос для поиска сущности с вашим компонентом синглтона
        //            //EntityQuery cacheQuery = em.CreateEntityQuery(typeof(GlobalVoxelModelCache));

        //            if (collisionWorld.CastRay(raycastInput, out RaycastHit hit))
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
        //                    EntityQuery cacheQuery = em.CreateEntityQuery(typeof(GlobalVoxelModelCache));
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

        //                                    for (int i = 0; i < children.Length; i++)
        //                                    {
        //                                        Entity childEntity = children[i].Value;

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
        //                                using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()))
        //                                {
        //                                    if (!query.IsEmptyIgnoreFilter)
        //                                    {
        //                                        Entity connectionEntity = query.GetSingletonEntity();

        //                                        if (hitChunkEntity != Entity.Null && em.HasComponent<LocalChunkDestructionMask>(hitChunkEntity))
        //                                        {
        //                                            //// 1. Создаем RPC-запрос для отправки на сервер
        //                                            //Entity requestEntity = em.CreateEntity();
        //                                            //em.AddComponent(requestEntity, new VoxelExplosionRequestRpc
        //                                            //{
        //                                            //    TargetEntity = hitChunkEntity,
        //                                            //    WorldPosition = hit.Position,
        //                                            //    Radius = destructionRadius
        //                                            //});


        //                                            //// В Unity Netcode для отправки RPC обязательно вешается этот компонент:
        //                                            //em.AddComponentData(requestEntity, new SendRpcCommandRequest
        //                                            //{
        //                                            //    TargetConnection = connectionEntity
        //                                            //});


        //                                            // 1. Создаем сущность сетевого RPC запроса
        //                                            Entity rpcEntity = em.CreateEntity();

        //                                            em.AddComponentData(rpcEntity, new VoxelExplosionRequestRpc
        //                                            {
        //                                                TargetEntity = hitChunkEntity,
        //                                                WorldPosition = hit.Position,
        //                                                Radius = destructionRadius
        //                                            });

        //                                            // 2. Привязываем RPC к серверному соединению
        //                                            em.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
        //#if UNITY_EDITOR
        //                                            Debug.LogWarning($"[Client] Кликнул в чанк со свойствами: {targetChunkCoords}. RPC создано!");
        //#endif
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        Debug.LogError("Не удалось отправить RPC: Клиент еще не подключен к серверу (NetworkId не найден).");
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }


        //            }

        //        }
    }
}
