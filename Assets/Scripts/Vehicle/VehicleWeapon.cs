using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отвечает за логику задержки стрельбы, поочередную смену стволов и расчет точки вылета снаряда.
/// </summary>
public class VehicleWeapon : MonoBehaviour, IShootable
{
    [Header("Ссылки на независимые части")]
    [SerializeField] private Transform turretMesh;  // Визуальный меш башни (крутится независимо)
    //[SerializeField] private Transform barrelMesh;  // Визуальный меш ствола (опционально, для наклона)

    LevelManager LevelManager;
    private WeaponStrategy activeStrategy;      // Текущая стратегия атаки
    MuzzlePartAsset Asset;
    private List<Transform> barrels = new List<Transform>(); // Список трансформаций стволов
    private Vector3 firePointOffset;            // Внутренний локальный вектор кончика дула
    private float nextFireTime;                 // Таймер задержки между выстрелами

    private AudioSource _audioSource;
    private bool _hasAudio;

    private VoxelRecoil recoilComponent;
    private int currentBarrelIndex = 0;         // Индекс ствола, который выстрелит следующим

    // Единственный пустой объект-маркер, который перемещается математически вместо создания сотен пустых GameObject
    private Transform staticTransformMarker;
    [SerializeField] private float turretRotationSpeed = 270f; // Скорость поворота башни

    // Быстрая проверка, готова ли пушка к выстрелу
    public bool CanShoot => Time.time >= nextFireTime && activeStrategy != null && barrels.Count > 0;

    // Добавляем кэш проектных оффсетов стволов
    private List<Vector3> cachedBarrelOffsets = new List<Vector3>();

    private void Awake()
    {
        // Кэшируем строго 1 раз при старте сцены / инициализации префаба
        _audioSource = GetComponent<AudioSource>();
        _hasAudio = _audioSource != null;

        recoilComponent = GetComponent<VoxelRecoil>();

        // Создаем служебный маркер один раз при рождении машины
        GameObject markerObj = new GameObject("[Dynamic_FirePoint_Marker]");
        markerObj.transform.SetParent(transform);
        staticTransformMarker = markerObj.transform;
    }

    ///// <summary>
    ///// Настройка параметров стрельбы из сборщика. Устраняет необходимость спавнить объекты FirePoint.
    ///// </summary>
    //public void SetupMultiBarrelOptimized(MuzzlePartAsset asset, List<Transform> activeBarrels, Vector3 localOffset, LevelManager levelManager, Transform _turretMesh)
    //{
    //    turretMesh = _turretMesh;
    //    Asset = asset;
    //    LevelManager = levelManager;
    //    activeStrategy = asset.attackStrategy;
    //    barrels = activeBarrels;
    //    firePointOffset = localOffset;
    //    currentBarrelIndex = 0; // Сбрасываем очередь стволов
    //}
    /// <summary>
    /// Настройка параметров стрельбы из сборщика. 
    /// </summary>
    public void SetupMultiBarrelOptimized(
        MuzzlePartAsset asset,
        List<Transform> activeBarrels,
        List<Vector3> assemblyOffsets, // Принимаем проектные оффсеты из сборщика!
        Vector3 localOffset,
        LevelManager levelManager,
        Transform _turretMesh)
    {
        turretMesh = _turretMesh;
        Asset = asset;
        LevelManager = levelManager;
        activeStrategy = asset.attackStrategy;
        barrels = activeBarrels;
        cachedBarrelOffsets = assemblyOffsets; // Кэшируем оффсеты
        firePointOffset = localOffset;
        currentBarrelIndex = 0; // Сбрасываем очередь стволов
    }

    ///// <summary>
    ///// Производит выстрел из текущего по очереди ствола.
    ///// </summary>
    //public void Shoot()
    //{
    //    if (!CanShoot) return;

    //    // Рассчитываем время готовности следующего выстрела на основе скорострельности стратегии
    //    nextFireTime = Time.time + activeStrategy.fireRate;
    //    Transform currentBarrel = barrels[currentBarrelIndex];

    //    if (currentBarrel != null)
    //    {
    //        // Высокоскоростной перевод локального вектора дула в мировые координаты Unity
    //        Vector3 worldFirePosition = currentBarrel.TransformPoint(firePointOffset);

    //        // Мгновенно перемещаем и разворачиваем наш маркер в эту точку
    //        staticTransformMarker.SetPositionAndRotation(worldFirePosition, currentBarrel.rotation);

    //        // Передаем маркер в ScriptableObject стратегию выстрела для спавна пули/луча
    //        activeStrategy.ExecuteAttack(staticTransformMarker, gameObject, LevelManager);
    //    }

    //    // Воспроизводим звук выстрела пушки
    //    //if (audioSource != null && audioSource.clip != null) audioSource.Play();
    //    if (Asset.soundShot != null && VoxelAudioManager.Instance != null)
    //    {
    //        VoxelAudioManager.Instance.Play3DLayer(Asset.soundShot, transform.position, 1, 1, 1);
    //    }

    //    // Триггерим анимацию отдачи строго для того ствола, который сейчас выстрелил
    //    if (recoilComponent != null)
    //    {
    //        recoilComponent.TriggerSingleRecoil(currentBarrelIndex);
    //    }

    //    // Сдвигаем циклическую очередь к следующему стволу (0 -> 1 -> 2 -> 0)
    //    currentBarrelIndex = (currentBarrelIndex + 1) % barrels.Count;
    //}

    /// <summary>
    /// Производит выстрел из текущего по очереди ствола с гарантированной защитой от гонки матриц.
    /// </summary>
    public void Shoot()
    {
        if (!CanShoot) return;

        nextFireTime = Time.time + activeStrategy.fireRate;

        // РАСЧЕТ ДЛЯ ПЛОСКОГО ПУЛА (Zero Race-Condition):
        // Находим, где должен стоять ствол относительно БАШНИ по воксельному чертежу
        Vector3 localBarrelPosition = cachedBarrelOffsets[currentBarrelIndex];

        // Добавляем к нему локальный вектор кончика дула
        Vector3 totalLocalOffset = localBarrelPosition + firePointOffset;

        // Переводим ИТОГОВУЮ точку выстрела в мир через стабильный трансформ БАШНИ
        Vector3 worldFirePosition = turretMesh.TransformPoint(totalLocalOffset);

        // Направление выстрела и разворот маркера — это всегда чистый форвард башни
        Quaternion worldFireRotation = turretMesh.rotation;

        // Мгновенно выставляем наш маркер
        staticTransformMarker.SetPositionAndRotation(worldFirePosition, worldFireRotation);

        // Передаем маркер в стратегию выстрела
        activeStrategy.ExecuteAttack(staticTransformMarker, gameObject, LevelManager);

        // Воспроизводим звук выстрела
        if (Asset.soundShot != null && VoxelAudioManager.Instance != null)
        {
            VoxelAudioManager.Instance.Play3DLayer(Asset.soundShot, transform.position, 1, 1, 1);
        }

        // Триггерим отдачу
        if (recoilComponent != null)
        {
            recoilComponent.TriggerSingleRecoil(currentBarrelIndex);
        }

        currentBarrelIndex = (currentBarrelIndex + 1) % barrels.Count;
    }


    /// <summary>
    /// Функция независимого прицеливания башни, управляемая из скрипта ввода
    /// </summary>
    public void AimAt(Vector3 worldTarget)
    {
        if (turretMesh == null) return;

        // 1. Вычисляем направление от башни к точке прицеливания в мире
        Vector3 directionToTarget = worldTarget - turretMesh.position;
        directionToTarget.y = 0; // Нам нужен поворот только по горизонтали

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            // 2. Находим целевой мировой разворот
            Quaternion targetTurretRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

            // 3. Плавно вращаем башню к цели в мировых координатах.
            // Так как корень машины движется независимо, а мы используем иерархию "соседей"
            // (ChassisMesh и TurretMesh лежат раздельно под корнем), это вращение будет идеальным.
            turretMesh.rotation = Quaternion.RotateTowards(
                turretMesh.rotation,
                targetTurretRotation,
                turretRotationSpeed * Time.deltaTime
            );
        }
    }

    private void SetClip(AudioClip soundShot)
    {
        if (_audioSource == null) return;

        _audioSource.clip = soundShot;
    }

    /// <summary>
    /// Активация звука при спавне родительского пулл-объекта
    /// </summary>
    public void ActivateComponent(AudioClip clip)
    {
        if (_hasAudio)
        {
            SetClip(clip);

            _audioSource.enabled = true;
        }
    }

    /// <summary>
    /// Полное отключение звука при уходе родителя в архив пула
    /// </summary>
    public void DeactivateComponent()
    {
        if (_hasAudio)
        {
            _audioSource.Stop();          // Мгновенно глушим хвост звука
            _audioSource.enabled = false; // Отключаем просчет 3D-аудио в Unity
        }
    }
}
