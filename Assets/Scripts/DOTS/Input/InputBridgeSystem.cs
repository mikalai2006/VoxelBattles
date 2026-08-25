using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class InputBridgeSystem : MonoBehaviour
{
    private InputSystem_Actions _gameInput;
    private EntityQuery _inputQuery;
    private EntityManager _entityManager;
    private World _targetWorld;

    private Vector2 _moveInput;
    private bool _switchTriggered;

    private void Awake() => _gameInput = new InputSystem_Actions();

    private void Start()
    {
        // Ищем мир, который отфильтрован как Клиентский (Client Simulation)
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                _targetWorld = world;
                break;
            }
        }

        // Если клиентский мир найден и активен — инициализируем синглтон ввода внутри него
        if (_targetWorld != null && _targetWorld.IsCreated)
        {
            _entityManager = _targetWorld.EntityManager;
            _inputQuery = _entityManager.CreateEntityQuery(typeof(InputStateSingleton));

            if (_inputQuery.IsEmpty)
            {
                var entity = _entityManager.CreateEntity(typeof(InputStateSingleton));
                _entityManager.SetName(entity, "InputState_Singleton");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("[InputBridge] Критическая ошибка: Не удалось обнаружить активный Клиентский ECS-мир!");
        }
    }

    private void OnEnable()
    {
        _gameInput.Player.Enable();
        _gameInput.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _gameInput.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        // Фиксируем нажатие кнопки смены сущности
        _gameInput.Player.Switch.performed += ctx => _switchTriggered = true;
    }

    private void OnDisable()
    {
        _gameInput.Player.Disable();
    }

    private void Update()
    {
        if (_targetWorld == null || !_targetWorld.IsCreated) return;

        // Каждую итерацию Update пушим данные в ECS синглтон
        if (_inputQuery.TryGetSingleton<InputStateSingleton>(out var inputState))
        {
            inputState.MoveInput = _moveInput;
            inputState.SwitchTargetTriggered = _switchTriggered;
            _inputQuery.SetSingleton(inputState);

            // Сбрасываем триггер после отправки в ECS, чтобы он сработал как "OnKeyDown"
            _switchTriggered = false;
        }
    }
}
