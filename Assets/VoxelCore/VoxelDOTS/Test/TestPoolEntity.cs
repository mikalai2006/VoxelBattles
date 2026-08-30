
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestPoolEntity : MonoBehaviour
{
    [Header("Настройки пула болванок")]
    public int poolSize = 100;
    public Vector3Int maxModelBounds = new Vector3Int(32, 32, 32);

    //[Header("Модели для спавна")]
    //public List<SOVoxelData> modelsToSpawn;

    [Header("Параметры сетки спавна")]
    public int rows = 5;
    public int columns = 5;
    public float spacing = 10f;
    public PhysicChunkMode PhysicChunkMode;

    // ПЕРЕМЕННЫЕ ДЛЯ КЛИКОВ: Отслеживают координаты следующей свободной ячейки
    private int _currentClickX = 0;
    private int _currentClickZ = 0;
    //private int _nextModelIndex = 0;
    //[SerializeField] private int countCreate = 0;
    //[SerializeField] private int countRemove = 0;

    [SerializeField] private bool _isInitialized = false;
    //private PhysicsVoxelPoolSystem _poolPhysicsSystem;
    private VoxelModelCacheManager _cacheManager;

    private bool isExistMoveEntity = false;


    [SerializeField] private VehiclePresetAsset presetToAssemble;
    [SerializeField] private SOVoxelData testSOToAssemble;


    // ПЕРЕМЕННАЯ ДЛЯ ДЕСПАВНА: Хранит ссылки на все активированные сейчас объекты
    private List<Entity> _activeEntities = new List<Entity>();

    private InputSystem_Actions inputActions;
#if !UNITY_SERVER
    private void Start()
    {
        if (_isInitialized) return;

        inputActions = new InputSystem_Actions();
        inputActions.Player.Interact1.Enable();
        inputActions.Player.Interact1.performed += TestSpawn;

        StartCoroutine(WaitForBakingAndInitialize());
    }

    private void OnDestroy()
    {
        inputActions.Player.Interact1.performed -= TestSpawn;
        inputActions.Player.Interact1.Disable();
    }
    
#endif
    private IEnumerator WaitForBakingAndInitialize()
    {
        if (_isInitialized) yield break;

        //if (modelsToSpawn == null || modelsToSpawn.Count == 0)
        //{
        //    Debug.LogError("[Voxel Test]: Список modelsToSpawn пуст!");
        //    yield break;
        //}

        World world = World.DefaultGameObjectInjectionWorld;
        while (world == null)
        {
            world = World.DefaultGameObjectInjectionWorld;
            Debug.LogWarning("[Voxel Test]: World.DefaultGameObjectInjectionWorld is null");
            yield return null;
        }

        //EntityManager em = world.EntityManager;
        //EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<VoxelGlobalConfigComponent>());

        //while (configQuery.CalculateEntityCount() == 0)
        //{
        //    Debug.LogWarning("[Voxel Test]: ConfigQuery count = 0!");
        //    yield return null;
        //}

        //var configEntities = configQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        //Entity configEntity = configEntities[0];
        //if (configEntity == null)
        //{
        //    Debug.LogError("[Voxel Test]: VoxelGlobalConfigComponent не найден!");
        //    yield break; // Выход из корутины (ее остановка)
        //}
        //configEntities.Dispose();

        //var config = em.GetComponentObject<VoxelGlobalConfigComponent>(configEntity);
        //while (config.OpaqueMaterial == null || config.TransparentMaterial == null)
        //{
        //    Debug.LogWarning("[Voxel Test]: Материалы или один из них не найдены!");
        //    yield return null;
        //}

        // Кэшируем ссылки на пул и кэш Блобов для последующих кликов
        //_poolPhysicsSystem = world.GetExistingSystemManaged<PhysicsVoxelPoolSystem>();
        _cacheManager = VoxelModelCacheManager.Instance;

        // Вызываем PREWARM пула болванок
        int3 maxBounds = new int3(maxModelBounds.x, maxModelBounds.y, maxModelBounds.z);
        //_poolPhysicsSystem.PrewarmPool(poolSize);

        // Переводим флаг в true — пул прогрет, клики разрешены
        _isInitialized = true;
    }

    private void Update()
    {
        // Если пул еще не прогрелся на старте — игнорируем любые нажатия
        if (!_isInitialized) return;

        //// Опрос мыши в новой Input System
        //if (Mouse.current != null)
        //{

        //    // Нажатие ЛКМ — Спавн
        //    if (Mouse.current.leftButton.wasPressedThisFrame)
        //    {
        //        //for (int i = 0; i < countCreate; i++)
        //        //{
        //        //    SpawnNewNextModelByClick();
        //        //}
        //        //SpawnVehicle();
        //        SpawnVehicleRPC();
        //    }

        //    // Нажатие ПКМ — Деспавн
        //    if (Mouse.current.rightButton.wasPressedThisFrame)
        //    {

        //        for (int i = 0; i < countRemove; i++)
        //        {
        //            DespawnVehicle();
        //        }
        //    }
        //}
    }


    private void TestSpawn(InputAction.CallbackContext context)
    {
        SpawnVehicleRPC();
    }
    //private void SpawnNextModelByClick(bool isDynamic)
    //{
    //    int modelsCount = modelsToSpawn.Count;
    //    SOVoxelData currentSO = modelsToSpawn[_nextModelIndex];

    //    //BlobAssetReference<VoxelModelBlob> blobRef = _cacheManager.GetOrCreateBlob(currentSO);
    //    SparseBakedModelResult blobRef = _cacheManager.GetOrCreateSparseModel(currentSO);
    //    float3 spawnPos = new float3(_currentClickX * spacing, 35f, _currentClickZ * spacing);

    //    // Активируем болванку из пула
    //    Entity spawnedChunk = _poolPhysicsSystem.SpawnEntity(spawnPos, blobRef.ChunkCoords, blobRef.ChunkBlobs, isDynamic ? PhysicChunkMode.Dynamic : PhysicChunkMode.Static);

    //    if (spawnedChunk == Entity.Null)
    //    {
    //        Debug.LogWarning("[Voxel System]: Пул пуст! Не удалось спавнить модель.");
    //        return;
    //    }

    //    // Запоминаем сущность в список активных, чтобы потом её можно было деспавнить
    //    _activeEntities.Add(spawnedChunk);

    //    // Сдвигаем координаты сетки вперед
    //    _currentClickX++;
    //    if (_currentClickX >= columns)
    //    {
    //        _currentClickX = 0;
    //        _currentClickZ++;
    //    }

    //    // ПРАВКА: Получаем менеджер сущностей из текущего мира
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    //    if (isDynamic)
    //    {
    //        em.SetComponentData(spawnedChunk, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 100f));
    //    }

    //    _nextModelIndex = (_nextModelIndex + 1) % modelsCount;
    //    //Debug.Log($"[Voxel System]: Спавн. Активно объектов на сцене: {_activeEntities.Count}");
    //}


    /// <summary>
    /// ПРИМЕР МЕТОДА ДЕСПАВНА: Возвращает последний созданный объект обратно в пул
    /// </summary>
    //private void DespawnLastModelByClick()
    //{
    //    // Если на сцене нет ни одного объекта — деспавнить нечего
    //    if (_activeEntities.Count == 0)
    //    {
    //        return;
    //    }

    //    // 1. Берем индекс самого последнего добавленного объекта
    //    int lastIndex = _activeEntities.Count - 1;
    //    Entity entityToDespawn = _activeEntities[lastIndex];

    //    // ПРАВКА: Получаем менеджер сущностей из текущего мира
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;

    //    // КРИТИЧЕСКАЯ ПРАВКА: Проверяем, существует ли сущность в мире.
    //    // А метод IsComponentEnabled точно скажет нам, не спит ли она уже в пуле.
    //    if (em.Exists(entityToDespawn) && em.IsComponentEnabled<ChunkActiveState>(entityToDespawn))
    //    {
    //        // Вызываем метод пула. Теперь он работает мгновенно и без Double Dispose
    //        _poolPhysicsSystem.DespawnModel(entityToDespawn);
    //    }

    //    // 3. Удаляем ссылку из нашего списка учета
    //    _activeEntities.RemoveAt(lastIndex);

    //    // 4. Сдвигаем координаты сетки назад, чтобы вернуть указатель в освободившуюся ячейку
    //    _currentClickX--;

    //    // Безопасная проверка выхода за границы без знака "меньше"
    //    if (_currentClickX == -1)
    //    {
    //        // Если ушли в минус по X, возвращаемся на конец предыдущего ряда по Z
    //        _currentClickX = columns - 1;
    //        _currentClickZ--;

    //        if (_currentClickZ == -1)
    //        {
    //            _currentClickZ = 0;
    //        }
    //    }

    //    // Корректируем индекс прокрутки моделей назад
    //    _nextModelIndex--;
    //    if (_nextModelIndex == -1)
    //    {
    //        _nextModelIndex = modelsToSpawn.Count - 1;
    //    }
    //}


    //private void SpawnNewNextModelByClick()
    //{
    //    int modelsCount = modelsToSpawn.Count;
    //    SOVoxelData currentSO = modelsToSpawn[_nextModelIndex];

    //    //BlobAssetReference<VoxelModelBlob> blobRef = _cacheManager.GetOrCreateBlob(currentSO);
    //    SparseBakedModelResult blobRef = _cacheManager.GetOrCreateSparseModel(currentSO);
    //    float3 spawnPos = new float3(_currentClickX * spacing, 35f, _currentClickZ * spacing);

    //    // Активируем болванку из пула
    //    Entity spawnedChunk = _poolPhysicsSystem.SpawnNewSparseModel(
    //        spawnPos,
    //        blobRef.ChunkCoords,
    //        blobRef.ChunkBlobs,
    //        new VoxelChunkStateComponent
    //        {
    //            PhysicsMode = PhysicChunkMode,
    //            CollisionFilter = CollisionFilter.Default,
    //        });

    //    if (spawnedChunk == Entity.Null)
    //    {
    //        Debug.LogWarning("[Voxel System]: Пул пуст! Не удалось спавнить модель.");
    //        return;
    //    }


    //    // Запоминаем сущность в список активных, чтобы потом её можно было деспавнить
    //    _activeEntities.Add(spawnedChunk);

    //    // Сдвигаем координаты сетки вперед
    //    _currentClickX++;
    //    if (_currentClickX >= columns)
    //    {
    //        _currentClickX = 0;
    //        _currentClickZ++;
    //    }

    //    // ПРАВКА: Получаем менеджер сущностей из текущего мира
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    //    //if (isDynamic)
    //    //{
    //    //    em.SetComponentData(spawnedChunk, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 100f));
    //    //}

    //    if (!isExistMoveEntity)
    //    {
    //        em.AddComponent<IsControlledTag>(spawnedChunk);
    //        em.AddComponentData(spawnedChunk, new AAA_MovementComponent
    //        {
    //            Acceleration = 100f,
    //            CurrentVelocity = 0f,
    //            Deceleration = 40f,
    //            MaxSpeed = 150f
    //        });
    //        isExistMoveEntity = true;
    //    }

    //    _nextModelIndex = (_nextModelIndex + 1) % modelsCount;
    //    //Debug.Log($"[Voxel System]: Спавн. Активно объектов на сцене: {_activeEntities.Count}");
    //}

    //private void NewDespawnLastEntity()
    //{
    //    // Если на сцене нет ни одного объекта — деспавнить нечего
    //    if (_activeEntities.Count == 0)
    //    {
    //        return;
    //    }

    //    // 1. Берем индекс самого последнего добавленного объекта
    //    int lastIndex = _activeEntities.Count - 1;
    //    Entity entityToDespawn = _activeEntities[lastIndex];

    //    // ПРАВКА: Получаем менеджер сущностей из текущего мира
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;

    //    // КРИТИЧЕСКАЯ ПРАВКА: Проверяем, существует ли сущность в мире.
    //    // А метод IsComponentEnabled точно скажет нам, не спит ли она уже в пуле.
    //    if (em.Exists(entityToDespawn) && em.IsComponentEnabled<ChunkActiveState>(entityToDespawn))
    //    {
    //        // Вызываем метод пула. Теперь он работает мгновенно и без Double Dispose
    //        _poolPhysicsSystem.DespawnEntity(entityToDespawn);
    //    }

    //    // 3. Удаляем ссылку из нашего списка учета
    //    _activeEntities.RemoveAt(lastIndex);

    //    // 4. Сдвигаем координаты сетки назад, чтобы вернуть указатель в освободившуюся ячейку
    //    _currentClickX--;

    //    // Безопасная проверка выхода за границы без знака "меньше"
    //    if (_currentClickX == -1)
    //    {
    //        // Если ушли в минус по X, возвращаемся на конец предыдущего ряда по Z
    //        _currentClickX = columns - 1;
    //        _currentClickZ--;

    //        if (_currentClickZ == -1)
    //        {
    //            _currentClickZ = 0;
    //        }
    //    }

    //    // Корректируем индекс прокрутки моделей назад
    //    _nextModelIndex--;
    //    if (_nextModelIndex == -1)
    //    {
    //        _nextModelIndex = modelsToSpawn.Count - 1;
    //    }
    //}

    private void SpawnVehicle()
    {
        if (presetToAssemble == null) return;
        //if (isExistMoveEntity) return;

        // Получаем доступ к ECS-миру
        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager em = world.EntityManager;

        // Высчитываем координаты спавна
        float3 spawnPos = new float3(_currentClickX * spacing, 35f, _currentClickZ * spacing);

        // Создаем чистую сущность-запрос
        Entity requestEntity = em.CreateEntity();

        // Прикрепляем управляемый компонент с данными и ссылкой на ScriptableObject
        em.AddComponentData(requestEntity, new RequestVehicleAssembly
        {
            Preset = presetToAssemble,
            SpawnPosition = spawnPos,
            SpawnRotation = quaternion.identity,
            isAddMove = !isExistMoveEntity,
            IsDynamic = true // Шасси будет динамическим твердым телом

        });

        isExistMoveEntity = true;

        // Сдвигаем координаты сетки вперед
        _currentClickX++;
        if (_currentClickX >= columns)
        {
            _currentClickX = 0;
            _currentClickZ++;
        }
    }

    //private void DespawnVehicle()
    //{
    //    // Если на сцене нет ни одного объекта — деспавнить нечего
    //    if (_activeEntities.Count == 0)
    //    {
    //        return;
    //    }

    //    // 1. Берем индекс самого последнего добавленного объекта
    //    int lastIndex = _activeEntities.Count - 1;
    //    Entity entityToDespawn = _activeEntities[lastIndex];

    //    // ПРАВКА: Получаем менеджер сущностей из текущего мира
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;

    //    // КРИТИЧЕСКАЯ ПРАВКА: Проверяем, существует ли сущность в мире.
    //    // А метод IsComponentEnabled точно скажет нам, не спит ли она уже в пуле.
    //    if (em.Exists(entityToDespawn) && em.IsComponentEnabled<ChunkActiveState>(entityToDespawn))
    //    {
    //        // Вызываем метод пула. Теперь он работает мгновенно и без Double Dispose
    //        //_poolPhysicsSystem.DespawnEntity(entityToDespawn);
    //    }

    //    // 3. Удаляем ссылку из нашего списка учета
    //    _activeEntities.RemoveAt(lastIndex);

    //    // 4. Сдвигаем координаты сетки назад, чтобы вернуть указатель в освободившуюся ячейку
    //    _currentClickX--;

    //    // Безопасная проверка выхода за границы без знака "меньше"
    //    if (_currentClickX == -1)
    //    {
    //        // Если ушли в минус по X, возвращаемся на конец предыдущего ряда по Z
    //        _currentClickX = columns - 1;
    //        _currentClickZ--;

    //        if (_currentClickZ == -1)
    //        {
    //            _currentClickZ = 0;
    //        }
    //    }

    //    // Корректируем индекс прокрутки моделей назад
    //    _nextModelIndex--;
    //    if (_nextModelIndex == -1)
    //    {
    //        _nextModelIndex = modelsToSpawn.Count - 1;
    //    }
    //}

    private void SpawnVehicleRPC()
    {
        //#if UNITY_EDITOR
        //        Debug.Log("[Client] SpawnVehicleRPC");
        //#endif
        if (testSOToAssemble == null) return;

        // 1. Находим клиентский сетевой мир Unity DOTS
        World clientWorld = null;
        foreach (var w in World.All)
        {
            if (w.IsClient())
            {
                clientWorld = w;
                break;
            }
        }

        if (clientWorld == null)
        {
            Debug.LogWarning("[Client] Не найден clientWorld");
            return;
        }
        EntityManager em = clientWorld.EntityManager;

        // Высчитываем координаты спавна
        float3 spawnPos = new float3(_currentClickX * spacing, 55f, _currentClickZ * spacing);

        // 2. Создаем чистую сущность с намерением спавна в клиентском мире
        Entity intentEntity = em.CreateEntity();
        // 1. Создаем unmanaged-строку из managed-имени
        FixedString64Bytes unmanagedName = new FixedString64Bytes(testSOToAssemble.name);

        // ИСПРАВЛЕНО: Используем нативный GetHashCode() и кастуем его в uint
        uint configHashName = (uint)unmanagedName.GetHashCode();
        em.AddComponentData(intentEntity, new SpawnVehicleIntent
        {
            PresetId = configHashName, // Передаем unmanaged ID пресета
            SpawnPosition = spawnPos,
            SpawnRotation = Quaternion.identity,
            IsAddMove = !isExistMoveEntity,
            IsDynamic = true
        });
        //#if UNITY_EDITOR
        //        Debug.LogWarning($"[Client] Отправлен RPC на создание модели с configHashName={configHashName}");
        //#endif
        isExistMoveEntity = true;

        // Сдвигаем координаты сетки вперед
        _currentClickX++;
        if (_currentClickX >= columns)
        {
            _currentClickX = 0;
            _currentClickZ++;
        }
    }


}
