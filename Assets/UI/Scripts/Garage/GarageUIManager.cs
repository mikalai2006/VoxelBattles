using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using UIToolkitLibrary;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class GarageUIManager : MonoBehaviour
{
    GameManager gameManager => GameManager.Instance;
    [SerializeField] private Transform Wrapper;
    [SerializeField] private Transform Wrapper3dModels;
    [SerializeField] private Camera Camera;
    [SerializeField] private CinemachineCamera CinemachineCamera;
    [SerializeField] private CinemachineInputAxisController CinemachineCameraInputController;
    // [SerializeField] private Camera CameraSecond;
    // [SerializeField] private CinemachineCamera CinemachineCameraSecond;
    // [SerializeField] private CinemachineInputAxisController CinemachineCameraSecondInputController;
    [SerializeField] private List<VehiclePresetAsset> machinesConfigs;
    [SerializeField] List<VehicleAssembler> machinesGameObjects;
    [SerializeField] private int activeIndexMachine = 0;
    [SerializeField] private InputActionReference mousePositionAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference actionCamera;
    [SerializeField] private ColorModifyItem activeColorModifyItem;
    //List<GameTowerShop> dataTowers;
    List<UIGarageTowerItemView> towersGameObjects;
    [SerializeField] private int activeIndexTower = -1;
    //[SerializeField] private float step = 0.4f;
    [SerializeField] private float offsetWrapperModels = 0.09f;
    // private List<DataDetail> cacheDataDetails;
    // CinemachineBrain brain;
    private int _previousWidth;
    private int _previousHeight;

    void Update()
    {
        if (_previousWidth != Screen.width || _previousHeight != Screen.height)
        {
            _previousWidth = Screen.width;
            _previousHeight = Screen.height;
            // Invoke the event when the size changes
            Wrapper3dModels.transform.position = Camera.ViewportToWorldPoint(new Vector3(0f, 0.55f, 1f));
        }
    }

    void OnEnable()
    {
        clickAction.action.Enable();
        mousePositionAction.action.Enable();

        clickAction.action.performed += OnClickPerformed;
        clickAction.action.started += OnStartTouch;
        clickAction.action.canceled += OnEndTouch;
    }

    void OnDisable()
    {
        clickAction.action.performed -= OnClickPerformed;
        clickAction.action.started -= OnStartTouch;
        clickAction.action.canceled -= OnEndTouch;

        clickAction.action.Disable();
        mousePositionAction.action.Disable();

        // OnClickButtonTowerClose();
    }

    void Start()
    {
        CinemachineOrbitalFollow cinemachineOrbitalFollow = CinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        if (cinemachineOrbitalFollow)
        {
            cinemachineOrbitalFollow.HorizontalAxis.Value = 90;
            cinemachineOrbitalFollow.VerticalAxis.Value = 15;
        }


        // cacheDataDetails = new();

        towersGameObjects = new();

        //dataTowers= new ();

        machinesGameObjects = new List<VehicleAssembler>();

        gameManager.SetActiveCamera(Camera);

        Init();

        // brain = Camera.GetComponent<CinemachineBrain>();

        GarageUIEvents.ClickButtonNextMachine += OnNextMachine;
        GarageUIEvents.ClickButtonPrevMachine += OnPrevMachine;
        GarageUIEvents.ClickButtonSellActiveMachine += OnSellActiveMachine;
        GarageUIEvents.ClickByColor += ChooseColorModify;
        GarageUIEvents.OpenColors += OnDisable;
        GarageUIEvents.CloseColors += OnEnable;
        GarageUIEvents.FillCancel += OnCancelColorModify;

        UIEvents.ClickButtonTower += OnClickButtonTower;
        UIEvents.ClickButtonPrevTower += OnClickButtonPrevTower;
        UIEvents.ClickButtonNextTower += OnClickButtonNextTower;
        UIEvents.ClickButtonTowerClose += OnClickButtonTowerClose;

        UIEvents.ClickButtonRepair += OnClickButtonRepair;
        UIEvents.ClickButtonBuyActiveTower += OnClickButtonBuyActiveTower;
    }

    void OnDestroy()
    {
        GarageUIEvents.ClickButtonNextMachine -= OnNextMachine;
        GarageUIEvents.ClickButtonPrevMachine -= OnPrevMachine;
        GarageUIEvents.ClickButtonSellActiveMachine -= OnSellActiveMachine;
        GarageUIEvents.ClickByColor -= ChooseColorModify;
        GarageUIEvents.OpenColors -= OnDisable;
        GarageUIEvents.CloseColors -= OnEnable;
        GarageUIEvents.FillCancel -= OnCancelColorModify;

        UIEvents.ClickButtonTower -= OnClickButtonTower;
        UIEvents.ClickButtonPrevTower -= OnClickButtonPrevTower;
        UIEvents.ClickButtonNextTower -= OnClickButtonNextTower;
        UIEvents.ClickButtonTowerClose -= OnClickButtonTowerClose;

        UIEvents.ClickButtonRepair -= OnClickButtonRepair;
        UIEvents.ClickButtonBuyActiveTower -= OnClickButtonBuyActiveTower;
    }

    private void OnClickButtonBuyActiveTower()
    {
        // gameManager.StateManager.BuyTower();
    }

    /// <summary>
    /// Удаляем поврежденные воксели у активной машины.
    /// </summary>
    private void OnClickButtonRepair()
    {
        gameManager.StateManager.RepairMachine(activeIndexMachine, 1);

        DrawMachine(gameManager.StateManager.statePlayer.machines[activeIndexMachine], activeIndexMachine);

        OnFocusMachineByIndex(activeIndexMachine);
    }


    private void OnClickButtonTower()
    {
        var activeMachine = machinesGameObjects[activeIndexMachine];

        // Сохраняем в кеш исходные данные машины.
        // cacheDataDetails.Clear();
        // cacheDataDetails.AddRange(activeMachine.MachineLevelData.data.dataDetails);

        // Создаем игровые объекты башен.
        CreateTowerItems();

        // Имитируем клик по первой башне.
        // OnClickButtonPrevTower();

        Wrapper3dModels.gameObject.SetActive(true);

        Wrapper3dModels.transform.localPosition = new Vector3(
            Wrapper3dModels.transform.localPosition.x,
            offsetWrapperModels,
            Wrapper3dModels.transform.localPosition.z);
    }

    private void OnClickButtonTowerClose()
    {
        int count = towersGameObjects.Count;
        for (int i = 0; i < count; i++)
        {
            if (towersGameObjects[i] != null)
            {
                Destroy(towersGameObjects[i].gameObject);
            }
        }

        towersGameObjects.Clear();

        ReDrawTower(-1);

        //dataTowers.Clear();

        // cacheDataDetails.Clear();

        Wrapper3dModels.gameObject.SetActive(false);

        activeIndexTower = -1;
    }

    private void OnClickButtonNextTower()
    {
        activeIndexTower += 1;
        FocusTower(activeIndexTower);
        // activeIndexTower = Mathf.Clamp(activeIndexTower, 0, dataTowers.Count - 1);


        // Wrapper3dModels.transform.localPosition = new Vector3(
        //     Wrapper3dModels.transform.localPosition.x,
        //     0.1f + activeIndexTower * step,
        //     Wrapper3dModels.transform.localPosition.z);

        // UIEvents.FocusTower?.Invoke(dataTowers[activeIndexTower]);

        // ReDrawTower(activeIndexTower);
    }

    private void OnClickButtonPrevTower()
    {
        activeIndexTower -= 1;

        FocusTower(activeIndexTower);
    }

    void FocusTower(int index)
    {
        //activeIndexTower = Mathf.Clamp(index, 0, dataTowers.Count - 1);

        //Wrapper3dModels.transform.localPosition = new Vector3(
        //    Wrapper3dModels.transform.localPosition.x,
        //    offsetWrapperModels + activeIndexTower * step,
        //    Wrapper3dModels.transform.localPosition.z);

        //UIEvents.FocusTower?.Invoke(dataTowers.ElementAt(activeIndexTower));

        //for (int i = 0; i < towersGameObjects.Count; i++)
        //{
        //    towersGameObjects.ElementAt(i).SetFocus(i == activeIndexTower);
        //}

        //// Перерисовываем башню.
        //ReDrawTower(activeIndexTower);
    }

    void ReDrawTower(int indexTower)
    {
        //List<DataDetail> newDataDetails = new();

        //if (indexTower > -1)
        //{
        //    // из данных в кэше берем все кроме башен и стволов.
        //    newDataDetails.AddRange(gameManager.StateManager.statePlayer.machines[activeIndexMachine].data.dataDetails
        //        .FindAll(x => x.type != VehicleDetailType.Tower && x.type != VehicleDetailType.Muzzle));

        //    // добавляем данные новой башни и ствола(ов).
        //    for (int i = 0; i < dataTowers[indexTower].items.Count; i++)
        //    {
        //        var item = dataTowers[indexTower].items.ElementAt(i);
        //        var uid = System.Guid.NewGuid().ToString();

        //        newDataDetails.Add(new DataDetail
        //        {
        //            nameConfig = item.Config.name,
        //            offset = item.offsetTower,
        //            ido = string.IsNullOrEmpty(item.ido) ? uid : item.ido,
        //            number = i,
        //            parentId = item.parentId,
        //            type = VehicleDetailType.Tower
        //        });

        //        for (int j = 0; j < item.muzzles.Count; j++)
        //        {
        //            var itemM = item.muzzles.ElementAt(j);

        //            newDataDetails.Add(new DataDetail
        //            {
        //                nameConfig = itemM.Config.name,
        //                offset = itemM.offsetMuzzle,
        //                number = j,
        //                type = VehicleDetailType.Muzzle,
        //                parentId = string.IsNullOrEmpty(item.ido) ? uid : item.ido
        //            });
        //        }
        //    }
        //} else
        //{
        //    // if (cacheDataDetails != null)
        //    // {
        //    //     List<GameTowerOption> allConfigs = new();

        //    //     var a = gameManager.Settings.machines.Select(x => x.towers).ToList();

        //    //     foreach (var item in a)
        //    //     {
        //    //         allConfigs.AddRange(item);
        //    //     }

        //    //     foreach (var item in cacheDataDetails)
        //    //     {
        //    //         var conf = allConfigs.FirstOrDefault(x => x.Config.name == item.nameConfig);

        //    //         if (conf != null)
        //    //         {
        //    //             // Debug.Log($"conf={conf.Config.name},cacheTower={cacheTower.nameConfig}");
        //    //             foreach (var item2 in cacheDataDetails)
        //    //             {
        //    //                 tower.ReDraw(conf, item2);
        //    //             }
        //    //         } else
        //    //         {
        //    //             Debug.LogWarning($"not found conf={conf},cacheTower={item.nameConfig}");
        //    //         }
        //    //     }
        //    // }
        //    newDataDetails.AddRange(gameManager.StateManager.statePlayer.machines[activeIndexMachine].data.dataDetails);
        //}

        //var activeMachine = machinesGameObjects[activeIndexMachine];

        //var activeStateMachinePlayer = gameManager.StateManager.statePlayer.machines[activeIndexMachine];

        //// CreateMachine(activeMachine.Config, activeIndexMachine, activeMachine.MachineLevelData);
        //StateMachinePlayer stateMachinePlayer = new StateMachinePlayer() {
        //    name = activeStateMachinePlayer.name,
        //    data = new StateMachinePlayerData
        //        {
        //            colorsModifies = activeStateMachinePlayer.data.colorsModifies,
        //            dataDetails = newDataDetails
        //        }
        //};

        //DrawMachine(stateMachinePlayer);

        //OnFocusMachineByIndex(activeIndexMachine);
    }


    private void ChooseColorModify(ColorModifyItem colorMI)
    {
        activeColorModifyItem = colorMI;
    }

    private void OnNextMachine()
    {
        OnFocusMachineByIndex(Mathf.Min(machinesGameObjects.Count - 1, activeIndexMachine + 1));
    }

    private void OnPrevMachine()
    {
        OnFocusMachineByIndex(Mathf.Max(0, activeIndexMachine - 1));
    }

    private void OnSellActiveMachine()
    {
        var configMachine = gameManager.StateManager.statePlayer.machines[activeIndexMachine];

        Destroy(machinesGameObjects[activeIndexMachine].gameObject);

        machinesGameObjects.RemoveAt(activeIndexMachine);

        gameManager.StateManager.SellMachine(configMachine);

        OnFocusMachineByIndex(0);
    }

    private void Init()
    {
        //// gameManager.StateManager.AddMachine("T3");

        //if (gameManager?.ResourceSystem != null)
        //{
        //    machinesConfigs.AddRange(gameManager.ResourceSystem.GetAllMachines());
        //}

        //DrawMachines();

        //var index = gameManager.StateManager.statePlayer != null ? gameManager.StateManager.statePlayer.indexActiveMachine : 0;

        //if (index > machinesGameObjects.Count)
        //{
        //    index = 0;
        //}

        //if (machinesGameObjects.Count > 0)
        //{
        //    OnFocusMachineByIndex(index);
        //}

        //Wrapper3dModels.gameObject.SetActive(false);
    }

    private void DrawMachines()
    {
        for (int i = 0; i < machinesGameObjects.Count; i++)
        {
            Destroy(machinesGameObjects[i].gameObject);

            machinesGameObjects.RemoveAt(i);
        }

        int index = 0;

        foreach (var item in gameManager.StateManager.statePlayer.machines)
        {
            machinesGameObjects.Add(default);

            DrawMachine(item, index);

            // GameMachine configMachine = machinesConfigs.Find(m => m.name == item.name);

            // if (configMachine != null)
            // {
            //     CreateMachine(configMachine, index, new MachineLevelData
            //     {
            //         id = item.name,
            //         data = item.data
            //     });
            // }

            index++;
        }

    }

    private void DrawMachine(StateMachinePlayer stateMachinePlayer, int index = -1)
    {
        int _index = index != -1 ? index : activeIndexMachine;

        // удаляем игровые объекты машины.
        if (machinesGameObjects[_index] != null)
        {
            Destroy(machinesGameObjects[_index].gameObject);
        }

        // создаем новые игровые объекты.
        VehiclePresetAsset configMachine = machinesConfigs.Find(m => m.name == stateMachinePlayer.name);

        CreateMachine(configMachine, _index, new MachineLevelData
        {
            id = stateMachinePlayer.name,
            name = stateMachinePlayer.name,
            data = stateMachinePlayer.data
        });
    }

    void OnFocusMachineByIndex(int index = 0)
    {
        activeIndexMachine = Mathf.Clamp(index, 0, gameManager.StateManager.statePlayer.machines.Count - 1);

        gameManager.StateManager.SetActiveMachine(activeIndexMachine);
        if (gameManager.StateManager.statePlayer.machines.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < machinesGameObjects.Count; i++)
        {
            machinesGameObjects[i].transform.localPosition = new Vector3(machinesGameObjects[i].transform.localPosition.x, 100, machinesGameObjects[i].transform.localPosition.z);
        }

        machinesGameObjects[activeIndexMachine].transform.localPosition = new Vector3(machinesGameObjects[activeIndexMachine].transform.localPosition.x, 0, machinesGameObjects[activeIndexMachine].transform.localPosition.z); ;


        //CinemachineCamera.Follow = machinesGameObjects[activeIndexMachine].objectTargetCamera.transform;
        //CinemachineCamera.LookAt = machinesGameObjects[activeIndexMachine].objectTargetCamera.transform;

        //// CinemachineOrbitalFollow cinemachineOrbitalFollow = CinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        //// if (cinemachineOrbitalFollow)
        //// {
        ////     cinemachineOrbitalFollow.HorizontalAxis.Value = 90;
        ////     cinemachineOrbitalFollow.VerticalAxis.Value = 15;
        //// }

        UIEvents.OnFocusMachineInGarage?.Invoke(gameManager.StateManager.statePlayer.machines[activeIndexMachine]);
    }

    void CreateMachine(VehiclePresetAsset configMachine, int index, MachineLevelData data)
    {
        //var gObject = Instantiate(
        //    configMachine.machinePrefab,
        //    new Vector3(0, 0, -index * 0.1f),
        //    Quaternion.identity,
        //    Wrapper
        //);

        //if (machinesGameObjects.ElementAt(index) != null)
        //{
        //    Destroy(machinesGameObjects.ElementAt(index).gameObject);
        //}

        //BaseMachine obj = gObject.GetComponent<BaseMachine>();
        //if (obj != null)
        //{
        //    // machinesGameObjects.Add(obj);
        //    machinesGameObjects[index] = null;
        //    machinesGameObjects[index] = obj;

        //    obj.GetComponent<PlayerController>().enabled = false;
        //    obj.GetComponent<PlayerInput>().enabled = false;
        //    obj.AreaSearch.gameObject.SetActive(false);
        //    // obj.GetComponentInChildren<NavMeshAgent>().enabled = false;
        //    // var lightComponent = obj.GetComponentInChildren<Light>();
        //    // if (lightComponent)
        //    // {
        //    //     lightComponent.enabled = true;
        //    // }
        //    // obj.Areol.SetActive(true);
        //    // obj.GetComponent<CameraFollow>().enabled = false;
        //    // obj.GetComponent<CameraFollowFPS>().enabled = false;
        //    obj.GetComponent<StateController>().enabled = false;
        //    obj.GetComponentInChildren<HealthBarController>().gameObject.SetActive(false);

        //    // CameraHandler.OnSetCharacter(obj);
        //    obj.Init(configMachine, data);

        //    foreach (var tower in obj.Towers)
        //    {
        //        tower.OnSetAngleTower(new Vector3(-1, 0.3f, 1), false, Time.deltaTime);
        //    }
        //}

        //gObject.layer = Wrapper.gameObject.layer;
        //Transform[] children = gObject.GetComponentsInChildren<Transform>(true);
        //foreach (Transform child in children)
        //{
        //    child.gameObject.layer = gObject.layer;
        //}

    }

    private void OnStartTouch(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = mousePositionAction.action.ReadValue<Vector2>();

        Ray ray = gameManager.ActiveCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Debug.Log($"hit.collider={hit.collider}");
            VehicleAssembler bm = hit.collider.GetComponentInParent<VehicleAssembler>();

            if (bm != null)
            {
                OnEnableInputCamera();
            }
        }
    }

    private void OnEndTouch(InputAction.CallbackContext context)
    {
        OnDisableInputCamera();
    }

    private void OnEnableInputCamera()
    {
        CinemachineCameraInputController.enabled = true;
    }

    private void OnDisableInputCamera()
    {
        CinemachineCameraInputController.enabled = false;
    }

    private void OnCancelColorModify()
    {
        activeColorModifyItem.color = Color.clear;
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // Read the current mouse position
        Vector2 mousePosition = mousePositionAction.action.ReadValue<Vector2>();

        // Create a ray from the camera through the mouse position
        Ray ray = gameManager.ActiveCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // Perform the raycast
        if (Physics.Raycast(ray, out hit))
        {
            // Log the name of the object hit
            Debug.Log($"Clicked on {hit.point}: " + hit.collider.gameObject.name);

            // You can add further logic here, e.g., call a method on the hit object
            // hit.collider.GetComponent<YourCustomScript>()?.HandleClick();
            IColored coloredObject = hit.collider.GetComponentInParent<IColored>();
            VehicleAssembler bm = hit.collider.GetComponentInParent<VehicleAssembler>();

            Debug.DrawLine(hit.point, gameManager.ActiveCamera.transform.position, Color.yellow, 5);

            if (coloredObject != null && !HelperVoxel.AreColorsApproximatelyEqual(activeColorModifyItem.color, Color.clear))
            {
                FillData output = coloredObject.OnFill(hit.point);

                output.outputColor = activeColorModifyItem.color;

                var newColorsModify = gameManager.StateManager.OnFillMachine(output);

                var activeMachine = gameManager.StateManager.statePlayer.machines[gameManager.StateManager.statePlayer.indexActiveMachine];

                // bm.MachineLevelData = new MachineLevelData
                // {
                //     id = activeMachine.name,
                //     colorsModify = activeMachine.colorsModifies,
                // };

                //bm.ReDraw(activeMachine.data.colorsModifies);

                //OnCancelColorModify();

                GarageUIEvents.FillOk?.Invoke();
            }
        }
    }

    void CreateTowerItems()
    {
        //dataTowers = gameManager.ResourceSystem.GetAllShopTower(); // gameManager.Settings.machines.Select(x => x.towers).ToList();

        //float i = 0f;
        //foreach (var item in dataTowers)
        //{
        //    CreateTowerItem(item, new Vector2(0, i));
        //    i += step;
        //}
    }

    //void CreateTowerItem(GameTowerShop configsTowers, Vector2 point)
    //{
    //    var gObject = Instantiate(
    //        gameManager.Settings.prefabGarageItemTower,
    //        Vector3.zero,
    //        Quaternion.identity,
    //        Wrapper3dModels
    //    );

    //    UIGarageTowerItemView obj = gObject.GetComponent<UIGarageTowerItemView>();
    //    if (obj != null)
    //    {
    //        towersGameObjects.Add(obj);

    //        var configActiveMachine = gameManager.ResourceSystem.GetAllMachines().Find(x => x.name == gameManager.StateManager.statePlayer.machines[activeIndexMachine].name);

    //        obj.Init(configsTowers, configActiveMachine);
    //    }

    //    // targetPanelSettings = obj.GetComponentInChildren<UIDocument>().panelSettings;
    //    // if (targetPanelSettings != null)
    //    // {
    //    //     targetPanelSettings.SetScreenToPanelSpaceFunction(ScreenCoordinatesToRenderTexture);
    //    //     Debug.Log($"Found settings!");
    //    // } else
    //    // {
    //    //     Debug.LogWarning($"Not found settings!");
    //    // }

    //    gObject.layer = Wrapper3dModels.gameObject.layer;
    //    Transform[] children = gObject.GetComponentsInChildren<Transform>(true);
    //    foreach (Transform child in children)
    //    {
    //        child.gameObject.layer = gObject.layer;
    //    }

    //    gObject.transform.SetLocalPositionAndRotation(new Vector3(point.x, -point.y, 0), Quaternion.Euler(0, 0, 0));
    //}
}
