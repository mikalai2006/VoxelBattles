using UnityEngine;
using UnityEngine.InputSystem; // Обязательный импорт для новой Input System

public class PlayerVehicleInput : MonoBehaviour
{
    private VehicleMovement movement;
    private VehicleWeapon weapon;

    // Ссылка на сгенерированный C# класс от Input System
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    // Кэшируем ссылку на камеру, чтобы метод Camera.main не искал её каждый кадр (это тяжело для процессора)
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null) {
            mainCamera = GameObject.FindGameObjectWithTag("CameraGame")?.GetComponent<Camera>();
        }
        // 1. Инициализируем систему ввода
        inputActions = new InputSystem_Actions();

        // 2. Подписываемся на событие стрельбы (вызывается в момент нажатия)
        inputActions.Player.Attack.performed += OnFirePerformed;
    }

    private void Start()
    {
        // Получаем ссылки на компоненты машины
        movement = GetComponent<VehicleMovement>();
        weapon = GetComponent<VehicleWeapon>();
    }

    private void OnEnable()
    {
        // Включаем карту действий при активации объекта
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        // Отписываемся от событий и выключаем ввод для безопасности
        inputActions.Player.Attack.performed -= OnFirePerformed;
        inputActions.Player.Disable();
    }

    void Update()
    {
        //// 3. Считываем непрерывное значение движения (WASD / Стик геймпада)
        //moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        //// 4. ДВИЖЕНИЕ: если ходовая активна, переводим Vector2 (X, Y) в трехмерное пространство (X, 0, Z)
        //if (movement != null && movement.enabled)
        //{
        //    Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        //    movement.Move(moveDirection);
        //}

        // 1. ДВИЖЕНИЕ ШАССИ (WASD / Стик)
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (movement != null && movement.enabled)
        {
            // Переводим Vector2 ввода в направление движения корпуса
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            movement.Move(moveDirection);
        }

        // 2. ПРИЦЕЛИВАНИЕ БАШНИ (Независимо от движения корпуса)
        // Используем универсальный Pointer.current (работает для мыши на ПК и для тача на мобильных)
        if (weapon != null && weapon.enabled && Pointer.current != null)
        {
            Vector2 pointerScreenPosition = Pointer.current.position.ReadValue();

            // Стреляем математическим лучом из камеры в точку на экране
            Ray ray = mainCamera.ScreenPointToRay(pointerScreenPosition);

            // Пускаем луч. В идеале вместо Plane.Raycast использовать Physics.Raycast, 
            // если у вас на сцене есть земля с коллайдером. 
            // Используем математическую плоскость на высоте машины, чтобы не нагружать PhysX.
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float enterDistance))
            {
                Vector3 targetWorldPosition = ray.GetPoint(enterDistance);

                // Передаем точку прицеливания в компонент оружия/башни
                weapon.AimAt(targetWorldPosition);
            }
        }
    }

    // 5. СТРЕЛЬБА: метод срабатывает автоматически по событию от новой Input System
    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        // Стреляем, только если оружейный компонент активен (пушка установлена)
        if (weapon != null && weapon.enabled)
        {
            weapon.Shoot();
        }
    }
}
