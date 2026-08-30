
using System.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class InputBridgeSystem : MonoBehaviour
{
    private InputSystem_Actions _gameInput;
    private EntityQuery _inputQuery;
    private EntityManager _entityManager;
    private World _targetWorld;
    [SerializeField] private string _targetWorldName;

    private Vector2 _moveInput;
    private bool _switchTriggered;
    [SerializeField] private bool _isInitialized = false;

    private void Awake() => _gameInput = new InputSystem_Actions();

    private void Start()
    {
        //if (_isInitialized) return;
#if !UNITY_SERVER
        StartCoroutine(WaitForInitialize());
#endif
    }

    private IEnumerator WaitForInitialize()
    {
        if (_isInitialized)
        {
            UnityEngine.Debug.Log("[InputBridge] InputStateSingleton уже инициализирован, корутина завершена!");
            yield break;
        }

        // 1. Крутим цикл ДО ТЕХ ПОР, пока не найдем созданный клиентский мир
        while (_targetWorld == null || !_targetWorld.IsCreated)
        {
            foreach (var world in World.All)
            {
                if (world.IsClient() && !world.Name.Contains("ThinClient"))
                {
                    _targetWorld = world;
                    _targetWorldName = world.Name;
                    UnityEngine.Debug.Log($"[InputBridge] Connect to {_targetWorldName}...");
                    break;
                }
            }

            // Если на этом кадре мир не найден — ждем один кадр и проверяем снова
            if (_targetWorld == null || !_targetWorld.IsCreated)
            {
                UnityEngine.Debug.LogWarning("[InputBridge] Wait client...");
                yield return null; // Пропускаем кадр и возвращаемся в начало цикла while
            }
        }

        // 2. Сюда код доберется ТОЛЬКО тогда, когда _targetWorld гарантированно существует и активен
        _entityManager = _targetWorld.EntityManager;
        _inputQuery = _entityManager.CreateEntityQuery(typeof(InputStateSingleton));

        if (_inputQuery.IsEmpty)
        {
            var entity = _entityManager.CreateEntity(typeof(InputStateSingleton));
            // Обратите внимание: изменять имена сущностей (SetName) в билде нельзя, 
            // так как в релизе вырезается дебаг-функционал имен. Строка ниже закомментирована правильно.
            // _entityManager.SetName(entity, "InputState_Singleton");
        }

        //UnityEngine.Debug.Log($"[InputBridge] InputStateSingleton успешно инициализирован в мире: {_targetWorld.Name}!");
        _isInitialized = true;
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