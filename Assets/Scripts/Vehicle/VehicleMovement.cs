using UnityEngine;

/// <summary>
/// Отвечает за физическое смещение и плавный разворот каркаса машины в пространстве Unity.
/// Вращение колес делегировано оптимизированному компоненту VehicleWheelRotator.
/// </summary>
public class VehicleMovement : MonoBehaviour, IMovable
{
    LevelManager LevelManager;
    private float currentSpeed;   // Текущая базовая скорость машины
    private float rotationSpeed;  // Скорость плавного разворота корпуса

    private Rigidbody _rigidbody;
    private bool _hasRigidbody;

    // Кэш компонента вращения колес, настроенного сборщиком
    private VehicleWheelRotator wheelRotator;

    // Публичное свойство для чтения скорости внешними скриптами
    public float Speed => currentSpeed;

    [Header("Ссылки на независимые части")]
    [SerializeField] private Transform chassisMesh; // Ссылка на визуальный корпус (Hull)


    private void Awake()
    {
        // Находим ротатор на этом же объекте при старте один раз (избавляет от GetComponent в Update)
        wheelRotator = GetComponent<VehicleWheelRotator>();
        _rigidbody = GetComponent<Rigidbody>();
        _hasRigidbody = _rigidbody != null;
    }

    /// <summary>
    /// Вызывается из сборщика VehicleAssembler при успешной установке подвески для передачи ТТХ.
    /// </summary>
    public void Setup(float speed, float rotSpeed, LevelManager levelManager)
    {
        LevelManager = levelManager;
        currentSpeed = speed;
        rotationSpeed = rotSpeed;
    }

    /// <summary>
    /// Универсальное перемещение. Автоматически выбирает физический MovePosition 
    /// или математический Translate в зависимости от наличия Rigidbody.
    /// </summary>
    public void Move(Vector3 direction)
    {
        // Интенсивность (сила) ввода игрока WASD / Стика геймпада
        float speedMultiplier = direction.magnitude;

        // Автоматически выбираем правильный шаг времени:
        // Если метод вызван из FixedUpdate — берем fixedDeltaTime, если из Update — deltaTime.
        float dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;

        if (speedMultiplier > 0.01f)
        {
            // Направление движения в метрах для текущего кадра
            Vector3 movement = direction.normalized * currentSpeed * speedMultiplier * dt;

            // ВАРИАНТ А: Если у объекта ЕСТЬ Rigidbody — двигаем строго через PhysX
            if (_hasRigidbody)
            {
                Vector3 targetPosition = _rigidbody.position + movement;

                // Физическое скольжение: машина бьется о стены и стоит на Plane, не проваливаясь
                _rigidbody.MovePosition(targetPosition);
            }
            // ВАРИАНТ Б: Если Rigidbody НЕТ — откатываемся на базовый математический Translate
            else
            {
                transform.Translate(movement, Space.World);
            }
        }

        // Если вектор направления движения не нулевой (игрок жмет на кнопки)
        if (speedMultiplier > 0.1f)
        {
            // Рассчитываем целевой мировой разворот носа
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Поворачиваем носом в сторону езды ТОЛЬКО визуальное шасси, а не весь корень!
            if (chassisMesh != null)
            {
                chassisMesh.rotation = Quaternion.RotateTowards(
                    chassisMesh.rotation,
                    targetRotation,
                    rotationSpeed * dt
                );
            }

            // УЛЬТИМАТИВНАЯ ОПТИМИЗАЦИЯ ВРАЩЕНИЯ КОЛЕС
            if (wheelRotator != null)
            {
                wheelRotator.RotateWheels(currentSpeed * speedMultiplier);
            }
        }
    }

    ///// <summary>
    ///// Передвигает объект, разворачивает его нос по вектору движения и командует колесам крутиться.
    ///// </summary>
    //public void Move(Vector3 direction)
    //{
    //    // Интенсивность (сила) ввода игрока WASD / Стика геймпада
    //    float speedMultiplier = direction.magnitude;

    //    // Физически передвигаем машину по координатной сетке игрового мира Unity.
    //    // Двигаем корень (transform), но сам корень при этом НЕ вращаем.
    //    transform.Translate(direction.normalized * currentSpeed * Time.deltaTime, Space.World);

    //    // Если вектор направления движения не нулевой (игрок жмет на кнопки)
    //    if (speedMultiplier > 0.1f)
    //    {
    //        // Рассчитываем целевой разворот
    //        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

    //        // ВАЖНО: Поворачиваем носом в сторону езды ТОЛЬКО визуальное шасси, а не весь корень!
    //        if (chassisMesh != null)
    //        {
    //            chassisMesh.rotation = Quaternion.RotateTowards(
    //                chassisMesh.rotation, 
    //                targetRotation, 
    //                rotationSpeed * Time.deltaTime
    //            );
    //        }

    //        // --- УЛЬТИМАТИВНАЯ ОПТИМИЗАЦИЯ ВРАЩЕНИЯ КОЛЕС ---
    //        if (wheelRotator != null)
    //        {
    //            wheelRotator.RotateWheels(currentSpeed * speedMultiplier);
    //        }
    //    }
    //}

    //public void Move(Vector3 direction)
    //{
    //    // Интенсивность (сила) ввода игрока WASD / Стика геймпада
    //    float speedMultiplier = direction.magnitude;

    //    // Физически передвигаем машину по координатной сетке игрового мира Unity
    //    transform.Translate(direction.normalized * currentSpeed * Time.deltaTime, Space.World);

    //    // Если вектор направления движения не нулевой (игрок жмет на кнопки)
    //    if (speedMultiplier > 0.1f)
    //    {
    //        // Рассчитываем целевой разворот и плавно поворачиваем машину носом в сторону езды
    //        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
    //        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

    //        // --- УЛЬТИМАТИВНАЯ ОПТИМИЗАЦИЯ ВРАЩЕНИЯ КОЛЕС ---
    //        // Тяжелые циклы поиска объектов transform.GetChild полностью удалены.
    //        // Вместо них мы делаем один прямой и мгновенный вызов к закэшированному массиву ротатора.
    //        if (wheelRotator != null)
    //        {
    //            // Передаем в ротатор текущую физическую скорость с учетом силы наклона стика/ввода
    //            wheelRotator.RotateWheels(currentSpeed * speedMultiplier);
    //        }
    //    }
    //}

}
