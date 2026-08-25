using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент отдачи на корпусе машины. Не использует LateUpdate.
/// Напрямую меняет вектор смещения универсального компонента VirtualChild.
/// </summary>
public class VoxelRecoil : MonoBehaviour
{
    [Header("Настройки отдачи")]
    [SerializeField] private float kickbackDistance = 0.2f; // Дистанция отката в метрах
    [SerializeField] private float returnDuration = 0.15f;  // Время возвращения в секундах

    // Работаем со стандартным списком VirtualChild, который выдал сборщик
    private List<VirtualChild> barrelComponents = new List<VirtualChild>();
    private float[] _currentOffsets;
    private int _count;

    /// <summary>
    /// Инициализация из сборщика автомобиля. Принимает чистые трансформы стволов.
    /// </summary>
    public void SetupMultiRecoil(List<Transform> barrelTransforms)
    {
        _count = barrelTransforms.Count;
        barrelComponents.Clear();
        barrelComponents.Capacity = _count;

        _currentOffsets = new float[_count];

        for (int i = 0; i < _count; i++)
        {
            // Находим стандартный VirtualChild, который гарантированно есть на объекте пула
            VirtualChild vChild = barrelTransforms[i].GetComponent<VirtualChild>();
            barrelComponents.Add(vChild);
            _currentOffsets[i] = 0f;
        }
    }

    public void TriggerSingleRecoil(int index)
    {
        if (_currentOffsets == null || index < 0 || index >= _count) return;

        // Взводим максимальный откат для ствола
        _currentOffsets[index] = kickbackDistance;

        if (barrelComponents[index] != null)
        {
            // Записываем смещение назад по оси Z в универсальный компонент
            barrelComponents[index].ExternalPositionOffset = new Vector3(0f, 0f, -kickbackDistance);
        }
    }

    private void Update()
    {
        if (_currentOffsets == null) return;

        float returnSpeed = kickbackDistance / returnDuration;

        for (int i = 0; i < _count; i++)
        {
            if (_currentOffsets[i] > 0f)
            {
                // Плавно возвращаем откат к нулю
                _currentOffsets[i] -= returnSpeed * Time.deltaTime;

                if (_currentOffsets[i] < 0f) _currentOffsets[i] = 0f;

                if (barrelComponents[i] != null)
                {
                    // Обновляем вектор смещения в VirtualChild (минус по оси Z — это откат назад)
                    barrelComponents[i].ExternalPositionOffset = new Vector3(0f, 0f, -_currentOffsets[i]);
                }
            }
        }
    }
}


//using UnityEngine;
//using System.Collections.Generic;

///// <summary>
///// Анимирует поочередный откат стволов назад на чистой математике времени.
///// Не тратит ресурсы CPU, когда машина не ведет огонь.
///// </summary>
//public class VoxelRecoil : MonoBehaviour
//{
//    [Header("Настройки отдачи")]
//    [SerializeField] private float kickbackDistance = 0.2f; // Расстояние отката ствола назад
//    [SerializeField] private float returnDuration = 0.15f;  // Время плавного возвращения в покой (в секундах)

//    private List<Transform> barrels = new List<Transform>();
//    private readonly List<Vector3> originalLocalPositions = new List<Vector3>();

//    private Transform _turretMesh; // Ссылка на стабильный трансформ башни

//    // Хранилище меток времени выстрела для каждого ствола
//    private float[] lastShotTimes;

//    // Переключатель «сна»: если false, метод LateUpdate мгновенно засыпает на 1-й строчке
//    private bool isAnyBarrelRecoilActive = false;

//    /// <summary>
//    /// Инициализация параметров отдачи из сборщика. Запоминает начальное положение деталей.
//    /// </summary>
//    public void SetupMultiRecoil(List<Transform> newBarrels, List<Vector3> barrelAssemblyOffsets, Transform turretMesh)
//    {
//        barrels = newBarrels;
//        int count = barrels.Count;
//        _turretMesh = turretMesh; // Запоминаем башню

//        originalLocalPositions.Clear();
//        originalLocalPositions.Capacity = count; // Защита от лишней переаллокации списка
//        lastShotTimes = new float[count];


//        for (int i = 0; i < count; i++)
//        {
//            //originalLocalPositions.Add(barrels[i].localPosition);

//            // ИСПРАВЛЕНО: Вместо "грязного" localPosition из Unity, 
//            // жестко записываем идеальную проектную позицию сборщика!
//            originalLocalPositions.Add(barrelAssemblyOffsets[i]);

//            lastShotTimes[i] = -999f; // Устанавливаем далекое прошлое время
//        }
//        isAnyBarrelRecoilActive = false;
//    }

//    /// <summary>
//    /// Вызывается в момент выстрела. Мгновенно толкает ствол назад в мировых координатах башни.
//    /// </summary>
//    public void TriggerSingleRecoil(int index)
//    {
//        if (index < 0 || index >= barrels.Count || barrels[index] == null) return;

//        lastShotTimes[index] = Time.time;

//        // РАСЧЕТ ДЛЯ ПЛОСКОГО ПУЛА: Ствол двигается назад относительно мирового направления трансформа башни/машины (this.transform)
//        Vector3 worldOrigin = transform.TransformPoint(originalLocalPositions[index]);
//        barrels[index].position = worldOrigin - transform.forward * kickbackDistance;

//        isAnyBarrelRecoilActive = true;
//    }

//    private void LateUpdate()
//    {
//        if (!isAnyBarrelRecoilActive) return;

//        bool hasActiveRecoil = false;
//        float currentTime = Time.time;
//        int count = barrels.Count;

//        // Кэшируем позицию и поворот башни/машины, чтобы не вызывать свойства в цикле
//        Transform vehicleTransform = this.transform;
//        Vector3 vehicleForward = vehicleTransform.forward;

//        for (int i = 0; i < count; i++)
//        {
//            Transform barrel = barrels[i];
//            if (barrel == null) continue;

//            float timePassed = currentTime - lastShotTimes[i];

//            // Находим текущую идеальную мировую позицию покоя для этого ствола прямо сейчас
//            Vector3 worldOrigin = vehicleTransform.TransformPoint(originalLocalPositions[i]);

//            if (timePassed < returnDuration)
//            {
//                Vector3 worldKickbackPos = worldOrigin - vehicleForward * kickbackDistance;

//                // Интерполируем СТРОГО в мировом пространстве (position вместо localPosition)
//                barrel.position = Vector3.Lerp(worldKickbackPos, worldOrigin, timePassed / returnDuration);
//                hasActiveRecoil = true;
//            }
//            else
//            {
//                // Время вышло — жестко фиксируем ствол в его проектной мировой точке
//                barrel.position = worldOrigin;
//            }
//        }

//        isAnyBarrelRecoilActive = hasActiveRecoil;
//    }


//    ///// <summary>
//    ///// Вызывается в момент выстрела. Мгновенно толкает ствол назад и будит LateUpdate.
//    ///// </summary>
//    //public void TriggerSingleRecoil(int index)
//    //{
//    //    if (index < 0 || index >= barrels.Count || barrels[index] == null) return;

//    //    // Запоминаем точный момент времени в секундах, когда произошел выстрел
//    //    lastShotTimes[index] = Time.time;

//    //    // Физически отбрасываем меш ствола назад по его локальной оси Z (вглубь башни)
//    //    barrels[index].localPosition = originalLocalPositions[index] - Vector3.forward * kickbackDistance;

//    //    // Пробуждаем математический обсчет в LateUpdate
//    //    isAnyBarrelRecoilActive = true;
//    //}

//    //private void LateUpdate()
//    //{
//    //    // УЛЬТИМАТИВНЫЙ ХАК ОПТИМИЗАЦИИ: если пушка спит, метод завершается тут же. 
//    //    // Нагрузка на процессор в режиме покоя равна ровно 0%.
//    //    if (!isAnyBarrelRecoilActive) return;

//    //    bool hasActiveRecoil = false;
//    //    float currentTime = Time.time;
//    //    int count = barrels.Count;

//    //    // Математический обсчет плавного возвращения стволов
//    //    for (int i = 0; i < count; i++)
//    //    {
//    //        Transform barrel = barrels[i];
//    //        if (barrel == null) continue;

//    //        // Сколько секунд прошло с момента выстрела из этого ствола
//    //        float timePassed = currentTime - lastShotTimes[i];

//    //        // Если ствол еще находится в процессе плавного возвращения назад
//    //        if (timePassed < returnDuration)
//    //        {
//    //            Vector3 origin = originalLocalPositions[i];
//    //            Vector3 kickbackPos = origin - Vector3.forward * kickbackDistance;

//    //            // Рассчитываем текущее положение между точкой отката и покоя на основе дельты времени
//    //            barrel.localPosition = Vector3.Lerp(kickbackPos, origin, timePassed / returnDuration);
//    //            hasActiveRecoil = true; // Сигнализируем, что как минимум один ствол еще движется
//    //        }
//    //        else
//    //        {
//    //            // Время возвращения вышло — жестко возвращаем деталь в исходную идеальную точку
//    //            barrel.localPosition = originalLocalPositions[i];
//    //        }
//    //    }

//    //    // Если абсолютно все стволы вернулись на место, выключаем триггер и усыпляем LateUpdate
//    //    isAnyBarrelRecoilActive = hasActiveRecoil;
//    //}
//}
