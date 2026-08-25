using Unity.Entities;
using Unity.NetCode; // Обязательно для поиска клиентского мира
using UnityEngine;

public class AAACameraBridge : MonoBehaviour
{
    [Header("AAA Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -12f);
    [SerializeField] private float smoothSpeed = 8f;

    private EntityManager _entityManager;
    private EntityQuery _cameraQuery;
    private World _clientWorld;

    private Vector3 _lastOffset;
    private float _lastSmoothSpeed;
    private bool _isInitialized = false;

    private void Start()
    {
        _lastOffset = offset;
        _lastSmoothSpeed = smoothSpeed;

        // Пытаемся инициализироваться сразу, если мир уже готов
        TryInitialize();
    }

    private bool TryInitialize()
    {
        if (_isInitialized) return true;

        // Ищем именно КЛИЕНТСКИЙ мир Netcode. Если игра еще загружается, его может не быть пару кадров
        _clientWorld = GetClientWorld();
        if (_clientWorld == null || !_clientWorld.IsCreated) return false;

        _entityManager = _clientWorld.EntityManager;
        _cameraQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<AAA_CameraSettingsSingleton>());

        // Если в клиентском мире синглтона еще нет — создаем его
        if (_cameraQuery.IsEmpty)
        {
            var entity = _entityManager.CreateEntity(typeof(AAA_CameraSettingsSingleton));
            _entityManager.SetComponentData(entity, new AAA_CameraSettingsSingleton
            {
                Offset = offset,
                SmoothSpeed = smoothSpeed,
                TargetPosition = transform.position
            });
        }

        _isInitialized = true;
        return true;
    }

    private void LateUpdate()
    {
        // Если мир еще не был найден (например, в самом первом кадре), пробуем привязаться снова
        if (!_isInitialized && !TryInitialize()) return;
        if (_clientWorld == null || !_clientWorld.IsCreated) return;

        if (_cameraQuery.HasSingleton<AAA_CameraSettingsSingleton>())
        {
            // Синхронизируем потоки: MonoBehaviour ждет завершения систем ECS, пишущих в этот синглтон
            _cameraQuery.CompleteDependency();

            var cameraSettings = _cameraQuery.GetSingleton<AAA_CameraSettingsSingleton>();

            if (float.IsNaN(cameraSettings.TargetPosition.x) ||
                float.IsNaN(cameraSettings.TargetPosition.y) ||
                float.IsNaN(cameraSettings.TargetPosition.z))
            {
                return;
            }

#if UNITY_EDITOR
            // Позволяет менять offset и smoothSpeed в инспекторе прямо во время игры
            if (Vector3.Distance(offset, _lastOffset) > 0.001f || !Mathf.Approximately(smoothSpeed, _lastSmoothSpeed))
            {
                cameraSettings.Offset = offset;
                cameraSettings.SmoothSpeed = smoothSpeed;
                _cameraQuery.SetSingleton(cameraSettings);

                _lastOffset = offset;
                _lastSmoothSpeed = smoothSpeed;
            }
#endif

            // Рассчитываем финальную точку для камеры: чистая позиция игрока + оффсет
            Vector3 desiredPosition = (Vector3)cameraSettings.TargetPosition + (Vector3)cameraSettings.Offset;


            // Плавное движение камеры за целевой позицией, которую посчитала наша ECS-система
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Вспомогательный метод для поиска активного Клиентского мира в Unity Netcode
    /// </summary>
    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                return world;
            }
        }
        return null;
    }
}


//using Unity.Entities;
//using UnityEngine;

//public class AAACameraBridge : MonoBehaviour
//{
//    [Header("AAA Camera Settings")]
//    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -12f);
//    [SerializeField] private float smoothSpeed = 8f;

//    private EntityManager _entityManager;
//    private EntityQuery _cameraQuery;
//    private World _targetWorld;

//    private Vector3 _lastOffset;
//    private float _lastSmoothSpeed;

//    private void Start()
//    {
//        _targetWorld = World.DefaultGameObjectInjectionWorld;
//        if (_targetWorld == null || !_targetWorld.IsCreated) return;

//        _entityManager = _targetWorld.EntityManager;
//        _cameraQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<AAA_CameraSettingsSingleton>());

//        if (_cameraQuery.IsEmpty)
//        {
//            var entity = _entityManager.CreateEntity(typeof(AAA_CameraSettingsSingleton));
//            _entityManager.SetComponentData(entity, new AAA_CameraSettingsSingleton
//            {
//                Offset = offset,
//                SmoothSpeed = smoothSpeed,
//                TargetPosition = transform.position
//            });
//        }

//        _lastOffset = offset;
//        _lastSmoothSpeed = smoothSpeed;
//    }

//    private void LateUpdate()
//    {
//        if (_targetWorld == null || !_targetWorld.IsCreated) return;

//        if (_cameraQuery.HasSingleton<AAA_CameraSettingsSingleton>())
//        {
//            // Самая важная строчка: завершаем Job камеры прямо перед тем, как MonoBehaviour прочитает синглтон
//            _cameraQuery.CompleteDependency();

//            var cameraSettings = _cameraQuery.GetSingleton<AAA_CameraSettingsSingleton>();

//            if (float.IsNaN(cameraSettings.TargetPosition.x) ||
//                float.IsNaN(cameraSettings.TargetPosition.y) ||
//                float.IsNaN(cameraSettings.TargetPosition.z))
//            {
//                return;
//            }


//#if UNITY_EDITOR
//            if (Vector3.Distance(offset, _lastOffset) > 0.001f || !Mathf.Approximately(smoothSpeed, _lastSmoothSpeed))
//            {
//                cameraSettings.Offset = offset;
//                cameraSettings.SmoothSpeed = smoothSpeed;
//                _cameraQuery.SetSingleton(cameraSettings);

//                _lastOffset = offset;
//                _lastSmoothSpeed = smoothSpeed;
//            }
//#endif

//            // Возвращаем ваш родной плавный Лерп на стороне MonoBehaviour
//            transform.position = Vector3.Lerp(
//                transform.position,
//                cameraSettings.TargetPosition,
//                smoothSpeed * Time.deltaTime
//            );
//        }
//    }
//}




////using Unity.Entities;
////using UnityEngine;

////public class AAACameraBridge : MonoBehaviour
////{
////    [Header("AAA Camera Settings")]
////    public Vector3 offset = new Vector3(0f, 10f, -12f);
////    public float smoothSpeed = 8f;

////    private EntityManager _entityManager;
////    private EntityQuery _cameraQuery;
////    private World _targetWorld;

////    private void Start()
////    {
////        _targetWorld = World.DefaultGameObjectInjectionWorld;
////        if (_targetWorld != null && _targetWorld.IsCreated)
////        {
////            _entityManager = _targetWorld.EntityManager;
////            _cameraQuery = _entityManager.CreateEntityQuery(typeof(AAA_CameraSettingsSingleton));

////            // Создаем синглтон камеры, если его нет
////            if (_cameraQuery.IsEmpty)
////            {
////                var entity = _entityManager.CreateEntity(typeof(AAA_CameraSettingsSingleton));
////                _entityManager.SetComponentData(entity, new AAA_CameraSettingsSingleton
////                {
////                    Offset = offset,
////                    SmoothSpeed = smoothSpeed,
////                    TargetPosition = transform.position
////                });
////#if UNITY_EDITOR
////                _entityManager.SetName(entity, "AAA_Camera_Singleton");
////#endif
////            }
////        }
////    }

////    // Используем LateUpdate для сглаживания движения камеры вслед за физикой/кадрами DOTS
////    private void LateUpdate()
////    {
////        if (_targetWorld == null || !_targetWorld.IsCreated) return;

////        // Считываем позицию, которую вычислил высокопроизводительный Burst
////        if (_cameraQuery.TryGetSingleton<AAA_CameraSettingsSingleton>(out var cameraSettings))
////        {
////            // Плавный AAA-интерполятор (Lerp) между текущей позицией и целевой
////            transform.position = Vector3.Lerp(
////                transform.position,
////                cameraSettings.TargetPosition,
////                cameraSettings.SmoothSpeed * Time.deltaTime
////            );

////            // Направляем камеру на точку под ней (или можно сделать жесткий LookAt на цель)
////            // Для вида сверху-сзади достаточно выставить правильный угол вручную в инспекторе,
////            // либо динамически раскоментить строку ниже:
////            // transform.LookAt(transform.position - (Vector3)cameraSettings.Offset);
////        }
////    }
////}
