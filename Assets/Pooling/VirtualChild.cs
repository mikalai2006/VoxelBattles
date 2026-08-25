using UnityEngine;

/// <summary>
/// Компонент для симуляции виртуального родительства (Virtual Parenting).
/// Привязанный объект полностью перенимает масштаб родителя один к одному.
/// </summary>
[DisallowMultipleComponent] // Запрещает вешать более одного такого скрипта на один GameObject
public class VirtualChild : MonoBehaviour
{
    public enum RotationMode
    {
        FullLock,
        FreeRollX,
        FreeRollY,
        FreeRollZ
    }
    [Header("Rotation Settings")]
    [SerializeField] private RotationMode rotationMode = RotationMode.FullLock;

    private float _currentFreeRollAngle;

    // Вектор оси вращения — кэшируем один раз, чтобы не создавать векторы в Update
    private Vector3 _rollAxisVector = Vector3.right;

    // Кэш собственных компонентов для оптимизации производительности
    private Transform _transform;
    private Rigidbody _rigidbody;
    private Collider _collider;

    // ДОБАВЛЕНО: Кэш ссылки на собственный PoolEntity, чтобы не искать его через GetComponent
    private PoolEntity _myPoolEntity;
    public PoolEntity MyPoolEntity => _myPoolEntity;

    // Ссылки на виртуального родителя
    private Transform _parentTransform;
    private Rigidbody _parentRigidbody;
    private PoolEntity _parentPoolEntity;

    // Смещения положения и поворота, вычисляемые в момент привязки
    private Vector3 _localOffsetPosition;    // Позиция в локальных координатах родителя
    private Quaternion _localOffsetRotation;  // Поворот относительно поворота родителя

    // Флаг текущего состояния привязки
    [SerializeField, Mikalai2006.Utils.ReadOnly] private bool _isAttached;

    /// <summary>
    /// Свойство для проверки, привязан ли объект в данный момент.
    /// </summary>
    public bool IsAttached => _isAttached;

    // Вектор дополнительного мирового или локального смещения (например, для отдачи)
    private Vector3 _externalPositionOffset = Vector3.zero;
    /// <summary>
    /// Публичное свойство для наложения внешних эффектов (отдача, тряска) без интерфейсов.
    /// </summary>
    public Vector3 ExternalPositionOffset
    {
        get => _externalPositionOffset;
        set => _externalPositionOffset = value;
    }

    /// <summary>
    /// Инициализация компонента. Кэширует ссылки, чтобы избежать вызовов GetComponent в рантайме.
    /// </summary>
    public void Initialize()
    {
        // Кэшируем один раз при создании/инициализации
        _transform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _myPoolEntity = GetComponent<PoolEntity>();
    }

    /// <summary>
    /// Привязывает объект к виртуальному родителю.
    /// </summary>
    /// <param name="parentTransform">Transform родителя, за которым нужно следовать.</param>
    /// <param name="parentRb">Rigidbody родителя (передать null, если родитель безфизический).</param>
    // ДОБАВЛЕНО: parentPoolEntity передается напрямую из менеджера/родителя при спавне
    public void Attach(Transform parentTransform, Rigidbody parentRb, PoolEntity parentPoolEntity)
    {
        _parentTransform = parentTransform;
        _parentRigidbody = parentRb;

        // Прямое присвоение закэшированной ссылки родителя без каких-либо GetComponentInParent!
        _parentPoolEntity = parentPoolEntity;

        // Переводим текущую мировую позицию объекта в локальное пространство родителя
        _localOffsetPosition = parentTransform.InverseTransformPoint(_transform.position);

        // Вычисляем локальный поворот объекта относительно родителя
        _localOffsetRotation = Quaternion.Inverse(parentTransform.rotation) * _transform.rotation;

        // Если родитель является объектом пула, автоматически регистрируемся в его списке для автоочистки
        if (_parentPoolEntity != null)
        {
            _parentPoolEntity.RegisterChild(this);
        }

        // Если у объекта есть физика, настраиваем её для корректного следования
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;

            // ХАК ДЛЯ РОДИТЕЛЕЙ БЕЗ RIGIDBODY: 
            if (_parentRigidbody == null)
            {
                _rigidbody.detectCollisions = false;
            }
        }

        // Выставляем флаг успешной привязки
        _isAttached = true;

        // КРИТИЧЕСКИЙ ФИКС: Включаем компонент ТОЛЬКО в момент реальной привязки!
        this.enabled = true;
    }

    //public void Attach(Transform parentTransform, Rigidbody parentRb)
    //{
    //    _parentTransform = parentTransform;
    //    _parentRigidbody = parentRb;

    //    // Переводим текущую мировую позицию объекта в локальное пространство родителя
    //    _localOffsetPosition = parentTransform.InverseTransformPoint(_transform.position);

    //    // Вычисляем локальный поворот объекта относительно родителя
    //    _localOffsetRotation = Quaternion.Inverse(parentTransform.rotation) * _transform.rotation;

    //    // Если у объекта есть физика, настраиваем её для корректного следования
    //    if (_rigidbody != null)
    //    {
    //        // Делаем Rigidbody кинематическим, чтобы PhysX не двигал его гравитацией или силами
    //        _rigidbody.isKinematic = true;

    //        // ХАК ДЛЯ РОДИТЕЛЕЙ БЕЗ RIGIDBODY: 
    //        // Если родитель движется в обычном Update, PhysX принудительно блокирует ручное перемещение 
    //        // коллайдеров. Отключение detectCollisions заставляет PhysX полностью игнорировать этот коллайдер.
    //        if (_parentRigidbody == null)
    //        {
    //            _rigidbody.detectCollisions = false;
    //        }
    //    }

    //    // Выставляем флаг успешной привязки
    //    _isAttached = true;
    //}

    public void SetRotationMode(RotationMode mode)
    {
        rotationMode = mode;
        _currentFreeRollAngle = 0f;

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Полностью обнуляем накопленный угол качения 
        // предыдущей сессии объекта, чтобы колеса не сходили с ума при пересоздании!
        _currentFreeRollAngle = 0f;

        // Кэшируем вектор оси один раз при настройке, освобождая Update от условий switch
        switch (mode)
        {
            case RotationMode.FreeRollX: _rollAxisVector = Vector3.right; break;
            case RotationMode.FreeRollY: _rollAxisVector = Vector3.up; break;
            case RotationMode.FreeRollZ: _rollAxisVector = Vector3.forward; break;
        }
    }

    public void AddRollRotation(float angleDelta)
    {
        _currentFreeRollAngle += angleDelta;

        // Защита от бесконечного роста числа (переполнения float)
        if (_currentFreeRollAngle > 360f) _currentFreeRollAngle -= 360f;
        if (_currentFreeRollAngle < -360f) _currentFreeRollAngle += 360f;
    }

    /// <summary>
    /// Обновление трансформации для безфизического родителя. Вызывается каждый кадр из Update менеджера.
    /// </summary>
    public void UpdateTick()
    {
        // Если объект не привязан — ничего не делаем
        if (!_isAttached) return;

        // Строго если у родителя НЕТ физического тела
        if (_parentRigidbody == null)
        {
            //// Вычисляем и применяем новую мировую позицию на основе локального смещения
            //_transform.position = _parentTransform.TransformPoint(_localOffsetPosition);

            //// Вычисляем и применяем новый мировой поворот с учетом вращения родителя
            ////_transform.rotation = _parentTransform.rotation * _localOffsetRotation;
            //_transform.rotation = CalculateOptimizedRotation();

            //// Применяем масштаб: объект полностью забирает итоговый глобальный масштаб родителя (lossyScale)
            //_transform.localScale = _parentTransform.lossyScale;

            // 1. Берем базовый проектный оффсет из чертежа сборщика
            Vector3 finalLocalPos = _localOffsetPosition;

            // 2. Просто прибавляем внешнее смещение (если оно было задано скриптом отдачи)
            finalLocalPos += _externalPositionOffset;

            // 3. Применяем итоговую чистую мировую координату за ОДИН вызов
            // Работаем напрямую с Transform (Zero-Alloc, O(1) скорость)
            _transform.position = _parentTransform.TransformPoint(finalLocalPos);
            _transform.localScale = _parentTransform.lossyScale;

            // Рассчитываем наше оптимизированное вращение (с учетом качения колес через AngleAxis)
            _transform.rotation = CalculateOptimizedRotation();
        }
    }

    /// <summary>
    /// Обновление трансформации для физического родителя. Вызывается каждый кадр из FixedUpdate менеджера.
    /// </summary>
    public void FixedUpdateTick()
    {
        // Если объект не привязан — ничего не делаем
        if (!_isAttached) return;

        // Если и у родителя, и у объекта есть физические тела
        if (_parentRigidbody != null && _rigidbody != null)
        {
            // Находим целевую мировую позицию и поворот для текущего физического кадра
            Vector3 targetPosition = _parentTransform.TransformPoint(_localOffsetPosition);
            Quaternion targetRotation = _parentTransform.rotation * _localOffsetRotation;

            // Двигаем Rigidbody через физический движок для плавной интерполяции и обсчета триггеров
            _rigidbody.MovePosition(targetPosition);
            //_rigidbody.MoveRotation(targetRotation);
            _rigidbody.MoveRotation(CalculateOptimizedRotation());

            // Физический движок Unity (PhysX) принципиально не умеет менять масштаб через Rigidbody.
            // Поэтому масштаб всегда меняем напрямую через Transform, даже внутри FixedUpdate.
            _transform.localScale = _parentTransform.lossyScale;
        }
    }

    /// <summary>
    /// Самый быстрый способ наложения локального вращения в Unity (Zero-Alloc, Минуя тяжелый Euler)
    /// </summary>
    private Quaternion CalculateOptimizedRotation()
    {
        Quaternion baseTargetRot = _parentTransform.rotation * _localOffsetRotation;

        if (rotationMode == RotationMode.FullLock) return baseTargetRot;

        // AngleAxis работает на уровне шейдерной/матричной математики процессора напрямую, 
        // это самая быстрая генерация вращения вокруг одной оси в Unity.
        return baseTargetRot * Quaternion.AngleAxis(_currentFreeRollAngle, _rollAxisVector);
    }

    /// <summary>
    /// Отвязывает объект от виртуального родителя, возвращая его в свободное физическое состояние.
    /// </summary>
    /// <param name="explosionForce">Импульс или сила, прикладываемая к объекту в момент отрыва.</param>
    /// <param name="mode">Режим приложения силы (например, ForceMode.Impulse).</param>
    public void Detach(Vector3 explosionForce, ForceMode mode)
    {
        // Если объект и так не был привязан — выходим
        if (!_isAttached) return;

        // Сбрасываем флаг привязки
        _isAttached = false;

        // КРИТИЧЕСКИЙ ФИКС: Гасим компонент при отрыве!
        // Теперь объект больше не считается "виртуальным ребенком" и полностью переходит под контроль PhysX
        this.enabled = false;

        //// Если у объекта есть физика, возвращаем её в стандартное динамическое состояние
        //if (_rigidbody != null)
        //{
        //    _rigidbody.isKinematic = false;     // Выключаем кинематику, возвращаем влияние гравитации
        //    _rigidbody.detectCollisions = true;  // Возвращаем регистрацию коллизий в PhysX

        //    // Если родитель был физическим, передаем объекту скорость родителя в точке отрыва,
        //    // чтобы объект сохранил инерцию (например, вылетел по ходу движения машины)
        //    if (_parentRigidbody != null)
        //    {
        //        // Используем современный linearVelocity вместо устаревшего velocity (для Unity 2023+)
        //        _rigidbody.linearVelocity = _parentRigidbody.GetPointVelocity(_transform.position);
        //    }

        //    // Если была передана сила отсоединения, прикладываем её к объекту
        //    if (explosionForce != Vector3.zero)
        //    {
        //        _rigidbody.AddForce(explosionForce, mode);
        //    }
        //}

        // Обнуляем угол качения прямо при отсоединении (взрыве/деспавне)
        _currentFreeRollAngle = 0f;

        //_transform.localPosition = Vector3.zero;
        //_transform.localRotation = Quaternion.identity;
        //_transform.localScale = Vector3.one;

        //_localOffsetPosition = Vector3.zero;
        //_localOffsetRotation = Quaternion.identity;

        // Полностью очищаем ссылки на бывшего родителя, чтобы избежать утечек памяти
        _parentTransform = null;
        _parentRigidbody = null;
    }

    /// <summary>
    /// Полный сброс параметров объекта при возврате в UltraVirtualPool.
    /// Вызывается СТРОГО в момент деспавна, обнуляя трансформы до базового состояния.
    /// </summary>
    public void DespawnReset()
    {
        // 1. Снимаем флаг активности (если объект не был отсоединен через Detach ранее)
        _isAttached = false;

        // 2. СБРОС СМЕЩЕНИЯ В ПУЛЕ
        _externalPositionOffset = Vector3.zero;

        //// 2. ОБНУЛЯЕМ ТРАНСФОРМЫ (для следующей сессии спавна из пула)
        //if (_transform != null)
        //{
        //    _transform.localPosition = archivePosition; // Vector3.zero;
        //    _transform.localRotation = Quaternion.identity;
        //    _transform.localScale = Vector3.one;
        //}

        // 3. Полностью обнуляем математику вращения
        _currentFreeRollAngle = 0f;
        rotationMode = RotationMode.FullLock;
        _rollAxisVector = Vector3.right;

        //// 4. Возвращаем физику в дефолтное безопасное состояние пула
        //if (_rigidbody != null)
        //{
        //    // СБРОС СКОРОСТЕЙ: Делаем строго если тело было динамическим!
        //    // Если оно уже кинематическое (пушка/башня), Unity пропустит этот блок и не выдаст варнинг.
        //    if (!_rigidbody.isKinematic)
        //    {
        //        _rigidbody.linearVelocity = Vector3.zero;
        //        _rigidbody.angularVelocity = Vector3.zero;
        //    }

        //    _rigidbody.isKinematic = true;      // В пуле физика должна спать
        //    _rigidbody.detectCollisions = false; // Отключаем коллизии спящего объекта
        //}

        // 5. Очищаем все внутренние смещения и ссылки
        _localOffsetPosition = Vector3.zero;
        _localOffsetRotation = Quaternion.identity;
        _parentTransform = null;
        _parentRigidbody = null;
        _parentPoolEntity = null;
    }


    public void SetPoolEntity(PoolEntity entity)
    {
        _myPoolEntity = entity;
    }
}



//using UnityEngine;

//[DisallowMultipleComponent]
//public class VirtualChild : MonoBehaviour
//{
//    private Transform _transform;
//    private Rigidbody _rigidbody;
//    private Collider _collider;

//    private Transform _parentTransform;
//    private Rigidbody _parentRigidbody;

//    private Vector3 _localOffsetPosition;
//    private Quaternion _localOffsetRotation;

//    private bool _isAttached;
//    public bool IsAttached => _isAttached;

//    public void Initialize()
//    {
//        _transform = transform;
//        _rigidbody = GetComponent<Rigidbody>();
//        _collider = GetComponent<Collider>();
//    }

//    public void Attach(Transform parentTransform, Rigidbody parentRb)
//    {
//        _parentTransform = parentTransform;
//        _parentRigidbody = parentRb;

//        // Вычисляем локальную матрицу смещения относительно безфизического родителя
//        _localOffsetPosition = parentTransform.InverseTransformPoint(_transform.position);
//        _localOffsetRotation = Quaternion.Inverse(parentTransform.rotation) * _transform.rotation;

//        if (_rigidbody != null)
//        {
//            _rigidbody.isKinematic = true;

//            // ХАК ДЛЯ РОДИТЕЛЕЙ БЕЗ RIGIDBODY: 
//            // Если у родителя нет физики, мы обязаны сказать PhysX полностью игнорировать 
//            // этот коллайдер во время Update-движения, иначе трансформации заблокируются.
//            if (_parentRigidbody == null)
//            {
//                _rigidbody.detectCollisions = false;
//            }
//        }

//        _isAttached = true;
//    }

//    /// <summary>
//    /// Этот метод теперь выполняет всю работу, так как наш родитель движется в Update!
//    /// </summary>
//    public void UpdateTick()
//    {
//        if (!_isAttached) return;

//        // Строго если у родителя НЕТ Rigidbody
//        if (_parentRigidbody == null)
//        {
//            // Прямая высокоскоростная интерполяция матриц на CPU
//            _transform.position = _parentTransform.TransformPoint(_localOffsetPosition);
//            _transform.rotation = _parentTransform.rotation * _localOffsetRotation;
//        }
//    }

//    public void FixedUpdateTick()
//    {
//        if (!_isAttached) return;

//        // Этот блок сработает только если появится физический родитель
//        if (_parentRigidbody != null && _rigidbody != null)
//        {
//            Vector3 targetPosition = _parentTransform.TransformPoint(_localOffsetPosition);
//            Quaternion targetRotation = _parentTransform.rotation * _localOffsetRotation;

//            _rigidbody.MovePosition(targetPosition);
//            _rigidbody.MoveRotation(targetRotation);
//        }
//    }

//    public void Detach(Vector3 explosionForce, ForceMode mode)
//    {
//        if (!_isAttached) return;

//        _isAttached = false;

//        if (_rigidbody != null)
//        {
//            // Возвращаем физику в стандартный рабочий режим PhysX
//            _rigidbody.isKinematic = false;
//            _rigidbody.detectCollisions = true;

//            if (_parentRigidbody != null)
//            {
//                _rigidbody.linearVelocity = _parentRigidbody.GetPointVelocity(_transform.position);
//            }

//            if (explosionForce != Vector3.zero)
//            {
//                _rigidbody.AddForce(explosionForce, mode);
//            }
//        }

//        _parentTransform = null;
//        _parentRigidbody = null;
//    }
//}
