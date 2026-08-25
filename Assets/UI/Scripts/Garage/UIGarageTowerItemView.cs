
using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UIGarageTowerItemView : UILocaleBase
{
  [SerializeField] private UIDocument _uiDoc;
  // BaseMachine baseMachine;
  private VisualElement m_Root;
  private VisualElement m_Wrapper;
  private Label m_Name;
  private Label m_Price;
  private Image m_ImageCoin;
  private Label m_Description;
  public GameObject ObjectTargetCamera;
  private int _previousWidth;

  public void Awake()
  {
    // mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
    // renderTextureCamera = GameObject.FindGameObjectWithTag("SecondCamera")?.GetComponent<Camera>();
    if (_uiDoc != null)
    {
      m_Root = _uiDoc.rootVisualElement;
      m_Wrapper = m_Root.Q<VisualElement>(UINames.VisualElementWrapper);
      m_Name = m_Root.Q<Label>(UINames.LabelName);
      m_Price = m_Root.Q<Label>(UINames.LabelPrice);
      m_ImageCoin = m_Root.Q<Image>(UINames.ImageCoin);
      m_ImageCoin.image = _gameManager.Theme.spriteCoin.texture;
      m_Description = m_Root.Q<Label>(UINames.LabelDescription);
      SetFocus(false);
    }
  }

  void Start()
  {
    _previousWidth = Screen.width;
    SetSizeWrapper();

  }

  void Update()
  {
    if (_previousWidth != Screen.width)
    {
      _previousWidth = Screen.width;
      SetSizeWrapper();
    }
  }

  public void SetFocus(bool status)
  {
    if (m_Wrapper == null) return;

    if (status)
    {
      m_Wrapper.AddToClassList("panel-primary");
      if (m_Wrapper.ClassListContains("panel-secondary"))
      {
        m_Wrapper.RemoveFromClassList("panel-secondary");
      }
      m_Description.style.display = DisplayStyle.Flex;
    } else
    {
      m_Wrapper.AddToClassList("panel-secondary");
      if (m_Wrapper.ClassListContains("panel-primary"))
      {
        m_Wrapper.RemoveFromClassList("panel-primary");
      }
      m_Description.style.display = DisplayStyle.None;
    }
  }

    // void OnEnable()
    // {

    //   if (_uiDoc.panelSettings != null)
    //   {
    //       _uiDoc.panelSettings.SetScreenToPanelSpaceFunction((Vector2 position) =>
    //       {
    //         var invalidPosition = new Vector2(float.NaN, float.NaN);

    //         var cameraRay = mainCamera.ScreenPointToRay(_shopManager.mousePositionAction.action.ReadValue<Vector2>());
    //         Debug.DrawRay(cameraRay.origin, cameraRay.direction * 100, Color.magenta);
    //     Debug.Log($"DrawRay");
    //         return invalidPosition;
    //       });
    //     Debug.Log($"Set ScreenCoordinatesToRenderTexture");
    //   }
    // }

    // void OnDisable()
    // {
    //   if (_uiDoc.panelSettings != null)
    //   {
    //     _uiDoc.panelSettings.SetScreenToPanelSpaceFunction(null);
    //   }
    // }

    // private Vector2 ScreenCoordinatesToRenderTexture(Vector2 screenPosition)
    // {

    //     Debug.Log($"ScreenCoordinatesToRenderTexture");
    //     // // As an example from the docs, assuming a helper script (UITextureProjection) handles the 
    //     // // complex world-to-panel logic (which often involves a raycast):
    //     // // If you are using the exact script provided in the Unity docs for this scenario, 
    //     // // the implementation of ScreenCoordinatesToRenderTexture would use the renderTextureCamera.

    //     // // Упрощенный расчет для полноэкранного RenderTexture, отображаемого на RawImage:
    //     // // Получение точки в области просмотра (диапазон 0-1) из положения на экране с помощью основной камеры.
    //     // Vector3 viewportPoint = mainCamera.ScreenToViewportPoint(screenPosition);

    //     // // Преобразуйте точку области просмотра в локальную позицию внутри области просмотра RenderTexture.
    //     // // Примечание: UI Toolkit использует верхнюю левую точку отсчета для оси Y, в то время как Screen/Viewport обычно использует нижнюю левую точку.

    //     // // В зависимости от конкретной конфигурации это часто требует инвертирования координаты Y.

    //     // // Простое инвертирование Y при необходимости:

    //     // // viewportPoint.y = 1.0f - viewportPoint.y;

    //     // // Сопоставьте координаты области просмотра с фактическими размерами панели в пикселях

    //     // // (которые должны совпадать с размерами RenderTexture для идеального сопоставления).
    //     // float panelWidth = targetPanelSettings.targetTexture.width;
    //     // float panelHeight = targetPanelSettings.targetTexture.height;

    //     // Debug.Log($"Click {panelWidth}/{panelHeight}");

    //     // Vector2 panelPosition = new Vector2(
    //     //     viewportPoint.x * panelWidth,
    //     //     viewportPoint.y * panelHeight
    //     // );

    //     // return panelPosition;
    //     var invalidPosition = new Vector2(float.NaN, float.NaN);
    //         screenPosition.y = Screen.height - screenPosition.y;
    //         Ray cameraRay = mainCamera.ScreenPointToRay(screenPosition);

    //         RaycastHit hit;
    //         if (!Physics.Raycast(cameraRay, out hit))
    //         {
    //             return invalidPosition;
    //         }

    //         var targetTexture = _uiDoc.panelSettings.targetTexture;
    //         Vector2 pixelUV = hit.textureCoord;

    //         pixelUV.y = 1 - pixelUV.y;
    //         pixelUV.x *= targetTexture.width;
    //         pixelUV.y *= targetTexture.height;
    //     return pixelUV;
    // }

  //public void Init(GameTowerShop _configsTowers, GameMachine configMachine)
  //{
  //  List<DataDetail> dataDetails = new () {};
  //  // dataDetails.Add(new DataDetail()
  //  // {
  //  //   nameConfig = configMachine.body.Config.name,
  //  //   offset = configMachine.body.offsetBody,
  //  //   type = VehicleDetailType.Body,
  //  // });

  //  // init data towers.
  //  for (int i = 0; i < _configsTowers.items.Count; i++)
  //  {
  //    var item = _configsTowers.items.ElementAt(i);
  //    var uid = System.Guid.NewGuid().ToString();

  //    dataDetails.Add(new DataDetail()
  //    {
  //      nameConfig = item.Config.name,
  //      number = i,
  //      offset = item.offsetTower,
  //      type = VehicleDetailType.Tower,
  //      ido = string.IsNullOrEmpty(item.ido) ? uid : item.ido,
  //      parentId = item.parentId,
  //    });

  //    // init data muzzles.
  //    for (int j = 0; j < item.muzzles.Count; j++)
  //    {
  //      var itemMuzzle = item.muzzles.ElementAt(j);
  //      dataDetails.Add(new DataDetail()
  //      {
  //        nameConfig = itemMuzzle.Config.name,
  //        number = j,
  //        offset = itemMuzzle.offsetMuzzle,
  //        type = VehicleDetailType.Muzzle,
  //        parentId = string.IsNullOrEmpty(item.ido) ? uid : item.ido
  //      });
  //    }
  //  }


  //  MachineLevelData machineLevelData = new MachineLevelData()
  //  {
  //    name = configMachine.name,
  //    data = new StateMachinePlayerData()
  //    {
  //      dataDetails = dataDetails
  //    }
  //  };
    

  //  CreateGameObject(machineLevelData, configMachine);
    
  //  if (_uiDoc != null && m_Root != null)
  //  {
  //    Initialize(m_Root);
  //  }
  //}

  //   void Update()
  // {
  //     if (_gameManager.ActiveCamera == null)
  //     {
  //         return;
  //     }
      
  //     Vector3 relativePos = _gameManager.ActiveCamera.transform.position - _uiDoc.transform.position;
  //     Quaternion rotation = Quaternion.LookRotation(relativePos, -_uiDoc.transform.up);
  //     rotation.x = 0;
  //     rotation.z = 0;
  //     _uiDoc.gameObject.transform.rotation = rotation;

  // }

  void CreateGameObject(MachineLevelData data, VehiclePresetAsset configMachine)
  {
      //var gObject = Instantiate(
      //    configMachine.machinePrefab,
      //    Vector3.zero,
      //    //new Vector3(data.isBot ? 30 : 241, 0.5f, data.isBot ? 30 : 22),
      //    // new Vector3(node.position.x, 0.5f, node.position.y),
      //    Quaternion.identity,
      //    transform
      //);
      //gObject.name = $"{configMachine.name}_{data.name}";

      //// var towerGO = Instantiate(
      ////     gameTowerOption.Config.prefab,
      ////     Vector3.zero,
      ////     Quaternion.identity,
      ////     transform
      //// );
      //// towerGO.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
      //// towerGO.transform.localRotation = Quaternion.Euler(15, 113, -19.5f);
      //BaseMachine obj = gObject.GetComponent<BaseMachine>();
      //if (obj != null)
      //{
      //  // var rb = gObject.AddComponent<Rigidbody>();
      //  // if (rb != null)
      //  // {
      //  //   Destroy(rb);
      //  // }

      //  // obj.GetComponent<PlayerController>().enabled = false;
      //  // obj.GetComponent<PlayerInput>().enabled = false;
      //  // var navMeshAgent = obj.navMeshAgent; //obj.GetComponent<NavMeshAgent>();
      //  // if (navMeshAgent != null)
      //  // {
      //  //     navMeshAgent.enabled = false;
      //  // };
      //  // var lightComponent = obj.GetComponentInChildren<Light>();
      //  // if (lightComponent)
      //  // {
      //  //     lightComponent.enabled = false;
      //  // }
      //  // obj.GetComponent<StateController>().enabled = false;

      //  // obj.IsSleep = true;

      //  // obj.Init(configMachine, data);
        
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
      //    obj.Init(configMachine, data, new Vector3(0.0025f, 0.0025f, 0.0025f));

      //    // foreach (var tower in obj.Towers)
      //    // {
      //    //   // tower.OnSetAngleTower(new Vector3(46,-90,30), false, Time.deltaTime);
      //    //   foreach (var muzzle in tower.Muzzles)
      //    //   {
      //    //     muzzle.transform.localRotation = Quaternion.Euler(0, 90, 0);
      //    //   }
      //    // }

      //    obj.transform.localRotation = Quaternion.Euler(0, 90, -10);
      //    obj.transform.localPosition = new Vector3(0.09f, 0.02f, -0.1f);
      //}
      //// if (baseTower != null)
      //// {
      ////     baseTower.Init(null, gameTowerOption, data);
      ////     baseTower.transform.localPosition = Vector3.zero;
      ////     baseTower.OnSetAngleTower(new Vector3(0.305000007f,-0.247999996f,-0.342000008f), true, Time.deltaTime);

      ////     // foreach (var item in config.muzzles)
      ////     // {
      ////     //   var muzzleGO = Instantiate(
      ////     //       item.Config.prefab,
      ////     //       Vector3.zero,
      ////     //       Quaternion.identity,
      ////     //       baseTower.MuzzlesBox.transform
      ////     //   );
      ////     //   BaseMuzzle baseMuzzle = muzzleGO.GetComponent<BaseMuzzle>();
      ////     //   if (baseMuzzle != null)
      ////     //   {
      ////     //     baseMuzzle.Init(null, baseTower, item, 0, data);
      ////     //   }
      ////     //   baseMuzzle.transform.localRotation = Quaternion.Euler(0, 90, 0);
      ////     // }
      //// }
      //// towerGO.transform.localPosition = new Vector3(0, 0, -0.1f);
  }
    
  void SetSizeWrapper()
  {
    m_Wrapper.style.width = 250; //(float)(Screen.width / 2960f) * 200;
    m_Wrapper.style.height = 120;
  }
}
