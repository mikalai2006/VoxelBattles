
// using Cysharp.Threading.Tasks;
// using UIToolkitLibrary;
// using UnityEngine;
// using UnityEngine.UIElements;

// [RequireComponent(typeof(UIDocument))]
// public class TopSideBarController : MonoBehaviour
// {
//     GameManager gameManager => GameManager.Instance;
//     [SerializeField] private LevelManager levelManager;
//     [Header("Elements")]
//     [SerializeField] string m_WrapperName = "TopSideBarBase";
//     [SerializeField] string m_CharacterName = "Character Name";
//     [SerializeField] bool m_ShowStat = false;
//     [SerializeField] bool m_ShowPlayerName = true;
//     [SerializeField] StyleSheet m_StyleSheetOverride;

//     [SerializeField] float m_LowHPPercent = 25;
//     // [SerializeField] Transform transformToFollow;
//     [SerializeField] TopSideBarComponent m_TopSideBar;
//     [SerializeField] private UIDocument m_RootDoc;
//     [SerializeField] VisualTreeAsset m_assetInfoItem;
//     [SerializeField] VisualElement m_BonusBox;
//     [SerializeField] VisualElement m_InfoBox;
//     Camera _camera;

//     [Header("Настройки позиционирования ")]
//     // [SerializeField] private List<Vector3> maxOffsets;
//     [SerializeField] Vector2 m_WorldSize = new Vector2(1.2f, 0.6f);
//     [SerializeField] Vector3 offset = new Vector3(0f, 1f, 0);
//     [Tooltip("x - нижняя граница(от), y - верхняя граница(до), z - значение maxOffset")]
//     public Vector2 rangeLerp = new Vector2(4f, 10f);
//     public float delimiter;
//     [SerializeField] private InfoBoxRowComponentPool poolInfoRows;

//     [Tooltip("Список уведомлений из инфоблока")]
//     // private List<AppInfoDamageData> messagesInfo;

// #region Unity methods
//     // void OnEnable()
//     // {
//     //     MediaQueryEvents.CameraResized += OnCameraResized;
//     // }

//     // void OnDisable()
//     // {
//     //     MediaQueryEvents.CameraResized -= OnCameraResized;
//     // }
//     void Awake()
//     {
//         // messagesInfo = new List<AppInfoDamageData>();
        
//         // _levelManager = GameObject.FindGameObjectWithTag("LevelManager")?.GetComponent<LevelManager>();

//         _camera = GameObject.FindGameObjectWithTag("CameraGame").GetComponent<Camera>();
//         SetupController();

//         // maxOffsets = new List<Vector3>()
//         // {
//         //     new Vector3(0, 10, 8),
//         //     new Vector3(11, 20, 15),
//         //     new Vector3(21, 30, 25),
//         // };
//         GameSceneEvents.AddInfoDamage += AddInfoItem;
        
//         GameSceneEvents.RefreshHP += UpdateHealth;
//         GameSceneEvents.SetHP += SetHealth;
//     }

//     void FixedUpdate()
//     {
//         UpdateHealthBar();
//     }

//     // void Update()
//     // {
//     //     for (int i = 0; i < messagesInfo.Count; i++)
//     //     {
//     //         AppInfoDamageData _data = messagesInfo[i];
//     //         _data.time += Time.deltaTime;
//     //         messagesInfo[i] = _data;
//     //     }

//     //     UpdateInfoBox();
//     // }

//     void OnDestroy()
//     {
//         m_TopSideBar.ButtonExit.clickable.clicked -= OnToStartMenu;
        
//         GameSceneEvents.AddInfoDamage -= AddInfoItem;
        
//         GameSceneEvents.RefreshHP -= UpdateHealth;
//         GameSceneEvents.SetHP -= SetHealth;
//     }
// #endregion

//     void SetupController()
//     {
//         // m_RootDoc = GetComponent<UIDocument>();
        
//         VisualElement rootElement = m_RootDoc.rootVisualElement;
//         rootElement.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        
//         if (m_StyleSheetOverride != null)
//         {
//             rootElement.styleSheets.Clear();
//             rootElement.styleSheets.Add(m_StyleSheetOverride);
//         }
        
//         m_TopSideBar = rootElement.Q<TopSideBarComponent>(m_WrapperName);
//         m_TopSideBar.pickingMode = PickingMode.Ignore;
//         m_TopSideBar.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
//         m_TopSideBar.TopSideBarPlayerName = m_CharacterName;

//         m_TopSideBar.TopSideBarData = new TopSideBarData();

//         // m_TopSideBar.style.backgroundColor = new StyleColor(gameManager.Theme.bgColor);
//         m_TopSideBar.style.flexShrink = 1;
//         VisualElement wrapperBg = m_TopSideBar.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarWrapper);
//         m_BonusBox = m_TopSideBar.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarBonusBox);
//         m_InfoBox = m_TopSideBar.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarInfoBox);

//         ShowNameAndStats(m_ShowPlayerName, m_ShowStat);

//         m_TopSideBar.ButtonExit.clickable.clicked += OnToStartMenu;
//     }

//     private void OnToStartMenu()
//     {
//         if (levelManager == null)
//         {
//             // CloseSettings();
//         }
//         else
//         {
//             AudioManager.Instance.Click();

//             gameManager.ChangeState(GameState.CloseLevel);

//             // var dashBoard = new StartUIOperation();
//             // dashBoard.ShowAndHide().Forget();

//             var uiManager = new UIManagerOperation();
//             uiManager.ShowAndHide().Forget();
//         }
//     }

//     public void AddInfoItem(AppInfoDamageData data)
//     {
//         // if (m_RootDoc == null)
//         //     return;
        
//         // // messagesInfo.Add(data);

//         // InfoBoxRowComponent el = poolInfoRows.GetObject(); // new InfoBoxRow{ name = "InfoBoxRow"};
//         // // el.style.flexGrow = 1;
//         // // el.style.flexDirection = FlexDirection.Row;
//         // // Color color = gameManager.Theme.bgColor;
//         // // color.a = 0.3f;
//         // // el.style.backgroundColor = new StyleColor(color);

//         // el.Init(this, data);

//         // // var infoEl = new VisualElement {name="infoItem"};
//         // // infoEl.Add(new Label
//         // // {
//         // //     text = "text",
//         // // });
        
//         // m_InfoBox.Add(el);
//     }

//     public void RemoveInfoItem(InfoBoxRowComponent el)
//     {
//         m_InfoBox.Remove(el);
//         poolInfoRows.ReturnObject(el);
//     }

//     // void UpdateInfoBox()
//     // {
//     //     m_InfoBox.Clear();

//     //     for (int i = messagesInfo.Count - 1; i >= 0; i--)
//     //     {
//     //         if (messagesInfo[i].time > 1f)
//     //         {
//     //             messagesInfo.RemoveAt(i);
//     //             continue;
//     //         }

//     //         VisualElement el = new VisualElement();
//     //         el.style.flexGrow = 1;
//     //         el.style.flexDirection = FlexDirection.Row;
//     //         Color color = gameManager.Theme.bgColor;
//     //         color.a = 0.3f;
//     //         el.style.backgroundColor = new StyleColor(color);

//     //         VisualElement infoEl = m_assetInfoItem.Instantiate();
//     //         infoEl.style.flexGrow = 1;
//     //         Label infoElLabel = infoEl.Q<Label>("InfoItemLabel");
//     //         infoElLabel.text = messagesInfo[i].kto.MachineLevelData.name;
//     //         el.Add(infoEl);

//     //         VisualElement infoElKomu = m_assetInfoItem.Instantiate();
//     //         infoElKomu.style.flexGrow = 1;
//     //         Label infoElKomuLabel = infoElKomu.Q<Label>("InfoItemLabel");
//     //         infoElKomuLabel.text = messagesInfo[i].komy.MachineLevelData.name;
//     //         el.Add(infoElKomu);

//     //         // var infoEl = new VisualElement {name="infoItem"};
//     //         // infoEl.Add(new Label
//     //         // {
//     //         //     text = "text",
//     //         // });
            
//     //         m_InfoBox.Add(el);
//     //     }
//     // }

//     // public void DisplayHealthBar(bool state)
//     // {
//     //     if (m_RootDoc == null)
//     //         return;

//     //     VisualElement rootElement = m_RootDoc.rootVisualElement;
//     //     rootElement.style.display = (state) ? DisplayStyle.Flex : DisplayStyle.None;
//     // }


//     /// <summary>
//     /// Setup a health bar
//     /// </summary>
//     /// <param name="health"></param>
//     /// <param name="maxHealth"></param>
//     void SetHealth(BaseMachine bm)
//     {
//         if (m_TopSideBar == null)
//         {
//             SetupController();
//         }
        
//         m_TopSideBar.TopSideBarData.CurrentHealth = 1;
//         m_TopSideBar.TopSideBarData.MaximumHealth = 1;

//         SetPlayerName(bm);
//         SetRank(bm);
//     }

//     void SetRank(BaseMachine bm)
//     {
//         var rank = gameManager.Settings.ranks[bm.MachineLevelData.rank];
//         m_TopSideBar.m_Rank.style.backgroundImage = new StyleBackground(rank.sprite);
//     }

//     void SetPlayerName(BaseMachine bm)
//     {
//         m_TopSideBar.TopSideBarPlayerName = bm.MachineLevelData.name;
//     }

    
//     // Switch health bar sprites when low on HP
//     void UpdateHealth(BaseMachine bm)
//     {
//         float health = bm.Data.ContainerData.levelDestruction;
//         // if (m_OriginalHPImage == null)
//         // {
//         //     // Store the original background style to reset the health bar sprite
//         //     m_OriginalHPImage = m_HealthBar.Q<VisualElement>(k_HPFillImage).style.backgroundImage;
//         // }

//         float lowHealth = m_TopSideBar.TopSideBarData.MaximumHealth * m_LowHPPercent / 100;
//         VisualElement healthBarProgress = m_TopSideBar.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarProgress);

//         if (health < lowHealth)
//         {
//             // fill.style.backgroundImage = new StyleBackground(m_LowHPImage);
//             healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorAccent);
//         }
//         else
//         {
//             healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorCompleted);
//             // fill.style.backgroundImage = m_OriginalHPImage;
//         }

//         m_TopSideBar.TopSideBarData.CurrentHealth = 1 - health;

//         SetPlayerName(bm);
//     }

//     void ShowNameAndStats(bool nameVisible, bool statVisible)
//     {
//         VisualElement playerName = m_TopSideBar.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarPlayerName);

//         if (playerName != null)
//         {
//             playerName.visible = nameVisible;
//         }

//         Label stat = m_TopSideBar.Q<Label>(TopSideBarComponent.IDNames.TopSideBarStat);
//         if (stat != null)
//         {
//             stat.visible = statVisible;
//         }

//     }

//     void UpdateHealthBar()
//     {
//         ShowNameAndStats(m_ShowPlayerName, m_ShowStat);
//     }
// }
