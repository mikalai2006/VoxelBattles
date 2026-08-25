using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using RaycastHit = Unity.Physics.RaycastHit;

public class VoxelClickDestroyer : MonoBehaviour
{
    [Header("Настройки разрушения")]
    [SerializeField] private float destructionRadius = 2.5f; // Радиус в вокселях
    [SerializeField] private float maxRayDistance = 100f;

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
            if (world.IsServer())
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
        if (_clientWorld == null || !_clientWorld.IsCreated) return;

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

        if (_physicsQuery.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
        {
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            // 1. Четко разделяем Старт и Направление
            float3 rayStart = ray.origin;
            float3 rayDir = ray.direction; // Это нормализованный вектор (длина = 1)

            var raycastInput = new RaycastInput
            {
                // Старт — мировая точка начала (глаза камеры)
                Start = rayStart,

                // КОНЕЦ — это СТАРТ + НАПРАВЛЕНИЕ, умноженное на дистанцию!
                End = rayStart + (rayDir * maxRayDistance),

                // Бронированный всеядный фильтр
                Filter = new CollisionFilter
                {
                    BelongsTo = unchecked((uint)~0),
                    CollidesWith = unchecked((uint)~0),
                    GroupIndex = 0
                }
            };

#if UNITY_EDITOR
            // Исправляем отладку для визуализации НОВОГО луча
            Debug.LogWarning($"Ray create Bodies={collisionWorld.Bodies.Length}");
            Debug.DrawLine(raycastInput.Start, raycastInput.End, Color.red, 2f);
#endif
            if (collisionWorld.CastRay(raycastInput, out RaycastHit hit))
            {
                Entity hitEntity = hit.Entity;
#if UNITY_EDITOR
                // Клик сработал! Лог выведется со 100% гарантией
                Debug.LogWarning($"[InputBridge] КЛИК СРАБОТАЛ! Попали в сущность: {hitEntity.Index}");
#endif
                if (em.HasComponent<LocalChunkDestructionMask>(hitEntity))
                {
                    float3 hitWorldPosition = hit.Position;

                    Entity requestEntity = em.CreateEntity();
                    em.AddComponentData(requestEntity, new VoxelExplosionRequest
                    {
                        TargetEntity = hitEntity,
                        WorldPosition = hitWorldPosition,
                        Radius = destructionRadius
                    });
                }
                //else
                //{
                //    Debug.LogWarning("Not found LocalChunkDestructionMask!");

                //}
            }
        }
    }
}
