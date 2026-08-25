using UIToolkitLibrary;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

public class UIGameViewController: MonoBehaviour
{
    GameManager _gameManager => GameManager.Instance;
    [SerializeField] private LevelManager levelManager;
    public UIGameView UIGameView;
    //[SerializeField] private InfoBoxRowComponentPool poolInfoRows;
    [SerializeField] private HPProgressController _HPProgressController;
    public HPProgressController HPProgressController => _HPProgressController;
    [SerializeField] private WeaponProgressController _weaponProgressController;
    public WeaponProgressController WeaponProgressController => _weaponProgressController;
    [SerializeField] private PricelController _pricelController;
    public PricelController PricelController => _pricelController;
    [SerializeField] private VLineController _vlineController;
    public VLineController VLineController => _vlineController;
    public VehicleAssembler machinePlayer;


    void Start()
    {
        WeaponProgressController.Init(this);

        HPProgressController.Init(this);

        PricelController.Init(this);

        VLineController.Init(this);

        var panelPos = RuntimePanelUtils.ScreenToPanel(UIGameView.m_UIDoc.rootVisualElement.panel, new Vector2(Screen.width / 2f, Screen.height - (Screen.height * 0.7f)));

        UIGameView.m_ScreenElements.style.translate = new StyleTranslate(new Translate(panelPos.x, panelPos.y));

        GameSceneEvents.AddInfoDamage += AddInfoItem;

        UIGameView.m_ButtonExit.clickable.clicked += OnToStartMenu;
    }

    void OnDestroy()
    {
        GameSceneEvents.AddInfoDamage -= AddInfoItem;
    }

    void OnDisable()
    {
        UIGameView.m_ButtonExit.clickable.clicked -= OnToStartMenu;
    }

    private void OnToStartMenu()
    {
        if (levelManager == null)
        {
            // CloseSettings();
        }
        else
        {
            AudioManager.Instance.Click();

            _gameManager.ChangeState(GameState.CloseLevel);

            // var dashBoard = new StartUIOperation();
            // dashBoard.ShowAndHide().Forget();

            var uiManager = new UIManagerOperation();
            uiManager.ShowAndHide().Forget();
        }
    }

    
    public void AddInfoItem(AppInfoDamageData data)
    {
        //if (UIGameView == null)
        //    return;
        
        //// messagesInfo.Add(data);

        //InfoBoxRowComponent el = poolInfoRows.GetObject(); // new InfoBoxRow{ name = "InfoBoxRow"};
        //// el.style.flexGrow = 1;
        //// el.style.flexDirection = FlexDirection.Row;
        //// Color color = gameManager.Theme.bgColor;
        //// color.a = 0.3f;
        //// el.style.backgroundColor = new StyleColor(color);

        //el.Init(this, data);

        //// var infoEl = new VisualElement {name="infoItem"};
        //// infoEl.Add(new Label
        //// {
        ////     text = "text",
        //// });
        
        //UIGameView.m_InfoBox.Add(el);
    }
    
    public void RemoveInfoItem(InfoBoxRowComponent el)
    {
        //UIGameView.m_InfoBox.Remove(el);

        //poolInfoRows.ReturnObject(el);
    }

    public void SetTarget(VehicleAssembler obj)
    {
        machinePlayer = obj;
        PricelController.SetTarget(obj);
        VLineController.SetTarget(obj);
    }

    /// <summary>
    /// Выводит дистанцию рядом с информационным прицелом.
    /// </summary>
    /// <param name="distance">Дистанция в units</param>
    public void SetDistance(float distance)
    {
        PricelController.SetDistance(distance);
    }
}