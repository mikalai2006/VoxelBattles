#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class WFCCameraController : MonoBehaviour
{
    // private GameManager _gameManager => GameManager.Instance;
    [SerializeField] private InputActionReference moveActionToUse;
    // [SerializeField] public InputActionAsset playerInputActions;
    // [SerializeField] private InputAction doubleTapAction;
    // [SerializeField] Camera _camera;
    // [SerializeField] Camera _cameraFPS;
    [SerializeField] Vector3 moveDirection;
    public CinemachineCamera cinemachineCamera;
    public CinemachineOrbitalFollow cinemachineOrbitalFollow;
    [SerializeField] Vector3 rotateDirection;
    [SerializeField] public VariableJoystick JoystickMove;
    [SerializeField] public VariableJoystick JoystickRotation;
    [SerializeField] float speedMoveTarget;
    [SerializeField] Vector2 speedRotateCamera;

    // Camera Camera => _camera.gameObject.activeSelf == true ? _camera : _cameraFPS;

    // private float speedRotateCameraDiff;
    
    // [Header("Настройки зажатой кнопки стрельбы")]
    // [SerializeField] private bool holdShot;
    
    // [Header("Настройки двойного клика")]
    // private float lastClickTime = 0f;
    // private bool firstClickDetected = false;

    void Awake()
    {
        // doubleTapAction = playerInputActions.FindActionMap("Player").FindAction("DoubleTap");
        cinemachineCamera.Target.TrackingTarget = transform;
    }

    void OnEnable()
    {
        moveActionToUse.action.Enable();

        // doubleTapAction.Enable();
    }

    void OnDisable()
    {
        moveActionToUse.action.Disable();

        // doubleTapAction.Disable();
    }

    // void Start()
    // {
    //     _camera = GameObject.FindGameObjectWithTag("CameraGame").GetComponent<Camera>();
    // }

    // void OnDestroy()
    // {
    // }

    void Update()
    {
        // захватываем значения джойстика перемещения.
        // moveDirection = Vector2.zero;

        // if (_gameManager.Settings.DebugSettings.mode == AppMode.Mobile)
        // {
        //     moveDirection = _machine.LevelManager.JoystickMove.Direction;
        // }
        // else
        // {
        //     moveDirection = moveActionToUse.action.ReadValue<Vector2>();
        // }
        moveDirection = moveActionToUse.action.ReadValue<Vector2>();
        if (moveDirection == Vector3.zero)
        {
            moveDirection = JoystickMove.Direction;
        }

        var forw = cinemachineCamera.transform.forward;
        var righ = cinemachineCamera.transform.right;
        
        forw.Normalize();
        righ.Normalize();

        Vector3 moveDir = (forw * moveDirection.y + righ * moveDirection.x).normalized;

        // transform.Translate(new Vector3(moveDirection.x,moveDirection.y,moveDirection.x) * speedMoveTarget * Time.deltaTime);
        transform.Translate(moveDir * speedMoveTarget * Time.deltaTime);

        // android.
        rotateDirection = JoystickRotation.Direction;
        rotateDirection.z = rotateDirection.y * speedRotateCamera.y * Time.deltaTime;
        rotateDirection.y = 0;
        rotateDirection.x = rotateDirection.x * speedRotateCamera.x * Time.deltaTime;


        // захватываем позицию мыши или джойстика управления башней.
        // if (!_machine.MachineLevelData.isBot) // && !_gameManager.Settings.autoTakeEnemy
        // {
        //     rotateDirection = Vector3.zero;

        //     if (_gameManager.Settings.DebugSettings.mode == AppMode.Mobile)
        //     {

        //         // добавляем время удержания джойстика.
        //         if (_machine.LevelManager.JoystickTower.TimeTouch > 0) {
        //             _machine.LevelManager.JoystickTower.AddTimeTouch(Time.deltaTime);
        //         }

        //         // android.
        //         rotateDirection = _machine.LevelManager.JoystickTower.Direction;
        //         rotateDirection.z = rotateDirection.y * _gameManager.Settings.playerOptions.speedRotateCamera.y * _machine.LevelManager.JoystickTower.TimeTouch;
        //         rotateDirection.y = 0;
        //         rotateDirection.x = rotateDirection.x * _gameManager.Settings.playerOptions.speedRotateCamera.x * _machine.LevelManager.JoystickTower.TimeTouch;

                
        //                 // android.
        //                 if (_machine.LevelManager.cinemachineOrbitalFollow != null)
        //                 {
        //                     if (_machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value == -180)
        //                     {
        //                         _machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value = 180;
        //                     } else if (_machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value == 180)
        //                     {
        //                         _machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value = -180;
        //                     }
        //                     _machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value = Mathf.Clamp(
        //                         _machine.LevelManager.cinemachineOrbitalFollow.HorizontalAxis.Value + rotateDirection.x,
        //                         -180,
        //                         180
        //                     );
        //                     _machine.LevelManager.cinemachineOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
        //                         _machine.LevelManager.cinemachineOrbitalFollow.VerticalAxis.Value - rotateDirection.z,
        //                         -10,
        //                         25
        //                     );
        //                 }
        //     }
        // }


        // // Если зажата кнопка - стреляем
        // if (holdShot)
        // {
        //     _machine.OnShot();
        // }
    }

    void FixedUpdate()
    {
        if (cinemachineOrbitalFollow != null)
        {
            if (cinemachineOrbitalFollow.HorizontalAxis.Value == -180)
            {
                cinemachineOrbitalFollow.HorizontalAxis.Value = 180;
            } else if (cinemachineOrbitalFollow.HorizontalAxis.Value == 180)
            {
                cinemachineOrbitalFollow.HorizontalAxis.Value = -180;
            }
            cinemachineOrbitalFollow.HorizontalAxis.Value = Mathf.Clamp(
                cinemachineOrbitalFollow.HorizontalAxis.Value + rotateDirection.x,
                -180,
                180
            );
            cinemachineOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                cinemachineOrbitalFollow.VerticalAxis.Value - rotateDirection.z,
                -90,
                90
            );
        }
        // if (_machine.Badge != null)
        // {
        //     _machine.Badge.OnSetNameText(moveDirection.ToString());
        // }

        // if (moveDirection != Vector3.zero)
        // {
        //     // if (moveDirection.x > 0.5f || moveDirection.x < -0.5f)
        //     // {
        //     //     _machine.Rotate(moveDirection);
        //     // }
        //     // else
        //     // {
        //     //     _machine.Rotate(Vector3.zero);
        //     // }

        //     _machine.Move(moveDirection);
        // }
        // else
        // {
        //     // if (_machine.IsMove)
        //     // {
        //     // }
        //     _machine.Stop();
        // }

        // // обработка вращения башни.
        //     if (_gameManager && _gameManager.Settings.DebugSettings.mode == AppMode.Mobile)
        //     {
        //         // android.
        //         if (cinemachineOrbitalFollow != null)
        //         {
        //             if (cinemachineOrbitalFollow.HorizontalAxis.Value == -180)
        //             {
        //                 cinemachineOrbitalFollow.HorizontalAxis.Value = 180;
        //             } else if (cinemachineOrbitalFollow.HorizontalAxis.Value == 180)
        //             {
        //                 cinemachineOrbitalFollow.HorizontalAxis.Value = -180;
        //             }
        //             cinemachineOrbitalFollow.HorizontalAxis.Value = Mathf.Clamp(
        //                 cinemachineOrbitalFollow.HorizontalAxis.Value + rotateDirection.x,
        //                 -180,
        //                 180
        //             );
        //             cinemachineOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
        //                 cinemachineOrbitalFollow.VerticalAxis.Value - rotateDirection.z,
        //                 -10,
        //                 25
        //             );
        //         }

        //         // // Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera, _machine.levelManager.UiTopSide.CrossObjectTransform.position);
        //         // // Vector3 worldPosition = Camera.ScreenToWorldPoint(_machine.levelManager.UiTopSide.CrossObjectTransform.position);
        //         // // Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera, _machine.levelManager.UiTopSide.CrossObjectTransform.position);

        //         // // Vector3 worldPoint;
        //         // // RectTransformUtility.ScreenPointToWorldPointInRectangle(_machine.levelManager.UiTopSide.CrossObjectTransform, screenPoint, Camera, out worldPoint);

        //         // Ray ray = Camera.ScreenPointToRay(_machine.levelManager.UiTopSide.CrossObjectTransform.position);
        //         // if (Physics.Raycast(ray, out RaycastHit hit))
        //         // {
        //         //     Vector3 targetPosition = hit.point;
        //         //     directionRotation = targetPosition - transform.position;
        //         //     //direction.y = 0;
        //         //     Debug.DrawRay(Camera.transform.position, directionRotation);
        //         //     // Debug.Log($"Camera name: {Camera.name}, POINT={hit.point}");

        //         // }

        //         // // Vector3 directionToTarget = worldPoint - transform.position;
        //         // // Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        //         // // Debug.Log($"targetRotation={targetRotation.eulerAngles}, worldPoint={worldPoint}");
        //     }
        //     // else
        //     // {
        //     //     // WEBGL.
        //     //     Vector3 positionMouse = Mouse.current.position.ReadValue();

        //     //     // positionMouse.z = Camera.transform.position.z;

        //     //     Vector3 positionMouseWorld = Camera.ScreenToWorldPoint(positionMouse);
        //     //     // Debug.Log($"positionMouseWorld = {positionMouseWorld}, positionMouse={positionMouse}");

        //     //     _machine.levelManager.UiTopSide.OnSetCrossPosition(positionMouse);

        //     //     // Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera, _machine.levelManager.UiTopSide.CrossObjectTransform.position);

        //     //     // Vector3 worldPoint;
        //     //     // RectTransformUtility.ScreenPointToWorldPointInRectangle(_machine.levelManager.UiTopSide.CrossObjectTransform, screenPoint, Camera, out worldPoint);

        //     //     Ray ray = Camera.ScreenPointToRay(positionMouse);
        //     //     // positionMouse.z = transform.position.z - _camera.transform.position.z; //_camera.farClipPlane * .5f;;
        //     //     // Vector3 worldPoint = _camera.ScreenToWorldPoint(positionMouse);
        //     //     // // Calculate the direction vector from the object to the mouse
        //     //     // direction = worldPoint - transform.position;
        //     //     // Debug.Log($"worldPoint= {worldPoint}, positionMouse= {positionMouse}, direction={direction}");
        //     //     if (Physics.Raycast(ray, out RaycastHit hit))
        //     //     {
        //     //         Vector3 targetPosition = hit.point;
        //     //         direction = targetPosition - transform.position;
        //     //         //direction.y = 0;
        //     //         Debug.DrawRay(Camera.transform.position, direction, Color.magenta);
        //     //         // Debug.Log($"Camera name: {Camera.name}, hit name={hit.collider.name}, POINT={hit.point}");
        //     //     }
        //     // }

        //     // if (direction != Vector3.zero)
        //     // {
        //     //     Debug.Log($"direction={direction}");
        //     //     // // Calculate the angle in degrees
        //     //     // float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        //     //     Quaternion lookRotation = Quaternion.LookRotation(direction);


        //     //     // Debug.Log($"angle = {angle}, direction= {direction}");
        //     //     for (int i = 0; i < _machine.Towers.Count; i++)
        //     //     {
        //     //         // _machine.Towers[i].OnSetAngleTower(angle);
        //     //         _machine.Towers[i].OnSetAngleTower(lookRotation.eulerAngles.y);
        //     //     }
        //     // }
        //     // if (rotateDirection != Vector3.zero)
        //     // {
        //     //     // Debug.Log($"direction={direction}");
        //     //     // // Calculate the angle in degrees
        //     //     // float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        //     //     // Quaternion lookRotation = Quaternion.LookRotation(direction);


        //     //     // Debug.Log($"angle = {angle}, direction= {direction}");
        //     //     for (int i = 0; i < _machine.Towers.Count; i++)
        //     //     {
        //     //         _machine.Towers[i].OnSetAngleTower(rotateDirection);
        //     //     }
        //     // }
    }
    

    // private void OnPointerDownRightJoystick(PointerEventData eventData)
    // {
    //     if (firstClickDetected && Time.time - lastClickTime < _gameManager.Settings.playerOptions.doubleClickThreshold)
    //     {
    //         // Обнаружен двойной щелчок.

    //         // запускаем функции.
    //         _machine.OnShot();

    //         firstClickDetected = false; // Сброс для следующего потенциального двойного щелчка
    //     }
    //     else
    //     {
    //         // Обнаружен первый щелчок, запущен таймер
    //         firstClickDetected = true;
    //         lastClickTime = Time.time;
    //     }
        
    //     // Если был обнаружен первый щелчок, но порог двойного щелчка превышен
    //     if (firstClickDetected && Time.time - lastClickTime >= _gameManager.Settings.playerOptions.doubleClickThreshold)
    //     {
    //         // Обнаружен одиночный щелчок (если необходимо дифференцировать)
            
    //         firstClickDetected = false; // Сброс
    //     }
    // }



    // public void EnableShot(InputAction.CallbackContext context)
    // {
    //     holdShot = true;
    // }

    // public void DisableShot(InputAction.CallbackContext context)
    // {
    //     holdShot = false;
    // }

    // private void OnDoubleTapPerformed(InputAction.CallbackContext context)
    // {
    //     Debug.Log("Double Tap Detected!");
    //     // Add your double-tap specific logic here
    //     _machine.OnShot();
    // }
}
#endif