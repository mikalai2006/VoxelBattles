
using UnityEngine;
using UnityEngine.UIElements;

public class UIShopItemView : UILocaleBase
{
  [SerializeField] private UIDocument _uiDoc;
  VehicleAssembler baseMachine;
  private VisualElement m_Root;
  private VisualElement m_Wrapper;
  public GameObject ObjectTargetCamera;

  public void Awake()
  {
    // mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
    // renderTextureCamera = GameObject.FindGameObjectWithTag("SecondCamera")?.GetComponent<Camera>();
    
    m_Root = _uiDoc.rootVisualElement;

    m_Wrapper = m_Root.Q<VisualElement>(UINames.VisualElementWrapper);
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

  public void Init(VehiclePresetAsset _config, MachineLevelData dataInput)
  {
    CreateMachine(_config, dataInput);

    Initialize(m_Root);
  }

  void Update()
  {
      if (_gameManager.ActiveCamera == null)
      {
          return;
      }
      
      Vector3 relativePos = _gameManager.ActiveCamera.transform.position - _uiDoc.transform.position;
      Quaternion rotation = Quaternion.LookRotation(relativePos, -_uiDoc.transform.up);
      rotation.x = 0;
      rotation.z = 0;
      _uiDoc.gameObject.transform.rotation = rotation;

  }

    void CreateMachine(VehiclePresetAsset configMachine, MachineLevelData data)
    {
        //var gObject = Instantiate(
        //    configMachine.machinePrefab,
        //    Vector3.zero,
        //    Quaternion.identity,
        //    transform
        //);
        //gObject.transform.localPosition = Vector3.zero;

        //BaseMachine obj = gObject.GetComponent<BaseMachine>();
        //if (obj != null)
        //{

        //    obj.AreaSearch.gameObject.SetActive(false);
        //    obj.GetComponent<PlayerController>().enabled = false;
        //    obj.GetComponent<PlayerInput>().enabled = false;
        //    obj.GetComponent<StateController>().enabled = false;
        //    obj.GetComponentInChildren<HealthBarController>().gameObject.SetActive(false);

        //    // if (data.data.dataDetails.Count == 0)
        //    // {
        //    //     data.data.dataDetails.Add(new DataDetail()
        //    //     {
        //    //       nameConfig = configMachine.body.Config.name,
        //    //       number = 0,
        //    //       offset = configMachine.body.offsetBody,
        //    //       type = VehicleDetailType.Body
        //    //     });

        //    //     for (int i = 0; i < configMachine.catterpillars.Count; i++)
        //    //     {
        //    //       var item = configMachine.catterpillars.ElementAt(i);
        //    //       data.data.dataDetails.Add(new DataDetail()
        //    //       {
        //    //         nameConfig = item.Config.name,
        //    //         number = i,
        //    //         offset = item.offsetCat,
        //    //         type = VehicleDetailType.Caterpillar,
        //    //       });
        //    //     }

        //    //     for (int i = 0; i < configMachine.wheels.Count; i++)
        //    //     {
        //    //       var item = configMachine.wheels.ElementAt(i);
        //    //       data.data.dataDetails.Add(new DataDetail()
        //    //       {
        //    //         nameConfig = item.Config.name,
        //    //         number = i,
        //    //         offset = item.offsetWheel,
        //    //         type = VehicleDetailType.Wheel,
        //    //       });
        //    //     }


        //    //     for (int i = 0; i < configMachine.towers.Count; i++)
        //    //     {
        //    //       var item = configMachine.towers.ElementAt(i);
        //    //       data.data.dataDetails.Add(new DataDetail()
        //    //       {
        //    //         nameConfig = item.Config.name,
        //    //         number = i,
        //    //         offset = item.offsetTower,
        //    //         type = VehicleDetailType.Tower,
        //    //         ido = item.ido,
        //    //         parentId = item.parentId,
        //    //       });

        //    //       for (int j = 0; j < item.muzzles.Count; j++)
        //    //       {
        //    //         var itemMuzzle = item.muzzles.ElementAt(j);
        //    //         data.data.dataDetails.Add(new DataDetail()
        //    //         {
        //    //           nameConfig = itemMuzzle.Config.name,
        //    //           number = j,
        //    //           offset = itemMuzzle.offsetMuzzle,
        //    //           type = VehicleDetailType.Muzzle
        //    //         });
        //    //       }
        //    //     }
        //    // }

        //    obj.Init(configMachine, data);

        //    obj.Body.OnSetAngleBody(45);
        //}
    }
}
