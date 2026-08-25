using UnityEngine;

[System.Serializable]
public struct PoolStats
{
    [Mikalai2006.Utils.ReadOnly] public int TotalSize;       // Общий размер пула, заданный в инспекторе
    [Mikalai2006.Utils.ReadOnly] public int ActiveCount;    // Сколько объектов сейчас в игре (активны)
    [Mikalai2006.Utils.ReadOnly] public int InArchiveCount; // Сколько объектов спит в архивных координатах
    [Mikalai2006.Utils.ReadOnly] public int ActiveAttached; // Сколько из активных привязаны к виртуальным родителям
    [Mikalai2006.Utils.ReadOnly] public long TotalSpawns;   // Сколько всего раз вызывался спавн за сессию (метрика нагрузки)
}

/// <summary>
/// Высокопроизводительный пул объектов (Zero-Alloc), поддерживающий виртуальное родительство,
/// контроль версий хэндлов для защиты от Double Despawn и асинхронный жизненный цикл.
/// </summary>
public class UltraVirtualPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject prefab; // Префаб, который пул будет размножать
    [SerializeField] private int poolSize = 1000; // Вместимость пула

    // Координаты на краю карты, куда улетают спать неактивные объекты
    private static readonly Vector3 ArchivePosition = new Vector3(100000f, 100000f, 100000f);

    private PoolNode[] _nodes;              // Массив всех созданных объектов (нод) пула
    private int[] _availableIndices;       // Стек свободных индексов в виде массива
    private int _topIndex;                 // Указатель на вершину стека свободных индексов

    private VirtualChild[] _activeChildren; // Линейный массив для быстрого обновления трансформаций на CPU
    private int _activeChildrenCount;      // Текущее количество активных виртуальных детей

    // Хранилище текущего состояния пула для инспектора и дебага
    [SerializeField] private PoolStats _stats;

    /// <summary>
    /// Свойство для безопасного чтения статистики другими системами (например, UI или дебаггером)
    /// </summary>
    public PoolStats Stats => _stats;

    private void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// Первоначальное наполнение пула. Выполняется один раз при старте сцены.
    /// </summary>
    private void InitializePool()
    {
        _nodes = new PoolNode[poolSize];
        _availableIndices = new int[poolSize];
        _activeChildren = new VirtualChild[poolSize];
        _topIndex = poolSize;
        _activeChildrenCount = 0;

        _stats.TotalSize = poolSize;
        _stats.InArchiveCount = poolSize;
        _stats.ActiveCount = 0;
        _stats.ActiveAttached = 0;
        _stats.TotalSpawns = 0;

        // Создаем пустой корневой объект в иерархии сцены
        GameObject poolHolder = new GameObject($"[POOL_{prefab.name}]");

        for (int i = 0; i != poolSize; i++)
        {
            GameObject instance = Instantiate(prefab, ArchivePosition, Quaternion.identity, poolHolder.transform);

            _nodes[i] = new PoolNode(instance);
            _nodes[i].VirtualChild.Initialize();

            // ЖЕСТКАЯ ДЕАКТИВАЦИЯ ПРИ СТАРТЕ:
            // Изначально выключаем компонент виртуального ребенка в пуле
            _nodes[i].VirtualChild.enabled = false;

            _availableIndices[i] = i;

            ConfigureForArchive(ref _nodes[i]);
        }
    }
    /// <summary>
    /// Безопасно возвращает логику сущности по её хэндлу, если версии совпадают.
    /// Работает со скоростью прямого чтения ячейки памяти (наносекунды).
    /// </summary>
    public PoolEntity GetElementByHandle(PoolHandle handle)
    {
        // Защита от выхода за границы массива на случай некорректного индекса
        if (handle.Index < 0 || handle.Index >= _nodes.Length) return null;

        ref PoolNode node = ref _nodes[handle.Index];

        // Проверяем версию хэндла, чтобы случайно не выдать объект, который уже переиспользован
        if (node.Version == handle.Version)
        {
            return node.EntityLogic;
        }

        return null;
    }

    /// <summary>
    /// Спавн автономного объекта в мире с гарантированной защитой от проваливания под Plane и рабочим вращением.
    /// </summary>
    public PoolHandle SpawnSafe(Vector3 position, Quaternion rotation, out PoolNode node, float lifeTime = 0f, Vector3? scale = null)
    {
        // 1. Извлекаем свободную ноду из пула (PrepareNode включит коллайдер у оригинала)
        int index = PrepareNodeFromPool(out node);
        if (index == -1) return PoolHandle.Invalid;

        // 2. Берем ЖЕСТКУЮ ref-ссылку на оригинал ячейки в массиве пула для работы с PhysX
        ref PoolNode originalNode = ref _nodes[index];

        // Применяем масштаб строго у оригинала в массиве до активации динамики тела
        originalNode.Transform.localScale = scale ?? Vector3.one;

        // 3. МГНОВЕННЫЙ ФИЗИЧЕСКИЙ ПЕРЕНОС (Выдергиваем из архивной точки 100000f)
        originalNode.Transform.SetPositionAndRotation(position, rotation);

        // ВЫЗОВ НАШЕГО ОПТИМИЗИРОВАННОГО ФИЗИЧЕСКОГО КОНВЕЙЕРА
        ActivateNodePhysics(ref originalNode, position, rotation);

        //// Принудительно синхронизируем матрицы Unity и PhysX на этом кадре,
        //// чтобы внешние AddForce скрипты сразу видели твердый объект на сцене, а не в архиве.
        //Physics.SyncTransforms();

        // 4. Генерируем уникальный паспорт-хэндл текущего поколения ячейки
        PoolHandle handle = new PoolHandle(index, originalNode.Version);

        // 5. Инициализируем логику сущности (запуск тайнеров UniTask и пользовательского OnSpawn)
        if (originalNode.EntityLogic != null)
        {
            originalNode.EntityLogic.InternalSetup(this, handle, lifeTime);
        }

        // Синхронизируем out параметр перед выходом, чтобы внешний код получил валитный Rigidbody
        node = originalNode;

        return handle;
    }

    /// <summary>
    /// Спавн обычного (автономного) объекта в мире.
    /// </summary>
    /// <summary>
    /// Спавн обычного (автономного) объекта в мире с поддержкой масштабирования.
    /// </summary>
    //public PoolHandle SpawnSafe(Vector3 position, Quaternion rotation, out PoolNode node, float lifeTime = 0f, Vector3? scale = null)
    //{
    //    // 1. Извлекаем свободную ноду из пула
    //    int index = PrepareNodeFromPool(out node);
    //    if (index == -1) return PoolHandle.Invalid;

    //    // 2. Берем ЖЕСТКУЮ прямую ссылку на оригинал ячейки в массиве пула, 
    //    // чтобы все настройки применились на C++ стороне PhysX, а не в копии структуры!
    //    ref PoolNode originalNode = ref _nodes[index];

    //    // НОВОЕ: Применяем масштаб СТРОГО в момент спавна до включения физики!
    //    // Если scale не передан, используем Vector3.one (дефолт префаба)
    //    node.Transform.localScale = scale ?? Vector3.one;

    //    // 2. МГНОВЕННЫЙ ФИЗИЧЕСКИЙ ПЕРЕНОС (ДО активации симуляции!)
    //    // 1. СНАЧАЛА выставляем визуальные и физические координаты
    //    originalNode.Transform.SetPositionAndRotation(position, rotation);

    //    if (originalNode.Rigidbody != null)
    //    {
    //        // ЖЕСТКАЯ СИНХРОНИЗАЦИЯ КООРДИНАТ ДО СНЯТИЯ КИНЕМАТИКИ
    //        originalNode.Rigidbody.position = position;
    //        originalNode.Rigidbody.rotation = rotation;

    //        // 2. Включаем компоненты
    //        if (originalNode.Collider != null) originalNode.Collider.enabled = true;
    //        if (originalNode.Renderer != null) originalNode.Renderer.enabled = true;

    //        // 3. СБРОС КИНЕМАТИКИ
    //        originalNode.Rigidbody.isKinematic = false;
    //        originalNode.Rigidbody.useGravity = true;
    //        originalNode.Rigidbody.mass = 100f;

    //        // 4. КРИТИЧЕСКИЙ ХАК ДЛЯ ПЕРЕЗАПУСКА PHYSX (Фикс призрака):
    //        // Переключение режима коллизий туда-обратно СТРОГО в этот момент 
    //        // принудительно заставляет C++ ядро PhysX выкинуть объект из архивной сетки коллизий 100000f 
    //        // и заново создать для него твердый Box Collider в игровых координатах!
    //        originalNode.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    //        originalNode.Rigidbody.detectCollisions = true;

    //        // 5. ОЧИСТКА ИНЕРЦИИ (Разблокирует вращение)
    //        originalNode.Rigidbody.linearVelocity = Vector3.zero;
    //        originalNode.Rigidbody.angularVelocity = Vector3.zero;
    //        originalNode.Rigidbody.ResetInertiaTensor();

    //        //// Принудительно будим тело
    //        //originalNode.Rigidbody.WakeUp();
    //    }

    //    //// КРИТИЧЕСКИЙ ФИКС ДЛЯ PHYSX:
    //    //// Принудительно заставляем физический движок Unity обновить матрицы и
    //    //// заново зарегистрировать объект в сетке коллизий сцены прямо на ЭТОМ кадре,
    //    //// вытащив его из "слепой зоны" архивных 100000f координат!
    //    //Physics.SyncTransforms();

    //    // 5. Генерируем уникальный хэндл текущего поколения ячейки
    //    PoolHandle handle = new PoolHandle(index, originalNode.Version);

    //    // 5. Инициализируем логику сущности
    //    if (node.EntityLogic != null)
    //    {
    //        node.EntityLogic.InternalSetup(this, handle, lifeTime);
    //    }

    //    return handle;
    //}
    //public PoolHandle SpawnSafe(Vector3 position, Quaternion rotation, out PoolNode node, float lifeTime = 0f)
    //{
    //    // 1. Извлекаем свободную ноду из пула
    //    int index = PrepareNodeFromPool(out node);
    //    if (index == -1) return PoolHandle.Invalid;

    //    // 2. Выставляем мировые координаты на сцене
    //    node.Transform.SetPositionAndRotation(position, rotation);

    //    // 3. Безопасно активируем динамику PhysX
    //    if (node.Rigidbody != null)
    //    {
    //        node.Rigidbody.isKinematic = false;
    //        node.Rigidbody.linearVelocity = Vector3.zero;
    //        node.Rigidbody.angularVelocity = Vector3.zero;
    //    }

    //    // 4. Генерируем уникальный хэндл текущего поколения ячейки
    //    PoolHandle handle = new PoolHandle(index, node.Version);

    //    // 5. Инициализируем логику сущности
    //    if (node.EntityLogic != null)
    //    {
    //        node.EntityLogic.InternalSetup(this, handle, lifeTime);
    //    }

    //    return handle;
    //}

    /// <summary>
    /// Спавн объекта с автоматической привязкой к виртуальному родителю.
    /// </summary>
    public PoolHandle SpawnAttachedSafe(
        Vector3 worldPosition,
        Quaternion worldRotation,
        Transform parentTransform,
        Rigidbody parentRb,
        PoolEntity parentPoolEntity, // ДОБАВЛЕНО: передаем сюда саму машину/родителя
        out PoolNode node)
    {
        // 1. Извлекаем свободную ноду из пула
        int index = PrepareNodeFromPool(out node);
        if (index == -1) return PoolHandle.Invalid;

        // 2. Ставим в начальные координаты перед расчетом смещений виртуального родительства
        node.Transform.SetPositionAndRotation(worldPosition, worldRotation);

        // 3. Рассчитываем офсеты виртуального родительства и настраиваем физику PhysX
        node.VirtualChild.Attach(parentTransform, parentRb, parentPoolEntity);

        // 4. Добавляем объект в линейный массив пула для последующего вызова Update обновлений
        _activeChildren[_activeChildrenCount] = node.VirtualChild;
        _activeChildrenCount++;

        _stats.ActiveAttached++;

        // 5. Создаем хэндл и инициализируем логику сущности
        PoolHandle handle = new PoolHandle(index, node.Version);

        if (node.EntityLogic != null)
        {
            node.EntityLogic.InternalSetup(this, handle, 0f);
        }

        return handle;
    }

    /// <summary>
    /// Универсальный спавн: поддерживает локальные смещения для дочерних элементов 
    /// и мировые координаты для независимых объектов.
    /// </summary>
    /// <param name="position">Локальное смещение (если есть родитель) или Мировая позиция (если родителя нет).</param>
    /// <param name="rotation">Локальный поворот (если есть родитель) или Мировое вращение (если родителя нет).</param>
    public PoolHandle SpawnSafeUniversal(
        Vector3 position,
        Quaternion rotation,
        out PoolNode node,
        float lifeTime = 0f,
        Transform parentTransform = null,
        Rigidbody parentRb = null,
        PoolEntity parentPoolEntity = null,
        Vector3? scale = null
    )
    {
        // 1. Извлекаем свободную ноду из пула
        int index = PrepareNodeFromPool(out node);
        if (index == -1) return PoolHandle.Invalid;

        // Применяем масштаб строго у оригинала в массиве до активации динамики тела
        node.Transform.localScale = scale ?? Vector3.one;

        // 2. Если передан родительский Transform — рассчитываем позицию с учетом смещения
        if (parentTransform != null)
        {
            // Переводим переданные локальные координаты в мировые прямо в момент спавна,
            // чтобы объект визуально сразу встал в правильную точку пространства.
            Vector3 worldPos = parentTransform.TransformPoint(position);
            Quaternion worldRot = parentTransform.rotation * rotation;

            node.Transform.SetPositionAndRotation(worldPos, worldRot);

            // Привязываем виртуальное родительство (внутрь передаем прямую ссылку на PoolEntity родителя)
            node.VirtualChild.Attach(parentTransform, parentRb, parentPoolEntity);

            // Добавляем в линейный массив пула для вызова UpdateTick/FixedUpdateTick
            _activeChildren[_activeChildrenCount] = node.VirtualChild;
            _activeChildrenCount++;

            _stats.ActiveAttached++;
        }
        else
        {
            //// Если родителя нет, трактуем position/rotation как чистые мировые координаты
            //node.Transform.SetPositionAndRotation(position, rotation);

            //// Настраиваем стандартную динамику PhysX для свободного объекта
            //if (node.Rigidbody != null)
            //{
            //    node.Rigidbody.isKinematic = false;
            //    node.Rigidbody.linearVelocity = Vector3.zero;
            //    node.Rigidbody.angularVelocity = Vector3.zero;
            //}
            node.Transform.SetPositionAndRotation(position, rotation);

            // ВЫЗОВ НАШЕГО ОПТИМИЗИРОВАННОГО ФИЗИЧЕСКОГО КОНВЕЙЕРА
            ActivateNodePhysics(ref node, position, rotation);
        }

        // 3. Генерируем хэндл и запускаем InternalSetup логики
        PoolHandle handle = new PoolHandle(index, node.Version);

        if (node.EntityLogic != null)
        {
            node.EntityLogic.InternalSetup(this, handle, lifeTime);
        }

        return handle;
    }


    /// <summary>
    /// Спавнит объект в "спящем" состоянии (без запуска OnSpawn). 
    /// Позволяет внешнему коду полностью сконфигурировать параметры ДО активации.
    /// </summary>
    public PoolHandle SpawnSleepingUniversal(
        Vector3 position,
        Quaternion rotation,
        out PoolNode node,
        float lifeTime = 0f,
        Transform parentTransform = null,
        Rigidbody parentRb = null,
        PoolEntity parentPoolEntity = null
    )
    {
        // 1. Извлекаем свободную ноду из пула (PrepareNode включит коллайдер и рендер у оригинала)
        int index = PrepareNodeFromPool(out node);
        if (index == -1) return PoolHandle.Invalid;

        // Берем прямую ref-ссылку на оригинал в массиве пула, чтобы избежать гонки копирования структур
        ref PoolNode originalNode = ref _nodes[index];

        // 2. ВАРИАНТ А: Объект становится виртуальным дочерним элементом машины
        if (parentTransform != null)
        {
            Vector3 worldPos = parentTransform.TransformPoint(position);
            Quaternion worldRot = parentTransform.rotation * rotation;

            originalNode.Transform.SetPositionAndRotation(worldPos, worldRot);

            // Привязываем виртуальное родительство (скрипт VirtualChild выключится при деспавне, 
            // а включится нативным OnEnable, когда вы вызовете Activate())
            originalNode.VirtualChild.Attach(parentTransform, parentRb, parentPoolEntity);

            // Регистрируем в линейный массив пула для CPU-обновлений в LateUpdate
            _activeChildren[_activeChildrenCount] = originalNode.VirtualChild;
            _activeChildrenCount++;

            _stats.ActiveAttached++;
        }
        // ВАРИАНТ Б: АВТОНОМНЫЙ ОБЪЕКТ (Обломки, снаряды)
        else
        {
            originalNode.Transform.SetPositionAndRotation(position, rotation);

            // Вызываем ваш закрытый метод настройки Rigidbody (ActivateNodePhysics)
            ActivateNodePhysics(ref originalNode, position, rotation);
        }

        // 3. ГЕНЕРИРУЕМ ХЭНДЛ
        PoolHandle handle = new PoolHandle(index, originalNode.Version);

        // 4. КРИТИЧЕСКИЙ ШАГ:
        // Связываем паспортные данные пула, но НЕ вызываем Activate()! 
        // Объект на сцене физически готов, но его логика OnSpawn() еще заморожена.
        if (originalNode.EntityLogic != null)
        {
            originalNode.EntityLogic.Prepare(this, handle, lifeTime);
        }

        // Синхронизируем out параметр, чтобы вернуть полностью настроенную физически ноду
        node = originalNode;
        return handle;
    }

    /// <summary>
    /// Синхронизирует позицию Rigidbody, переводит его из кинематического режима в динамический
    /// и настраивает Continuous коллизии без холостых вызовов Wake/Reset.
    /// </summary>
    private void ActivateNodePhysics(ref PoolNode node, Vector3 position, Quaternion rotation)
    {
        if (node.Rigidbody == null) return;

        // 1. Телепортируем само нативное тело PhysX в игровые координаты ДО снятия кинематики
        node.Rigidbody.position = position;
        node.Rigidbody.rotation = rotation;

        // 2. Включаем симуляцию, гравитацию и массу
        node.Rigidbody.isKinematic = false;
        node.Rigidbody.useGravity = true;
        node.Rigidbody.mass = 100f;

        // 3. Переключаем CCD для мгновенной перестройки сетки Broadphase коллизий
        node.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        node.Rigidbody.detectCollisions = true;

        // 4. Полностью обнуляем остаточный мусор скоростей из прошлой жизни объекта
        node.Rigidbody.linearVelocity = Vector3.zero;
        node.Rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Безопасный возврат объекта обратно в пул (архив). Защищен от Double Despawn.
    /// </summary>
    public void DespawnSafe(PoolHandle handle, Vector3 explosionForce, ForceMode forceMode)
    {
        int index = handle.Index;
        ref PoolNode node = ref _nodes[index];

        // КРИТИЧЕСКАЯ ЗАЩИТА: Проверка на Double Despawn
        if (node.Version != handle.Version)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[UltraPool] Отклонен Double Despawn! Объект {index} уже в пуле.");
#endif
            return;
        }
        // =========================================================================
        // АВТОМАТИЗАЦИЯ НА УРОВНЕ ПУЛА:
        // Перед тем как спрятать объект в архив, заставляем его сущность (PoolEntity)
        // рекурсивно деспавнить все свои пушки и вложенные объекты!
        // =========================================================================
        if (node.EntityLogic != null && node.EntityLogic.Handle.IsValid)
        {
            // Вызываем метод массового удаления дочерних элементов, сохраненных в массиве
            node.EntityLogic.RemoveAllActive(explosionForce, forceMode);
        }
        // =========================================================================

        // Кэшируем флаг привязки ДО того, как вызовем Detach
        bool wasAttached = node.VirtualChild.IsAttached;

        if (wasAttached)
        {
            node.VirtualChild.Detach(explosionForce, forceMode);
            RemoveFromActiveChildren(node.VirtualChild);
            _stats.ActiveAttached--;
        }

        // Полностью отключаем объект и перемещаем в координаты архива
        ConfigureForArchive(ref node);

        // Повышаем версию ячейки! Старые хэндлы в игре мгновенно станут невалидными
        node.Version++;

        // Возвращаем индекс ячейки обратно на вершину стека свободных индексов
        _availableIndices[_topIndex] = index;
        _topIndex++;

        _stats.ActiveCount--;
        _stats.InArchiveCount++;
    }

    /// <summary>
    /// Извлекает свободный индекс из стека и активирует компоненты визуала/коллизий.
    /// Выполняется строго за O(1) времени без выделения памяти (Zero-Alloc).
    /// </summary>
    private int PrepareNodeFromPool(out PoolNode node)
    {
        // Защита от переполнения пула, заданного в инспекторе
        if (_topIndex == 0)
        {
            Debug.LogError($"[UltraPool] Превышен лимит пула для префаба {prefab.name}!");
            node = default;
            return -1;
        }

        // Достаем индекс свободной ячейки с вершины стека
        _topIndex--;
        int index = _availableIndices[_topIndex];

        // КРИТИЧЕСКИЙ ФИКС: Работаем строго по ref-ссылке, чтобы менять оригинал в массиве!
        ref PoolNode internalNode = ref _nodes[index];

        // ШАГ 1 ДЛЯ PHYSX: Объект обязан обрести твердость ДО телепортации и настройки Rigidbody.
        // Включаем коллайдер и рендер на оригинальной структуре.
        if (internalNode.Collider != null) internalNode.Collider.enabled = true;
        if (internalNode.Renderer != null) internalNode.Renderer.enabled = true;

        // Обновляем метрики производительности и дебаг-статистику для инспектора
        _stats.ActiveCount++;
        _stats.InArchiveCount--;
        _stats.TotalSpawns++;

        // Передаем уже физически активную структуру в out-параметр для внешних систем
        node = internalNode;
        return index;
    }

    //private int PrepareNodeFromPool(out PoolNode node)
    //{
    //    if (_topIndex == 0)
    //    {
    //        Debug.LogError($"[UltraPool] Превышен лимит пула для префаба {prefab.name}!");
    //        node = default;
    //        return -1;
    //    }

    //    _topIndex--;
    //    int index = _availableIndices[_topIndex];

    //    // КРИТИЧЕСКИЙ ФИКС: Берем ref-ссылку на оригинал в массиве
    //    ref PoolNode internalNode = ref _nodes[index];

    //    // ШАГ 1 ДЛЯ PHYSX: Активируем коллайдер и визуал СТРОГО первыми!
    //    // Объект должен обрести физическую оболочку до того, как мы начнем его двигать или настраивать Rigidbody
    //    if (internalNode.Collider != null) internalNode.Collider.enabled = true;
    //    if (internalNode.Renderer != null) internalNode.Renderer.enabled = true;

    //    _stats.ActiveCount++;
    //    _stats.InArchiveCount--;
    //    _stats.TotalSpawns++;

    //    // Передаем уже физически включенную ноду в out-параметр
    //    node = internalNode;
    //    return index;
    //}

    //private int PrepareNodeFromPool(out PoolNode node)
    //{
    //    if (_topIndex == 0)
    //    {
    //        Debug.LogError($"[UltraPool] Превышен лимит пула для префаба {prefab.name}!");
    //        node = default;
    //        return -1;
    //    }

    //    _topIndex--;
    //    int index = _availableIndices[_topIndex];

    //    // КРИТИЧЕСКИЙ ФИКС: Извлекаем по ref-ссылке из массива, чтобы настроить оригинал ячейки!
    //    ref PoolNode internalNode = ref _nodes[index];

    //    // Включаем компоненты обратно при выходе из архива
    //    //if (internalNode.Collider != null) internalNode.Collider.enabled = true;
    //    if (internalNode.Renderer != null) internalNode.Renderer.enabled = true;

    //    _stats.ActiveCount++;
    //    _stats.InArchiveCount--;
    //    _stats.TotalSpawns++;

    //    // Копируем данные в out параметр для внешнего конфигуратора сборщика
    //    node = internalNode;

    //    return index; // Возвращаем индекс в массиве
    //}

    //private int PrepareNodeFromPool(out PoolNode node)
    //{
    //    if (_topIndex == 0)
    //    {
    //        Debug.LogError($"[UltraPool] Превышен лимит пула для префаба {prefab.name}!");
    //        node = default;
    //        return -1;
    //    }

    //    _topIndex--;
    //    int index = _availableIndices[_topIndex];
    //    node = _nodes[index];

    //    if (node.Collider != null) node.Collider.enabled = true;
    //    if (node.Renderer != null) node.Renderer.enabled = true;

    //    _stats.ActiveCount++;
    //    _stats.InArchiveCount--;
    //    _stats.TotalSpawns++;

    //    return index;
    //}

    //private void ConfigureForArchive(ref PoolNode node)
    //{
    //    if (node.Rigidbody != null)
    //    {
    //        node.Rigidbody.linearVelocity = Vector3.zero;
    //        node.Rigidbody.angularVelocity = Vector3.zero;
    //        node.Rigidbody.isKinematic = true;
    //    }

    //    if (node.Collider != null) node.Collider.enabled = false;
    //    if (node.Renderer != null) node.Renderer.enabled = false;

    //    node.Transform.position = ArchivePosition;
    //}
    //private void ConfigureForArchive(ref PoolNode node)
    //{
    //    //if (node.Rigidbody != null)
    //    //{
    //    //    node.Rigidbody.linearVelocity = Vector3.zero;
    //    //    node.Rigidbody.angularVelocity = Vector3.zero;
    //    //    node.Rigidbody.isKinematic = true;

    //    //    // КРИТИЧЕСКИ ВАЖНО: Принудительно усыпляем тело в PhysX, 
    //    //    // чтобы Unity полностью перестала тратить CPU на обсчет его физики!
    //    //    //node.Rigidbody.Sleep();
    //    //}

    //    //if (node.Collider != null) node.Collider.enabled = false;
    //    //if (node.Renderer != null) node.Renderer.enabled = false;

    //    // ОБНУЛЕНИЕ ТРАНСФОРМ (Ваш главный запрос)
    //    // Зануляем локальные данные относительно родительского PoolHolder на сцене
    //    //node.Transform.localPosition = Vector3.zero;
    //    node.Transform.localRotation = Quaternion.identity;
    //    node.Transform.localScale = Vector3.one;

    //    // ПЕРЕМЕЩЕНИЕ В АРХИВ
    //    // После сброса локальных матриц жестко телепортируем объект на край карты
    //    node.Transform.position = ArchivePosition;

    //    // ПОЛНЫЙ СБРОС СЕССИИ ВРАЩЕНИЯ
    //    if (node.VirtualChild != null)
    //    {
    //        // Метод, который мы дописали в VirtualChild для обнуления _currentFreeRollAngle
    //        node.VirtualChild.DespawnReset();
    //    }
    //}
    private void ConfigureForArchive(ref PoolNode node)
    {
        // 1. Сначала сбрасываем только математику VirtualChild
        if (node.VirtualChild != null) node.VirtualChild.DespawnReset();

        // 2. Выключаем физическую оболочку
        if (node.Collider != null) node.Collider.enabled = false;
        if (node.Renderer != null) node.Renderer.enabled = false;

        if (node.Rigidbody != null)
        {
            // Сбрасываем скорости, если тело было динамическим, проверяя флаг
            if (!node.Rigidbody.isKinematic)
            {
                node.Rigidbody.linearVelocity = Vector3.zero;
                node.Rigidbody.angularVelocity = Vector3.zero;
            }

            // ЖЕСТКОЕ УСЫПЛЕНИЕ НА УРОВНЕ ПУЛА:
            node.Rigidbody.isKinematic = true;
            node.Rigidbody.detectCollisions = false;
        }

        // 3. Уносим в архив
        node.Transform.position = ArchivePosition;
        node.Transform.rotation = Quaternion.identity;
        node.Transform.localScale = Vector3.one;
    }


    private void RemoveFromActiveChildren(VirtualChild child)
    {
        int count = _activeChildrenCount;
        for (int i = 0; i < count; i++)
        {
            if (_activeChildren[i] == child)
            {
                _activeChildren[i] = _activeChildren[count - 1];
                _activeChildren[count - 1] = null;
                _activeChildrenCount--;
                return;
            }
        }
    }

    public void UpdateActiveChildren()
    {
        int count = _activeChildrenCount;
        for (int i = 0; i != count; i++)
        {
            _activeChildren[i].UpdateTick();
        }
    }

    public void LateUpdateActiveChildren()
    {
        int count = _activeChildrenCount;
        for (int i = 0; i != count; i++)
        {
            // Вызываем обновление позиций СТРОГО после того, как отработало ВСЕ управление машины
            _activeChildren[i].UpdateTick();
        }
    }

    public void FixedUpdateActiveChildren()
    {
        int count = _activeChildrenCount;
        for (int i = 0; i != count; i++)
        {
            _activeChildren[i].FixedUpdateTick();
        }
    }

    /// <summary>
    /// Проверяет, активен ли узел пула в мире по его индексу.
    /// Работает мгновенно через сравнение позиций.
    /// </summary>
    public bool IsNodeActiveInWorld(int index)
    {
        // Если позиция НЕ равна архивной — объект сейчас летит или работает на сцене
        return _nodes[index].Transform.position != ArchivePosition;
    }

    /// <summary>
    /// Проверяет, активен ли элемент по его хэндлу.
    /// Если объект был деспавнен, версия ячейки изменилась, и метод вернет false.
    /// </summary>
    public bool IsHandleValidAndActive(PoolHandle handle)
    {
        if (handle.Index < 0 || handle.Index >= _nodes.Length) return false;

        // Объект активен, если сохраненная в игре версия совпадает с текущей версией пула
        return _nodes[handle.Index].Version == handle.Version;
    }
}
