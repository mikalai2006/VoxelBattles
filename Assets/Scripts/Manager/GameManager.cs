using System;
using UnityEngine;
using User;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Collections.Generic;
using Loader;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using System.IO;

public class GameManager : StaticInstance<GameManager>
{
  public static event Action OnChangeTheme;
  public static event Action<GameState> OnBeforeStateChanged;
  public static event Action<GameState> OnAfterStateChanged;
  
  [SerializeField] private InputActionAsset inputAction;
  [SerializeField] private UnityEngine.UI.Image _imageBg;
  [SerializeField] private UnityEngine.UI.Image _imageWrapper;
  [SerializeField] private UnityEngine.UI.Image _imageShadow;
  [SerializeField] private string namePlayPref;
  // private string langCodePlayPref;
  // public string KeyPlayPref => string.Format("{0}_{1}", namePlayPref, langCodePlayPref);
  public AppInfoContainer AppInfo { get; private set; }
  public GameSetting Settings;
  public GameLevel LevelConfig;
  public Camera ActiveCamera { get; private set; }
  public GameTheme Theme { get; private set; }
  public AudioManager audioManager { get; private set; }
  public DataManager DataManager { get; private set; }
//   // [field: SerializeField]
  public StateManager StateManager;// { get; private set; }
  public SceneInstance currentScene;

  public LoaderBarProvider LoaderBarProvider { get; private set; }
  public InitUserProvider InitUserProvider { get; private set; }
  public AssetProvider AssetProvider { get; private set; }
  public GameState State { get; private set; }
//   public AdManager AdManager;
  public ResourceSystem ResourceSystem { get; internal set; }
  //public ECSManager ECSManager;

    //   public LevelManager LevelManager { get; private set; }
    //   [HideInInspector] public GamePlayerSetting PlayerSetting { get; private set; }
    //   [HideInInspector] public GameType GameType { get; private set; }

    // public SceneInstance environment { get; private set; }
    //   public InputManager InputManager { get; private set; }

    // [SerializeField] private ProgressManager _progressManager;
    // public ProgressManager Progress => _progressManager;


    // void OnApplicationFocus(bool hasFocus)
    // {
    //   Debug.Log($"OnApplicationFocus {hasFocus}");
    //   if (hasFocus)
    //   {
    //     Time.timeScale = 1;
    //   }
    //   else
    //   {
    //     Time.timeScale = 0;
    //   }
    // }

    // void OnApplicationPause(bool pauseStatus)
    // {
    //   Debug.Log($"OnApplicationPause {pauseStatus}");

    //   if (pauseStatus)
    //   {
    //     Time.timeScale = 0;
    //   }
    //   else
    //   {
    //     Time.timeScale = 1;
    //   }
    // }


    protected override void Awake()
  {
    LoaderBarProvider = new LoaderBarProvider();

    ResourceSystem = ResourceSystem.Instance;

    AppInfo = new AppInfoContainer();

    InitUserProvider = new InitUserProvider();

    AssetProvider = new AssetProvider();

    DataManager = DataManager.Instance;

    audioManager = AudioManager.Instance;

    base.Awake();

    Theme = Settings.ThemeDefault;

    ChangeTheme();

    inputAction.Enable();
  }

  // private void OnDestroy()
  // {
  //   DataManager.OnLoadData -= InitPlayerInfo;
  // }

  void Start()
  {
#if UNITY_EDITOR
    Debug.unityLogger.logEnabled = true;
#else
    Debug.unityLogger.logEnabled = false;
#endif
    // Application.targetFrameRate = 60;
    // ChangeState(GameState.StartApp);

    // InputManager = new();

    
    if (File.Exists(Settings.pathFileColors))
    {
        string jsonContent = File.ReadAllText(Settings.pathFileColors);

        RootObjectColorsItemFromJSON data = JsonUtility.FromJson<RootObjectColorsItemFromJSON>(jsonContent);

        Debug.Log("Parse colors: " + data.colors.Length);

        List<ColorModifyItem> newColorsForModify = new List<ColorModifyItem>();

        foreach (var color in data.colors)
        {
          Color colorRGBA;
          if (ColorUtility.TryParseHtmlString(color.name, out colorRGBA))
          {
              // Debug.Log("Parsed RGBA Color: " + colorRGBA.ToString()); 
              Color32 color32 = colorRGBA;
              
              newColorsForModify.Add(new ColorModifyItem
              {
                color = color32,
                cost = 0,
                rank = 0
              });
          }
          else
          {
              Debug.LogError("Failed to parse RGBA hex string.");
          }
          Settings.colorsModify = newColorsForModify;
        }
    }
    else
    {
        Debug.LogError("Not found colors at path: " + Settings.pathFileColors);
    }
  }

    void Update()
    {
      Debug.unityLogger.logEnabled = Settings.DebugSettings.logEnabled;
    }

  public void ChangeState(GameState newState, object Params = null)
  {
    OnBeforeStateChanged?.Invoke(newState);
    Debug.Log($"GAME state => {newState}");
    State = newState;
    switch (newState)
    {
      case GameState.StartApp:
        HandleStartApp();
        break;
      case GameState.CreateGame:
        HandleCreateGame();
        break;
      case GameState.RunLevel:
        HandleRunLevel();
        break;
      case GameState.CloseLevel:
      case GameState.ChangeMenuItem:
        HandleCloseLevel();
        break;
      case GameState.StopDrag:
        break;
      case GameState.StartDrag:
        break;
      case GameState.ShowMenu:
        break;
      case GameState.LevelGameOver:
        break;
      case GameState.StartPause:
        Time.timeScale = 0;
        AudioListener.pause = true;
        break;
      case GameState.StopPause:
        Time.timeScale = 1;
        AudioListener.pause = false;
        break;
      case GameState.LoadLevel:
        // HandleLoadLevel();
        break;
      default:
        // throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        break;
    }

    OnAfterStateChanged?.Invoke(newState);
  }

  private void HandleStartApp()
  {
    ChangeTheme();

  }

  public void SetDefaultState()
  {
    var appInfo = new AppInfoContainer();
  }

  private void ChangeTheme()
  {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = Theme.bgColor;
        }
    // _imageBg.color = Theme.bgColor;
    // _imageShadow.color = Theme.colorBgGrid;
    // _imageWrapper.sprite = Theme.bgImage;
  }

  private async void HandleRunLevel()
  {
    if (currentScene.Scene.IsValid())
    {
      await AssetProvider.UnloadAdditiveScene(currentScene);
    }
  }
  private async void HandleCloseLevel()
  {
    // LevelManager = null;
    if (currentScene.Scene.IsValid())
    {
      await AssetProvider.UnloadAdditiveScene(currentScene);
    }
  }
  private async void HandleCreateGame()
  {
    var operations = new Queue<ILoadingOperation>();
    operations.Enqueue(new GameInitOperation());
    await LoaderBarProvider.LoadAndDestroy(operations);
    // LevelManager.CreateLevel();
  }
  // private async void HandleLoadLevel()
  // {
  //   // var operations = new Queue<ILoadingOperation>();
  //   // operations.Enqueue(new GameInitOperation());
  //   // await LoadingScreenProvider.LoadAndDestroy(operations);
  //   // //var dataGame = DataManager.Load();
  //   // // LevelManager.LoadLevel();

  // }


  public void SetTheme(GameTheme newTheme)
  {
    if (newTheme == null) newTheme = Settings.ThemeDefault;

    if (AppInfo != null && AppInfo.setting != null)
    {
      AppInfo.setting.theme = newTheme.name;

      Theme = newTheme;

      ChangeTheme();

      // DataManager.Save();

      OnChangeTheme?.Invoke();
    }
  }

//   public void InitGameGrid(LevelManager levelManager, SceneInstance environment)
//   {
//     // this.StateManager = new StateManager();
//     this.LevelManager = levelManager;
//     this.environment = environment;
//     LevelManager.Init().Forget();
//   }

  public async UniTask SetAppInfo(AppInfoContainer dataInfo)
  {
    AppInfo = dataInfo;


    // Locale needSetLocale = LocalizationSettings.AvailableLocales.Locales.Find(t => t.Identifier.Code == AppInfo.setting.lang);
    // if (AppInfo.setting.lang != LocalizationSettings.SelectedLocale.Identifier.Code)
    // {
    //   LocalizationSettings.SelectedLocale = needSetLocale;
    // }

    // Set theme.
    List<GameTheme> allThemes = ResourceSystem.GetAllTheme();
    GameTheme userTheme = allThemes.Where(t => t.name == AppInfo.setting.theme).FirstOrDefault();
    SetTheme(userTheme);

    AppInfo.SaveSettings();

    AudioManager.Instance.EffectSource.volume = dataInfo.setting.auv;
    AudioManager.Instance.MusicSource.volume = dataInfo.setting.muv;

    await UniTask.Yield();
  }

  // public void SetLangCodePlayPref(string code)
  // {
  //   langCodePlayPref = code;
  //   Debug.Log($"Init key playpref as {code}");
  // }

//   public void SetPlayerSetting(GamePlayerSetting playerSetting)
//   {
//     Debug.Log($"Set player info {playerSetting.idPlayerSetting}");
//     PlayerSetting = playerSetting;



//     // Progress.Refresh();

//     // Debug.LogWarning(Progress.ToString());
//   }

  public void SetLevelConfig(GameLevel obj)
  {
    LevelConfig = obj;
  }
  public void SetActiveCamera(Camera cam)
  {
    ActiveCamera = cam;
  }
}

[Serializable]
public enum GameState
{
  StartApp = 0,
  CreateGame = 1,
  StartDrag = 2,
  StopDrag = 3,
  ShowMenu = 4,
  RunLevel = 5,
  CloseLevel = 6,
  LoadLevel = 7,
  LevelGameOver = 8,
  LevelReady = 9,
  LevelComplete = 10,
  StartPause = 11,
  StopPause = 12,
  ShowThroph = 13,
  CloseThroph = 14,
  ChangeMenuItem = 15
}