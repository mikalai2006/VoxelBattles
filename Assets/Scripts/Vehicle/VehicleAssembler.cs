using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный конструктор машины. Динамически собирает меши и рассчитывает стыковки деталей.
/// Автоматически стыкует колеса по ширине по формуле: (Половина Базы - Половина Колеса).
/// </summary>
public class VehicleAssembler : PoolEntity
{
    private VehicleWheelRotator wheelRotatorComponent;
    public LevelManager LevelManager { get; private set; }

    [Header("Модульное Оружие")]
    public TowerPartAsset selectedTurret;       // Ссылка на выбранную башню (Scriptable Object)
    public MuzzlePartAsset selectedBarrelWeapon; // Ссылка на выбранный ствол (Scriptable Object)

    [Header("Модульная Ходовая Часть")]
    public ChassisAsset selectedChassis;       // Ссылка на выбранную подвеску/ось (Scriptable Object)
    public VehiclePresetWheelAsset selectedWheels;

    // Кэш ссылок на компоненты логики, находящиеся на этом же GameObject машины
    private VehicleMovement movementComponent;
    private PlayerVehicleInput inputComponent;
    private VehicleWeapon weaponComponent;
    private VoxelRecoil recoilComponent;

    [Header("Стартовый Пресет")]
    [Tooltip("Если это поле заполнено, машина автоматически соберется по этому пресету при старте")]
    public VehiclePresetAsset defaultPreset;

    // Оптимизированный список слотов. Используется readonly для защиты от перевыделения памяти
    private readonly List<PartSlot> slots = new List<PartSlot>();

    // Вычисляемые на лету координаты для автоматического выравнивания слотов
    [SerializeField] private float chassisSlotY = 0f;      // Высота подъема родительского слота ходовой
    [SerializeField] private float chassisMeshLocalY = 0f; // Дополнительное локальное смещение для меша подвески
    [SerializeField] private float turretSlotY = 0f;       // Высота подъема слота башни пушки

    [SerializeField] private bool _isMovement = false;

    [SerializeField] private GameObject VisualRoot;

    //private Dictionary<VoxelMeshRender, bool> voxelMeshRenderList = new Dictionary<VoxelMeshRender, bool>();

    private void Awake()
    {
        // Сбрасываем счетчик активных детей конкретно этой машины при старте
        _activeCount = 0;

        //Setup();
        //// Автоматически запускаем процедурную сборку машины при старте сцены
        //AssembleVehicle().Forget();

        // Поиск слотов без выделения памяти (0 аллокаций)
        GetComponentsInChildren(true, slots);

        movementComponent = GetComponent<VehicleMovement>();
        inputComponent = GetComponent<PlayerVehicleInput>();
        weaponComponent = GetComponent<VehicleWeapon>();
        recoilComponent = GetComponent<VoxelRecoil>();
        wheelRotatorComponent = GetComponent<VehicleWheelRotator>();
    }

    /// <summary>
    /// Метод инициализации. Анализирует и подключает все части машины.
    /// </summary>
    public void Setup(bool isMovement)
    {
        if (VisualRoot != null && GameManager.Instance != null) {
            VisualRoot.transform.localScale = new Vector3(GameManager.Instance.Settings.scaleObjects, GameManager.Instance.Settings.scaleObjects, GameManager.Instance.Settings.scaleObjects);
        }

        _isMovement = isMovement;

        // меняем статус компонентов.
        movementComponent.enabled = _isMovement;
        inputComponent.enabled = _isMovement;

        Debug.LogWarning($"Setup");
        //if (!Application.isPlaying)
        //{
        //    voxelMeshRenderList
        //}
        LevelManager = GameObject.FindGameObjectWithTag("LevelManager")?.GetComponent<LevelManager>();


        //movementComponent = GetComponent<VehicleMovement>();
        //inputComponent = GetComponent<PlayerVehicleInput>();
        //weaponComponent = GetComponent<VehicleWeapon>();
        //recoilComponent = GetComponent<VoxelRecoil>();
        //wheelRotatorComponent = GetComponent<VehicleWheelRotator>();

        // ОПТИМИЗАЦИЯ: Если геймдизайнер подсунул стартовый пресет, 
        // распаковываем его данные в текущие слоты перед сборкой
        if (defaultPreset != null)
        {
            UnpackPreset(defaultPreset);
        }

    }

    /// <summary>
    /// Находит колесо с самым большим Bounds.y (высотой) среди всех настроенных слотов шасси.
    /// </summary>
    private Vector3 GetMaxWheelBounds(VehiclePresetWheelAsset wheelsPreset)
    {
        Vector3 maxBounds = Vector3.zero;

        if (wheelsPreset == null)
            return maxBounds;

        int slotCount = wheelsPreset.wheelSlots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            WheelPartAsset wheel = wheelsPreset.wheelSlots[i].wheelPartAsset;

            // ИСПРАВЛЕНО: Убрана ошибочная проверка структуры meshConfig != null
            // Мы сразу безопасно проверяем ссылку на Scriptable Object вокселей внутри конфигурации
            if (wheel != null && wheel.meshConfig.sOVoxelData != null)
            {
                Vector3 currentBounds = wheel.meshConfig.sOVoxelData.Bounds;

                // Если текущее колесо выше, чем ранее найденное — запоминаем его
                if (currentBounds.y > maxBounds.y)
                {
                    maxBounds.y = currentBounds.y;
                }

                //if (currentBounds.y > maxBounds.y)
                //{
                //    maxBounds.y = currentBounds.y;
                //}
            }
        }

        return maxBounds;
    }

    /// <summary>
    /// Полностью пересобирает визуальную структуру и обновляет параметры логики машины.
    /// </summary>
    public void AssembleVehicle()
    {
        //if (!Application.isPlaying)
        //{
        //    // очищаем объекты старые вручную, чтобы избежать утечки памяти в режиме Editor.
        //    //foreach (KeyValuePair<VoxelMeshRender, bool> item in voxelMeshRenderList) {
        //    //    item.Key.ClearContainers();
        //    //}


        //}
        DeSpawn();

        int totalHealth = 100; // Базовая прочность несущего голого каркаса
        float totalWeight = 0; // Базовый вес всей конструкции

        // Сбрасываем расчеты авто-высот перед новой сборкой
        chassisSlotY = 0f;
        chassisMeshLocalY = 0f;
        turretSlotY = 0f;

        // Узнаем масштаб вокселя из доступных деталей (используем размер из настроек sOVoxelData)
        float voxelSize = 0.1f;

        if (selectedChassis != null && selectedChassis.meshConfig.sOVoxelData != null)
            voxelSize = selectedChassis.meshConfig.sOVoxelData.sizeVoxel;
        else if (selectedTurret != null && selectedTurret.meshConfig.sOVoxelData != null)
            voxelSize = selectedTurret.meshConfig.sOVoxelData.sizeVoxel;

        // ВАШИ ИСПРАВЛЕННЫЕ ФОРМУЛЫ ВЫРАВНИВАНИЯ ВЫСОТ
        float wheelRadius = 0f;
        float chassisHalfWidth = 0f;

        Vector3 maxWheelBounds = GetMaxWheelBounds(selectedWheels);

        //if (selectedWheel != null && selectedWheel.meshConfig.sOVoxelData != null)
        //{
        //    // Находим половину высоты (радиус) колеса в метрах Unity
        //    wheelRadius = (selectedWheel.meshConfig.sOVoxelData.Bounds.y * 0.5f) * voxelSize;

        //    // Находим половину ширины колеса по оси X в метрах Unity
        //    wheelHalfWidth = (selectedWheel.meshConfig.sOVoxelData.Bounds.x * 0.5f) * voxelSize;

        //    // ПРАВИЛО 1: Родительский слот Chassis поднимаем ровно на половину высоты колеса
        //    chassisSlotY = wheelRadius;
        //}

        // Находим половину высоты (радиус) колеса в метрах Unity
        wheelRadius = (maxWheelBounds.y * 0.5f) * voxelSize;

        // ПРАВИЛО 1: Родительский слот Chassis поднимаем ровно на половину высоты колеса
        chassisSlotY = wheelRadius;

        if (selectedChassis != null && selectedChassis.meshConfig.sOVoxelData != null)
        {
            // Находим физическую половину высоты меша подвески
            float chassisHalfHeight = (selectedChassis.meshConfig.sOVoxelData.Bounds.y * 0.5f) * voxelSize;

            // Находим физическую полуширину рамы подвески по оси X в метрах Unity
            chassisHalfWidth = (selectedChassis.meshConfig.sOVoxelData.Bounds.x * 0.5f) * voxelSize;

            // ПРАВКА 2: Меш подвески дополнительно поднимаем локально на половину его собственной высоты * 0.5f + радиус колеса - размер вокселя
            chassisMeshLocalY = chassisHalfHeight - voxelSize; // * 0.5f + wheelRadius 

            // ПРАВКА 3: Вычисляем высоту слота башни (Полная высота подвески + половина высоты башни для её пивота)
            if (selectedTurret != null && selectedTurret.meshConfig.sOVoxelData != null)
            {
                float turretHalfHeight = (selectedTurret.meshConfig.sOVoxelData.Bounds.y * 0.5f) * voxelSize;
                turretSlotY = chassisMeshLocalY + chassisHalfHeight + turretHalfHeight + wheelRadius; // turretHalfHeight;
            }
            //Debug.LogWarning(
            //    $"Точка спавна подвески = {chassisHalfHeight}," +
            //    $"chassisSlotY={chassisSlotY}, " +
            //    $"chassisHalfWidth={chassisHalfWidth}," +
            //    $"chassisMeshLocalY={chassisMeshLocalY}," +
            //    $"turretSlotY={turretSlotY}");
        }



        // Обходим циклом все найденные на каркасе слоты
        int slotCount = slots.Count;
        for (int s = 0; s < slotCount; s++)
        {
            PartSlot slot = slots[s];

            //// TODO применение масштаба.
            //if (GameManager.Instance != null)
            //{
            //    slot.transform.localScale = new Vector3(GameManager.Instance.Settings.scaleObjects, GameManager.Instance.Settings.scaleObjects, GameManager.Instance.Settings.scaleObjects);
            //}

            //// Если в слоте уже была старая деталь — уничтожаем её
            //if (slot.CurrentInstalledPart != null)
            //{
            //    if (Application.isPlaying)
            //    {
            //        Destroy(slot.CurrentInstalledPart); // Работает, если игра ЗАПУЩЕНА
            //    }
            //    else
            //    {
            //        DestroyImmediate(slot.CurrentInstalledPart); // Работает в РЕДАКТОРЕ без запуска игры
            //    }
            //    //Destroy(slot.CurrentInstalledPart);
            //}


            // --- 1. СБОРКА ХОДОВОЙ ЧАСТИ ---
            if (slot.slotType == PartSlot.SlotType.Chassis && selectedChassis != null && selectedChassis.meshConfig.sOVoxelData != null)
            {
                bool hasChassisLocal = selectedChassis.text.title != null && !selectedChassis.text.title.IsEmpty;
                string chassisName = hasChassisLocal ? selectedChassis.text.title.GetLocalizedString() : selectedChassis.partName;

                Vector3 chassisSlotPos = slot.transform.localPosition;
                chassisSlotPos.y = chassisSlotY;
                slot.transform.localPosition = chassisSlotPos;

                GameObject chassisInstance = slot.gameObject; // new GameObject(chassisName);
                //chassisInstance.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
                //chassisInstance.transform.SetParent(slot.transform);
                //chassisInstance.transform.localScale = Vector3.one;
                slot.CurrentInstalledPart = chassisInstance;
                //slot.transform.localScale = Vector3.one;

                PoolNode chassisMeshObj = CreateVoxelSubPart(
                    "Chassis_Base_Mesh",
                    slot.transform,
                    Quaternion.identity,
                    new Vector3(0f, chassisMeshLocalY, 0f),
                    selectedChassis.meshConfig.sOVoxelData,
                    selectedChassis.meshConfig,
                    selectedChassis.colorsModifies
                );
                //chassisMeshObj.transform.localPosition = new Vector3(0f, chassisMeshLocalY, 0f);

                totalHealth += selectedChassis.bonusHealth;
                totalWeight += selectedChassis.mass;

                //if (selectedWheels != null && selectedWheels.wheelSlots.Count > 0)
                //{
                //    int wheelCount = selectedWheels.wheelSlots.Count;
                //    List<PoolNode> rotatableWheelTransforms = new List<PoolNode>();

                //    for (int i = 0; i < wheelCount; i++)
                //    {
                //        WheelSlotConfig wheelConfig = selectedWheels.wheelSlots[i];
                //        WheelPartAsset currentWheelAsset = wheelConfig.wheelPartAsset;
                //        //if (currentWheelAsset == null || currentWheelAsset.meshConfig.sOVoxelData != null) continue;
                //        if (currentWheelAsset == null || currentWheelAsset.meshConfig.sOVoxelData == null) continue;

                //        Vector3 offsetInVoxels = wheelConfig.offsetInVoxels;
                //        float currentWheelHalfWidth = (currentWheelAsset.meshConfig.sOVoxelData.Bounds.x * 0.5f) * voxelSize;
                //        float finalPositionX = (offsetInVoxels.x >= 0) ?
                //            (chassisHalfWidth - currentWheelHalfWidth) - voxelSize + offsetInVoxels.x:
                //            -(chassisHalfWidth - currentWheelHalfWidth) + voxelSize + offsetInVoxels.x;

                //        Vector3 mountPosition = new Vector3(finalPositionX, offsetInVoxels.y * voxelSize, offsetInVoxels.z * voxelSize);

                //        Quaternion rot = mountPosition.x < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(0f, 0, 0f);

                //        PoolNode wheelObj = CreateVoxelSubPart(
                //            $"Wheel_{i}",
                //            chassisInstance.transform,
                //            rot,
                //            mountPosition,
                //            currentWheelAsset.meshConfig.sOVoxelData,
                //            currentWheelAsset.meshConfig,
                //            currentWheelAsset.colorsModifies
                //        );
                //        //wheelObj.transform.localPosition = mountPosition;

                //        if (wheelConfig.isRotatable) rotatableWheelTransforms.Add(wheelObj);

                //        totalHealth += currentWheelAsset.bonusHealth;
                //        totalWeight += currentWheelAsset.weight;
                //    }
                //    if (wheelRotatorComponent != null && rotatableWheelTransforms.Count > 0)
                //    {
                //        wheelRotatorComponent.SetupWheels(rotatableWheelTransforms, LevelManager);
                //    } else
                //    {
                //        wheelRotatorComponent.enabled = false;
                //    }
                //}
                if (selectedWheels != null && selectedWheels.wheelSlots.Count > 0)
                {
                    int wheelCount = selectedWheels.wheelSlots.Count;
                    List<PoolNode> rotatableWheelTransforms = new List<PoolNode>();

                    // ДОБАВЛЕНО: Быстрый список для сохранения проектных координат чертежа
                    List<Vector3> wheelAssemblyPositions = new List<Vector3>();

                    for (int i = 0; i < wheelCount; i++)
                    {
                        WheelSlotConfig wheelConfig = selectedWheels.wheelSlots[i];
                        WheelPartAsset currentWheelAsset = wheelConfig.wheelPartAsset;
                        if (currentWheelAsset == null || currentWheelAsset.meshConfig.sOVoxelData == null) continue;

                        Vector3 offsetInVoxels = wheelConfig.offsetInVoxels;
                        float currentWheelHalfWidth = (currentWheelAsset.meshConfig.sOVoxelData.Bounds.x * 0.5f) * voxelSize;
                        float finalPositionX = (offsetInVoxels.x >= 0) ?
                            (chassisHalfWidth - currentWheelHalfWidth) - voxelSize + offsetInVoxels.x :
                            -(chassisHalfWidth - currentWheelHalfWidth) + voxelSize + offsetInVoxels.x;

                        Vector3 mountPosition = new Vector3(finalPositionX, offsetInVoxels.y * voxelSize, offsetInVoxels.z * voxelSize);

                        Quaternion rot = mountPosition.x < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(0f, 0, 0f);

                        PoolNode wheelObj = CreateVoxelSubPart(
                            $"Wheel_{i}",
                            chassisInstance.transform,
                            rot,
                            mountPosition,
                            currentWheelAsset.meshConfig.sOVoxelData,
                            currentWheelAsset.meshConfig,
                            currentWheelAsset.colorsModifies
                        );

                        if (wheelConfig.isRotatable)
                        {
                            rotatableWheelTransforms.Add(wheelObj);

                            // ДОБАВЛЕНО: Запоминаем mountPosition именно этого колеса до того, как данные уйдут в пул
                            wheelAssemblyPositions.Add(mountPosition);
                        }

                        totalHealth += currentWheelAsset.bonusHealth;
                        totalWeight += currentWheelAsset.mass;
                    }

                    // ИЗМЕНЕНО: Передаем в ротатор колес оба списка (ноды и их проектные оффсеты)
                    if (wheelRotatorComponent != null && rotatableWheelTransforms.Count > 0)
                    {
                        wheelRotatorComponent.SetupWheels(rotatableWheelTransforms, wheelAssemblyPositions, LevelManager);
                    }
                    else
                    {
                        if (wheelRotatorComponent != null) wheelRotatorComponent.enabled = false;
                    }
                }
                if (movementComponent != null) { movementComponent.Setup(selectedWheels.moveSpeed, selectedWheels.rotationSpeed, LevelManager); movementComponent.enabled = true; }
            }
            else if (slot.slotType == PartSlot.SlotType.Chassis && movementComponent != null) movementComponent.enabled = false;

            // --- 2. СБОРКА АВТО-ОРУЖЕЙНОЙ СИСТЕМЫ (БАШНЯ + СТВОЛЫ) ---
            if (slot.slotType == PartSlot.SlotType.MainWeapon)
            {
                if (selectedTurret != null && selectedTurret.meshConfig.sOVoxelData != null)
                {
                    bool hasTurretLocal = selectedTurret.text.title != null && !selectedTurret.text.title.IsEmpty;
                    string gunName = hasTurretLocal ? selectedTurret.text.title.GetLocalizedString() : selectedTurret.partName;

                    // Передвигаем сам слот башни вверх по вашей проверенной формуле
                    Vector3 localSlotPos = slot.transform.localPosition;
                    localSlotPos.y = turretSlotY;
                    slot.transform.localPosition = localSlotPos;

                    // Создаем контейнер для всей оружейной системы на крыше подвески
                    GameObject gunInstance = slot.gameObject; // new GameObject(gunName);
                    //gunInstance.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
                    //gunInstance.transform.SetParent(slot.transform);
                    //gunInstance.transform.localScale = Vector3.one;
                    slot.CurrentInstalledPart = gunInstance;

                    // Генерируем воксельный меш башни пушки
                    CreateVoxelSubPart(
                        "Base_Mesh",
                        gunInstance.transform,
                        Quaternion.identity,
                        Vector3.zero,
                        selectedTurret.meshConfig.sOVoxelData,
                        selectedTurret.meshConfig,
                        selectedTurret.colorsModifies
                    );

                    totalHealth += selectedTurret.bonusHealth;
                    totalWeight += selectedTurret.mass;

                    int barrelCount = selectedTurret.barrelMountOffsets.Count;
                    List<Transform> barrelTransforms = new List<Transform>(barrelCount);
                    // Создаем быстрый список оффсетов стволов (точно так же, как делали для колес)
                    List<Vector3> barrelAssemblyOffsets = new List<Vector3>();

                    // Кэшируем расчет локального вектора кончика дула ОДИН раз
                    Vector3 firePointLocalOffset = Vector3.zero;
                    bool hasBarrelData = selectedBarrelWeapon != null && selectedBarrelWeapon.meshConfig.sOVoxelData != null;

                    if (hasBarrelData)
                    {
                        SOVoxelData stvolData = selectedBarrelWeapon.meshConfig.sOVoxelData;

                        // Кончик дула находится на расстоянии ПОЛОВИНЫ его длины (Bounds.z * 0.5f) вперед по оси Z от центра ствола
                        firePointLocalOffset = new Vector3(
                            0f,
                            0f,
                            (stvolData.Bounds.z * 0.5f) * voxelSize
                        );
                    }

                    // Спавним стволы во все оружейные гнезда башни
                    if (hasBarrelData && selectedBarrelWeapon.attackStrategy != null)
                    {
                        // Смещение стволов по высоте Y теперь полностью обнулено 
                        float turretHalfHeight = 0;

                        for (int i = 0; i < barrelCount; i++)
                        {
                            Vector3Int offsetInVoxels = selectedTurret.barrelMountOffsets[i];

                            Vector3 mountPosition = new Vector3(
                                offsetInVoxels.x * voxelSize,
                                turretHalfHeight + (offsetInVoxels.y * voxelSize),
                                offsetInVoxels.z * voxelSize + selectedTurret.meshConfig.sOVoxelData.Bounds.z * 0.5f
                            );

                            // Создаем i-й объект ствола пушки
                            PoolNode barrelObj = CreateVoxelSubPart(
                                $"Muzzle_Mesh_{i}",
                                gunInstance.transform,
                                Quaternion.identity,
                                mountPosition,
                                selectedBarrelWeapon.meshConfig.sOVoxelData,
                                selectedBarrelWeapon.meshConfig,
                                selectedBarrelWeapon.colorsModifies
                            );
                            //barrelObj.transform.localPosition = mountPosition;
                            barrelTransforms.Add(barrelObj.Transform);
                            barrelAssemblyOffsets.Add( mountPosition );
                        }
                    }

                    // Передаем стратегию и массивы в логику стрельбы
                    if (weaponComponent != null && hasBarrelData && selectedBarrelWeapon.attackStrategy != null)
                    {
                        weaponComponent.SetupMultiBarrelOptimized(selectedBarrelWeapon, barrelTransforms, barrelAssemblyOffsets, firePointLocalOffset, LevelManager, gunInstance.transform);
                        weaponComponent.enabled = true;

                        weaponComponent.ActivateComponent(selectedBarrelWeapon.soundShot);
                        ////if (TryGetComponent<AudioSource>(out var audioSource))
                        //if (GetComponent<AudioSource>() is AudioSource audioSource)
                        //{
                        //    audioSource.clip = selectedBarrelWeapon.soundShot;
                        //}
                    }
                    else if (weaponComponent != null)
                    {
                        weaponComponent.enabled = false;
                    }

                    // Инициализируем систему анимации отдачи стволов
                    if (recoilComponent != null && barrelTransforms.Count > 0)
                    {
                        recoilComponent.SetupMultiRecoil(barrelTransforms);
                    }
                }
                else if (weaponComponent != null)
                {
                    weaponComponent.enabled = false;
                }
            }
        }
    }

    private void DeSpawn()
    {
        if (weaponComponent != null)
        {
            weaponComponent.DeactivateComponent();
        }
        // Защита от вызова, если активных элементов и так нет
        if (_activeCount == 0 || _activePoolHandle == null) return;

        // Идем с конца массива на случай, если порядок элементов важен
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            PoolHandle handle = _activePoolHandle[i];

            // Проверяем, действительно ли хэндл в этой ячейке валидный
            if (handle.IsValid)
            {
                // Сила взрыва при отсоединении/деспавне
                Vector3 explosionForce = Vector3.up * 10f;

                // Возвращаем объект в пул
                LevelManager.PoolVoxelMeshRender.DespawnSafe(handle, explosionForce, ForceMode.Impulse);

                // КРИТИЧЕСКИ ВАЖНО: Затираем хэндл, чтобы он больше не считался активным
                _activePoolHandle[i] = PoolHandle.Invalid;
            }
        }

        // Сбрасываем счетчик активных элементов
        _activeCount = 0;
    }

    /// <summary>
    /// Создает GameObject, вешает VoxelMeshRender, инициализирует его через SetConfig/SetColorsModify и вызывает Init().
    /// </summary>
    //async private UniTask<GameObject> CreateVoxelSubPart(string name, Transform parent, SOVoxelData voxelData, MeshConfig baseConfig, List<ColorsModify> modifies)
    //{
    //    // ОПТИМИЗИРОВАНО: Объект создается СРАЗУ внутри родителя и СРАЗУ с нужным компонентом.
    //    // 1 проход в памяти вместо 3.
    //    // GameObject subPart = new GameObject(name, typeof(VoxelMeshRender));
    //    GameObject subPart = await LevelManager.PoolVoxelMeshRender.GetObject();

    //    // Сразу передаем родителю (в Unity новые перегрузки делают это мгновенно без пересчета сцены)
    //    subPart.transform.SetParent(parent, false);

    //    // Вместо тяжелого AddComponent просто забираем УЖЕ СОЗДАННЫЙ компонент через GetComponent
    //    VoxelMeshRender voxelRenderer = subPart.GetComponent<VoxelMeshRender>();

    //    voxelRenderer.tag = parent.tag;
    //    voxelRenderer.gameObject.layer = parent.gameObject.layer;

    //    MeshConfig instanceConfig = baseConfig;
    //    instanceConfig.sOVoxelData = voxelData;

    //    voxelRenderer.SetConfig(instanceConfig);
    //    voxelRenderer.SetColorsModify(modifies);

    //    voxelRenderer.Init();

    //    //if (!Application.isPlaying) {
    //    //}
    //    voxelMeshRenderList.Add(voxelRenderer, true);

    //    return subPart;
    //}

    /// <summary>
    /// Создает PoolNode, где есть VoxelMeshRender, инициализирует его через SetConfig/SetColorsModify и вызывает Init().
    /// </summary>
    private PoolNode CreateVoxelSubPart(string name, Transform parent, Quaternion rotation, Vector3 positionOffset, SOVoxelData voxelData, MeshConfig baseConfig, List<ColorsModify> modifies)
    {
        // ОПТИМИЗИРОВАНО
        //PoolHandle poolHandle = LevelManager.PoolVoxelMeshRender.SpawnAttachedSafe(parent.position, Quaternion.identity, parent, null, this, out PoolNode node);

        PoolHandle poolHandle = LevelManager.PoolVoxelMeshRender.SpawnSafeUniversal(
            positionOffset,
            rotation,
            out PoolNode node,
            lifeTime: 0f,
            parentTransform: parent,
            parentRb: null,
            parentPoolEntity: this   // Передаем себя (машину) для автоочистки);
        );

        if (node.EntityLogic is VoxelMeshRender voxelRenderer)
        {
            // 0 аллокаций, мгновенный доступ
            voxelRenderer.tag = parent.tag;
            voxelRenderer.gameObject.layer = parent.gameObject.layer;

            MeshConfig instanceConfig = baseConfig;
            instanceConfig.sOVoxelData = voxelData;

            voxelRenderer.SetConfig(instanceConfig);
            voxelRenderer.SetColorsModify(modifies);

            voxelRenderer.Init();

            RegisterChild(node.VirtualChild);
        }

        //if (!Application.isPlaying) {
        //voxelMeshRenderList.Add(voxelRenderer, true);
        //}
        if (poolHandle.IsValid)
        {
            // 2. Сохраняем хэндл в массив вызывающего файла
            _activePoolHandle[_activeCount] = poolHandle;
            _activeCount++;
        }

        return node;
    }

    /// <summary>
    /// Ультимативный метод для смены конфигурации машины на лету.
    /// Принимает всего один файл пресета, полностью очищает старую технику и собирает новую.
    /// </summary>
    public void ChangeEquipment(VehiclePresetAsset newPreset)
    {
        if (newPreset == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Попытка установить пустой пресет техники!");
            return;
        }

        // 1. Распаковываем ссылки на запчасти из единого файла конфигурации
        UnpackPreset(newPreset);

        // 2. Даем команду полностью пересобрать воксельные меши, высоты, оффсеты и логику
        AssembleVehicle();

        //Debug.Log($"[{gameObject.name}] Успешно пересобран в конфигурацию: {newPreset.presetName}");
    }

    /// <summary>
    /// Вспомогательный приватный метод для быстрой распаковки SO-пресета в рабочие ссылки
    /// </summary>
    private void UnpackPreset(VehiclePresetAsset preset)
    {
        selectedChassis = preset.chassis;
        selectedTurret = preset.tower;
        selectedBarrelWeapon = preset.muzzle;
        selectedWheels = preset.wheelsPreset;
    }

    /// <summary>
    /// Публичный метод для отправки команды на выстрел. 
    /// Вызывается из скриптов ввода (Player) или искусственного интеллекта (AI).
    /// </summary>
    public void FireWeapon()
    {
        // Проверяем, установлен ли компонент оружия и включен ли он (собрана ли пушка)
        if (weaponComponent != null && weaponComponent.enabled)
        {
            weaponComponent.Shoot(); // Вызываем публичный метод из VehicleWeapon
        }
    }

    protected override void OnSpawn()
    {
        //Setup();
        //AssembleVehicle();

        //Debug.LogWarning($"OnSpawn: {_isMovement}");
    }

    protected override void OnDespawn()
    {
        //Debug.LogWarning($"OnDespawn: {_isMovement}");

        // Очищаем настройки слотов
        int slotCount = slots.Count;
        for (int s = 0; s < slotCount; s++)
        {
            PartSlot slot = slots[s];
            slot.transform.localPosition = Vector3.zero;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
        }

        // меняем статус компонентов.
        movementComponent.enabled = false;
        inputComponent.enabled = false;
    }
}
