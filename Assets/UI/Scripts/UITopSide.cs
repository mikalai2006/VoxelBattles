using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UITopSide : MonoBehaviour
{
    GameManager _gameManager => GameManager.Instance;
    LevelManager _levelManager;
    [SerializeField] private GameObject _objectSpawnBonuses;
    [SerializeField] private RectTransform progressHP;
    [SerializeField] public TMPro.TextMeshProUGUI textName;
    [SerializeField] private Image rankImage;
    [SerializeField] private Image gerbImage;
    private VehicleAssembler target;
    public VehicleAssembler Target => target;
    //private float maxWidth = 280;
    // public Button fireButton;
    public Button changeCamerButton;

    [SerializeField] private InputActionReference pressSystem;
    //[SerializeField] private SerializedDictionary<TypeBonus, BonusLayoutItem> gameObjectsBonus;
    [SerializeField] public RectTransform CrossObjectTransform;
    public CrossHair crossHair;

    void Awake()
    {
        _levelManager = GameObject.FindGameObjectWithTag("LevelManager")?.GetComponent<LevelManager>();

        // BaseMachine.OnChangeHPs += OnChangeData;
    }

    // void OnDestroy()
    // {
    //     BaseMachine.OnChangeHPs -= OnChangeData;
    // }

    void Start()
    {
        // fireButton.onClick.AddListener(OnClickBtnFire);
        // pressSystem.action.performed += (InputAction.CallbackContext c) =>
        // {
        //     OnClickBtnFire();

        // };

        // changeCamerButton.onClick.AddListener(OnClickBtnChangeCamera);
        // pressSystem.action.performed += (InputAction.CallbackContext c) =>
        // {
        //     OnClickBtnFire();

        // };
    }

    public void OnChangeCrossPosition(Vector3 delta)
    {
        CrossObjectTransform.localPosition = CrossObjectTransform.localPosition + 10 * new Vector3(delta.x, delta.z, 0);
    }

    public void OnSetCrossPosition(Vector3 position)
    {
        CrossObjectTransform.position = position;
    }

    private void OnClickBtnChangeCamera()
    {
        //if (target != null)
        //{
        //    CameraFollowFPS cfFPS = target.GetComponent<CameraFollowFPS>();
        //    CameraFollow cf = target.GetComponent<CameraFollow>();
        //    Camera mainCamera = _levelManager.Camera;
        //    if (cfFPS.enabled == true)
        //    {
        //        cfFPS.enabled = false;
        //        cf.enabled = true;
        //        // _levelManager.OnSetActiveCamera(mainCamera);
        //        target.Camera.gameObject.SetActive(false);
        //        mainCamera.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        cfFPS.enabled = true;
        //        cf.enabled = false;
        //        // _levelManager.OnSetActiveCamera(target.Camera);
        //        target.Camera.gameObject.SetActive(true);
        //        mainCamera.gameObject.SetActive(false);
        //    }
        //}
    }

    //void Update()
    //{
    //    if (target != null)
    //    {
    //        // for (int i = 0; i < Target.Data.bonuses.Count; i++)
    //        // {
    //        //     Target.Data.bonuses.ElementAt(i).Value.time -= Time.deltaTime;

    //        //     if (Target.Data.bonuses.ElementAt(i).Value.time <= 0)
    //        //     {
    //        //         TypeBonus key = Target.Data.bonuses.ElementAt(i).Key;
    //        //         Destroy(gameObjectsBonus[key].gameObject);
    //        //         Target.Data.bonuses.Remove(key);
    //        //         gameObjectsBonus.Remove(key);
    //        //     }
    //        // }
    //    }
    //    else
    //    {
    //        // TODO
    //        BaseMachine bm = _levelManager.machines.Find(m => !m.MachineLevelData.isBot);
    //        if (bm != null)
    //        {
    //            OnSetTarget(bm);
    //        }
    //    }
    //}

    //public void OnCreateUIBonus(GameBonus configBonus)
    //{
    //    if (!gameObjectsBonus.ContainsKey(configBonus.typeBonus))
    //    {
    //        var obj = Instantiate(configBonus.prefabUI, _objectSpawnBonuses.transform);
    //        obj.Init(configBonus, target);
    //        gameObjectsBonus.Add(configBonus.typeBonus, obj);
    //    }
    //}

    //public void OnRemoveUIBonus(TypeBonus key)
    //{
    //    Destroy(gameObjectsBonus[key].gameObject);
    //    gameObjectsBonus.Remove(key);
    //}

    public void OnSetTarget(VehicleAssembler bm)
    {
        //target = bm;

        //Init(bm.MachineLevelData);
    }

    public void Init(MachineLevelData _machineLevelData)
    {
        textName.text = _machineLevelData.name;

        var configRank = _gameManager.Settings.ranks.Find(r => r.name.ToString() == _machineLevelData.rank.ToString());

        rankImage.sprite = configRank.sprite;

        // установка герба.
        Sprite gerb = _gameManager.Settings.gerbs.Find(l => l.name == _machineLevelData.gerbId);
        if (gerb)
        {
            gerbImage.sprite = gerb;
        }
    }

    // public void OnChangeData(BaseMachine machine)
    // {
    //     var oneProcentHP = maxWidth / 1; //machine.Config.hp;
    //     progressHP.sizeDelta = new Vector2(oneProcentHP * (1 - machine.Data.ContainerData.levelDestruction), progressHP.sizeDelta.y);
    // }

    public void OnToStartMenu()
    {

        if (_levelManager == null)
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


}
