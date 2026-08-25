
//using UIToolkitLibrary;
//using UnityEngine;
//using UnityEngine.UIElements;

//// [RequireComponent(typeof(UIDocument))]
//public class HealthBarController : MonoBehaviour
//{
//    GameManager gameManager => GameManager.Instance;
//    [SerializeField] Transform transformMachine;
//    [SerializeField] private BaseMachine Machine;
//    [Header("HealthBar Elements")]
//    [SerializeField] string m_HealthBarName = "HealthBarBase";
//    [SerializeField] string m_CharacterName = "Character Name";
//    [SerializeField] bool m_ShowStat = true;
//    [SerializeField] bool m_ShowNameplate = true;
//    [SerializeField] StyleSheet m_StyleSheetOverride;
//    Label playerName;
//    VisualElement rankElement;

//    [SerializeField] float m_LowHPPercent = 25;
//    [SerializeField] Transform transformToFollow;
//    [SerializeField]  HealthBarComponent m_HealthBar;
//    // [SerializeField] Sprite m_LowHPImage;
//    // [SerializeField] StyleBackground m_OriginalHPImage;
//    [SerializeField] UIDocument m_HealthBarDoc;
//    [SerializeField] private Camera _camera;

//    [Header("Настройки позиционирования ")]
//    // [SerializeField] private List<Vector3> maxOffsets;
//    [SerializeField] Vector2 m_WorldSize = new Vector2(1.2f, 0.6f);
//    [SerializeField] Vector3 offset = new Vector3(0f, 1f, 0);
//    [Tooltip("x - нижняя граница(от), y - верхняя граница(до), z - значение maxOffset")]
//    public Vector2 rangeLerp = new Vector2(4f, 10f);
//    public float delimiter;
//    // [SerializeField] private WorldSpaceUIDocument uiDocumentPrefab;


//    // void OnEnable()
//    // {
//    //     MediaQueryEvents.CameraResized += OnCameraResized;
//    // }

//    // void OnDisable()
//    // {
//    //     MediaQueryEvents.CameraResized -= OnCameraResized;
//    // }
//    void Awake()
//    {
//        var cameraGO = GameObject.FindGameObjectWithTag("CameraGame");
//        if (cameraGO != null)
//        {
//            _camera = cameraGO.GetComponent<Camera>();
//        }


//        HealthBarSetup();



//        // maxOffsets = new List<Vector3>()
//        // {
//        //     new Vector3(0, 10, 8),
//        //     new Vector3(11, 20, 15),
//        //     new Vector3(21, 30, 25),
//        // };
//    }

//    void OnBecameVisible()
//    {
//        if (!Application.isPlaying) return;

//        BaseMachine bm = transform.GetComponentInParent<BaseMachine>();
//        if (bm)
//        {
//            bm.SetInCamera(true);
//        }
//    }

//    void OnBecameInvisible()
//    {
//        if (!Application.isPlaying) return;

//        BaseMachine bm = transform.GetComponentInParent<BaseMachine>();
//        if (bm)
//        {
//            bm.SetInCamera(false);
//        }
//    }

//    void FixedUpdate()
//    {
//        if (_camera == null)
//        {
//            return;
//        }
        
//        Vector3 relativePos = _camera.transform.position - transform.position;
//        Quaternion rotation = Quaternion.LookRotation(relativePos, -transform.up);
//        rotation.x = 0;
//        rotation.z = 0;
//        gameObject.transform.rotation = rotation;

//        UpdateHealthBar();
//    }

//    void HealthBarSetup()
//    {
//        // var obj = Instantiate(uiDocumentPrefab, transform, true);
//        // obj.transform.localPosition = Vector3.zero;

//        m_HealthBarDoc = GetComponentInChildren<UIDocument>();
        
//        VisualElement rootElement = m_HealthBarDoc.rootVisualElement;
//        rootElement.usageHints = UsageHints.GroupTransform & UsageHints.DynamicTransform & UsageHints.DynamicColor;
        
//        if (m_StyleSheetOverride != null)
//        {
//            rootElement.styleSheets.Clear();
//            rootElement.styleSheets.Add(m_StyleSheetOverride);
//        }
        
//        m_HealthBar = rootElement.Q<HealthBarComponent>(m_HealthBarName);
//        m_HealthBar.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
//        m_HealthBar.HealthBarTitle = m_CharacterName;

//        m_HealthBar.HealthData = new HealthData();

//        m_HealthBar.style.backgroundColor = new StyleColor(gameManager.Theme.bgColor);
//        m_HealthBar.style.flexShrink = 0;
//        m_HealthBar.style.width = new StyleLength(200);
//        m_HealthBar.style.height = new StyleLength(40);
//        VisualElement healthBarBg = m_HealthBar.Q<VisualElement>(HealthBarComponent.IDNames.HealthBarBackground);
//        healthBarBg.style.backgroundColor = new StyleColor(gameManager.Theme.bgColor);

//        playerName = m_HealthBar.Q<Label>(HealthBarComponent.IDNames.HealthBarTitle);
//        playerName.text = Machine.MachineLevelData.name;

//        rankElement = m_HealthBar.Q<VisualElement>(HealthBarComponent.IDNames.HealthBarRank);
//        var rank = gameManager.Settings.ranks[Machine.MachineLevelData.rank];
//        rankElement.style.backgroundImage = new StyleBackground(rank.sprite);

//        ShowNameAndStats(m_ShowNameplate, m_ShowStat);
//        // MoveToWorldPosition(m_HealthBar, transformToFollow.position, m_WorldSize);
//    }

//    public void DisplayHealthBar(bool state)
//    {
//        if (m_HealthBarDoc == null)
//            return;

//        VisualElement rootElement = m_HealthBarDoc.rootVisualElement;
//        rootElement.style.display = (state) ? DisplayStyle.Flex : DisplayStyle.None;
//    }


//    /// <summary>
//    /// Setup a health bar
//    /// </summary>
//    /// <param name="health"></param>
//    /// <param name="maxHealth"></param>
//    public void SetHealth(float health, float maxHealth)
//    {
//        if (m_HealthBar == null)
//        {
//            HealthBarSetup();
//        }
        
//        m_HealthBar.HealthData.CurrentHealth = health;
//        m_HealthBar.HealthData.MaximumHealth = maxHealth;

//    }

    
//    // Switch health bar sprites when low on HP
//    public void UpdateHealth(float health)
//    {
//        // if (m_OriginalHPImage == null)
//        // {
//        //     // Store the original background style to reset the health bar sprite
//        //     m_OriginalHPImage = m_HealthBar.Q<VisualElement>(k_HPFillImage).style.backgroundImage;
//        // }

//        float lowHealth = m_HealthBar.HealthData.MaximumHealth * m_LowHPPercent / 100;
//        VisualElement healthBarProgress = m_HealthBar.Q<VisualElement>(HealthBarComponent.IDNames.HealthBarProgress);

//        if (health < lowHealth)
//        {
//            // fill.style.backgroundImage = new StyleBackground(m_LowHPImage);
//            healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorAccent);
//        }
//        else
//        {
//            healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorCompleted);
//            // fill.style.backgroundImage = m_OriginalHPImage;
//        }


//        m_HealthBar.HealthData.CurrentHealth = Mathf.Round(health * 100f) / 100f;;
//    }

//    void ShowNameAndStats(bool nameVisible, bool statVisible)
//    {
//        VisualElement stat = m_HealthBar.Q<Label>(HealthBarComponent.IDNames.HealthBarStat);

//        if (playerName != null)
//        {
//            playerName.visible = nameVisible;
//        }

//        if (stat != null)
//        {
//            stat.visible = statVisible;
//        }

//    }

//    // moves health bar to match world position
//    void MoveToWorldPosition(VisualElement element, Vector3 worldPosition, Vector2 worldSize)
//    {
//        // BaseMachine bm = transformMachine.GetComponent<BaseMachine>();

//        var distance = Vector3.Distance(_camera.transform.position, transformToFollow.transform.position);

//        if (Machine.isVisible)
//        {
//            var lerpStep = distance / delimiter;

//            Rect rect = RuntimePanelUtils.CameraTransformWorldToPanelRect(element.panel, worldPosition + (offset * Mathf.Min(15, Mathf.Max(4f, lerpStep))), worldSize, _camera); //  + new Vector3(0, 0.5f, 0)

//            // element.transform.position = rect.position;
//            element.style.display = DisplayStyle.Flex;

//            // Debug.Log($"rect size = {rect}/ {distance} [[[{rangeLerp}<{Mathf.Min(rangeLerp.y, Mathf.Max(rangeLerp.x, lerpStep))}><{lerpStep}>]]]");
//            // element.style.scale = new StyleScale(new Vector3(2,2,2));
//            //element.style.translate = new StyleTranslate(new Translate(rect.x, rect.y));
//            // transform.rotation = _camera.transform.rotation;
//        }
//        else
//        {
//            element.style.display = DisplayStyle.None;
//        }

//    }

//    // Refresh health bar setup when camera updates
//    void OnCameraResized()
//    {
//        UpdateHealthBar();
//    }

//    void UpdateHealthBar()
//    {
//        ShowNameAndStats(m_ShowNameplate, m_ShowStat);
//        // MoveToWorldPosition(m_HealthBar, transformToFollow.position, m_WorldSize);
//    }

//}
