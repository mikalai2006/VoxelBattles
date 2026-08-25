using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Loader;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

public class UIDashboard : UILocaleBase
{
  // [DllImport("__Internal")]
  // private static extern void GetLeaderBoard();
  [SerializeField] private UIDocument _uiDoc;
  [SerializeField] private VisualTreeAsset UserInfoDoc;
  [SerializeField] private VisualTreeAsset LeaderDoc;
  private VisualElement _root;
  private Button _exitButton;
  private Button _speedButton;
  private Button _arcadaButton;
  private Button _classicButton;
  private Button _settingsButton;
  // private Button _newGameButton;
  private VisualElement _userInfoBlok;
  private Label _userName;
  private Label _userRate;
  // private VisualElement _leaderBoard;
  // private Button _loadGameMenuButton;
  private TaskCompletionSource<DataDialogResult> _processCompletionSource;
  // private DataDialogResult _result;

  private void Awake()
  {
    UISettings.OnChangeLocale += RefreshMenu;
    GameManager.OnChangeTheme += RefreshMenu;
    StateManager.OnChangeState += SetValue;
    // DataManager.OnLoadLeaderBoard += DrawLeaderListBlok;
  }

  private void OnDestroy()
  {
    UISettings.OnChangeLocale -= RefreshMenu;
    GameManager.OnChangeTheme -= RefreshMenu;
    StateManager.OnChangeState -= SetValue;
    // DataManager.OnLoadLeaderBoard -= DrawLeaderListBlok;
  }

  public virtual void Start()
  {
    _root = _uiDoc.rootVisualElement.Q<VisualElement>("DashBoard");
    _userInfoBlok = _uiDoc.rootVisualElement.Q<VisualElement>("UserInfoBlok");
    _userName = _root.Q<Label>("UserName");
    // // _leaderBoard = _uiDoc.rootVisualElement.Q<VisualElement>("LeaderBoard");
    // // _leaderBoard.style.display = DisplayStyle.None;
    // _classicButton = _root.Q<Button>("BtnClassicGame");
    // _classicButton.clickable.clicked += () =>
    // {
    //   ClickClassicGame();
    // };

    // _arcadaButton = _root.Q<Button>("BtnArcadaGame");
    // _arcadaButton.clickable.clicked += () =>
    // {
    //   ClickArcadaGame();
    // };
    // _arcadaButton.style.display = DisplayStyle.None;


    // _speedButton = _root.Q<Button>("BtnSpeedGame");
    // _speedButton.clickable.clicked += () =>
    // {
    //   ClickSpeedGame();
    // };
    // _settingsButton = _root.Q<Button>("BtnSettings");
    // _settingsButton.clickable.clicked += () =>
    // {
    //   ShowSettings();
    // };
    // _exitButton = _root.Q<Button>("ExitBtn");
    // _exitButton.clickable.clicked += () =>
    // {
    //   ClickExitButton();
    // };


    // #if UNITY_EDITOR
    //     _gameManager.DataManager.GetLeaderBoard("{\"leaderboard\":{\"title\":[{\"lang\":\"ru\",\"value\":\"Лидеры по количеству слов\"}]},\"userRank\":25,\"entries\":[{\"rank\":24,\"score\":90,\"name\":\"Tamara Ivanovna Semenovatoreva\",\"lang\":\"ru\",\"photo\":\"\"},{\"rank\":25,\"score\":80,\"name\":\"Mikalai P.2\",\"lang\":\"ru\",\"photo\":\"https://games-sdk.yandex.ru/games/api/sdk/v1/player/avatar/66VOVRVF2GJAXS5VWT3X54YATTEZAJLGXTPIXJTG3465T5HXLNQFMZIOJ7WYALX2PEC2DIAHLM6FC7ABRLOA27IRF55DP6DXJU7JDS4IFW63KJWT4IFLT2I26N44GVCAAX6FGHPPVKQY65KZZOXXYODUUKJMK2Y25M2VUDFYRPJDR3TS4JVBUOZNWFE2QNABMFRQEVLJRRIODYNB2JKIIK76YMZEEA3VQHV3M6Q=/islands-retina-medium\"},{\"rank\":26,\"score\":70,\"name\":\"Mikalai P.3\",\"lang\":\"ru\",\"photo\":\"https://games-sdk.yandex.ru/games/api/sdk/v1/player/avatar/66VOVRVF2GJAXS5VWT3X54YATTEZAJLGXTPIXJTG3465T5HXLNQFMZIOJ7WYALX2PEC2DIAHLM6FC7ABRLOA27IRF55DP6DXJU7JDS4IFW63KJWT4IFLT2I26N44GVCAAX6FGHPPVKQY65KZZOXXYODUUKJMK2Y25M2VUDFYRPJDR3TS4JVBUOZNWFE2QNABMFRQEVLJRRIODYNB2JKIIK76YMZEEA3VQHV3M6Q=/islands-retina-medium\"}]}");
    // #endif
    // #if ysdk
    //     GetLeaderBoard();
    // #endif
    //     // await DrawLeaderListBlok();

    RefreshMenu();

    // var diffDate = string.IsNullOrEmpty(_gameManager.StateManager.stateGame.lastTime)
    //   ? (DateTime.Now - DateTime.Now)
    //   : (DateTime.Now - DateTime.Parse(_gameManager.StateManager.stateGame.lastTime));
    // // Debug.Log($"diffDate={diffDate}|||[{DateTime.Now}:::::{_gameManager.StateManager.stateGame.lastTime}]");
    // if (string.IsNullOrEmpty(_gameManager.StateManager.stateGame.lastTime) || diffDate.TotalHours > _gameSetting.countHoursDailyGift)
    // {
    //   ShowDailyDialog();
    // }

    // base.Initialize(_uiDoc.rootVisualElement);
  }

  private void SetValue(StatePlayer state)
  {
    // _userCoin.text = string.Format("{0}", state.coins);
    // _userRate.text = string.Format("{0}", state.rate);

    RefreshMenu();
  }

  public async UniTask<DataDialogResult> ProcessAction()
  {
    // _result = new DataDialogResult();


    // #if ysdk
    //         GetLeaderBoard();
    // #endif


    _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

    return await _processCompletionSource.Task;
  }

  // private async void ShowDailyDialog()
  // {
  //   // var title = await Helpers.GetLocaledString("dailycoins_t");
  //   // var message = await Helpers.GetLocalizedPluralString("dailycoins_d", new Dictionary<string, object>() {
  //   //   { "count", _gameSetting.countHoursDailyGift },
  //   // });
  //   // var configCoin = _gameManager.ResourceSystem.GetAllEntity().Find(t => t.typeEntity == TypeEntity.Coin);
  //   // var configStar = _gameManager.ResourceSystem.GetAllEntity().Find(t => t.typeEntity == TypeEntity.RandomLetter);
  //   // var entities = new List<ShopItem<GameEntity>>() {
  //   //       new ShopItem<GameEntity>(){
  //   //         entity = configCoin,
  //   //         cost = 0,
  //   //         count = 500
  //   //       },
  //   //       new ShopItem<GameEntity>(){
  //   //         entity = configStar,
  //   //         cost = 0,
  //   //         count = 3
  //   //       }
  //   //     };

  //   // var dialog = new DialogProvider(new DataDialog()
  //   // {
  //   //   title = title,
  //   //   message = message,
  //   //   entities = entities,
  //   //   showCancelButton = false,
  //   // });

  //   // _gameManager.InputManager.Disable();
  //   // var result = await dialog.ShowAndHide();
  //   // if (result.isOk)
  //   // {
  //   //   foreach (var entityItem in entities)
  //   //   {
  //   //     if (entityItem.entity.typeEntity == TypeEntity.Coin)
  //   //     {
  //   //       _gameManager.StateManager.IncrementTotalCoin(entityItem.count);
  //   //     }
  //   //     else
  //   //     {
  //   //       _gameManager.StateManager.BuyHint(entityItem);
  //   //     }
  //   //   }
  //   //   _gameManager.StateManager.SetLastTime();
  //   // }
  //   // _gameManager.InputManager.Enable();
  // }

  private async void RefreshMenu()
  {

    await DrawUserInfoBlok();

    // #if ysdk && !UNITY_EDITOR
    //     await DrawLeaderListBlok();
    // #endif

    base.Initialize(_uiDoc.rootVisualElement);
  }

  private async UniTask DrawUserInfoBlok()
  {
    await LocalizationSettings.InitializationOperation.Task;

    // var stateGame = _gameManager.StateManager.stateGame;
    // var dataGame = _gameManager.StateManager.dataGame;
    // if (string.IsNullOrEmpty(dataGame.rank)) return;


    // _root.Q<Label>("AppNameLabel").style.color = _gameManager.Theme.colorAccent;


    // var blok = UserInfoDoc.Instantiate();

    // blok.Q<VisualElement>("Blok").style.backgroundColor = _gameManager.Theme.colorBgDialog;

    // var progressBlok = blok.Q<VisualElement>("ProgressBlok");

    var AppInfo = _gameManager.AppInfo;
    if (AppInfo != null)
    {

      _userName.text = AppInfo.UserInfo.name;
    }
    // // _gameManager.Progress.Refresh();

    // // var nameFile = blok.Q<Label>("NameFile");
    // // var titleFile = _gameManager.GameSettings.wordFiles.Find(t => t.locale.Identifier.Code == LocalizationSettings.SelectedLocale.Identifier.Code);
    // // nameFile.text = await Helpers.GetLocaledString(titleFile.text.title);


    // var allPlayerSetting = _gameSetting.PlayerSettings.OrderBy(t => -t.countFindWordsForUp).First();

    // if (_gameManager.StateManager.stateGame.rate >= allPlayerSetting.countFindWordsForUp)
    // {
    //   progressBlok.Clear();
    //   Label newLabel = new Label()
    //   {
    //     text = await Helpers.GetLocaledString("endheadprogress"),
    //   };
    //   newLabel.AddToClassList("text-primary");
    //   newLabel.AddToClassList("text-wrap");
    //   newLabel.AddToClassList("text-xs");
    //   progressBlok.Add(newLabel);
    // }
    // else
    // {

    //   var progress = blok.Q<VisualElement>("ProgressBar");
    //   progress.style.backgroundColor = new StyleColor(_gameManager.Theme.colorAccent);

    //   var status = blok.Q<Label>("Status");
    //   var foundWords = blok.Q<Label>("FoundWords");

    //   var percentFindWords = _gameManager.Progress.percent;
    //   progress.style.width = new StyleLength(new Length(percentFindWords, LengthUnit.Percent));

    //   string nameStatus = await Helpers.GetLocaledString(playerSetting.text.title);
    //   status.text = nameStatus;

    //   var textCountWords = await Helpers.GetLocalizedPluralString(
    //         "foundwords_dialog",
    //          new Dictionary<string, object> {
    //         {"name", nameStatus},
    //         {"count",  _gameManager.Progress.countNeedOpenWords},
    //         {"procent",  percentFindWords},
    //         }
    //       );
    //   foundWords.text = string.Format("{0}", textCountWords);

    //   // blok.Q<Label>("Rate").text = string.Format("{0}", stateGame.rate);
    //   // blok.Q<VisualElement>("RateImg").style.backgroundImage = new StyleBackground(_gameSettings.spriteRate);

    //   // var textCoin = await Helpers.GetLocalizedPluralString(
    //   //       "coin",
    //   //        new Dictionary<string, object> {
    //   //         {"count",  stateGame.coins},
    //   //       }
    //   //     );
    //   // blok.Q<Label>("Coin").text = string.Format("{0} <size=12>{1}</size>", dataGame.activeLevel.coins, textCoin);
    //   // var configCoin = _gameManager.ResourceSystem.GetAllEntity().Find(t => t.typeEntity == TypeEntity.Coin);
    //   // blok.Q<VisualElement>("CoinImg").style.backgroundImage = new StyleBackground(configCoin.sprite);

    // }


    // var _newGameButton = blok.Q<Button>("NewGameBtn");
    // _newGameButton.clickable.clicked += () =>
    // {
    //   ClickLoadGameButton();
    // };
    // _newGameButton.style.display = DisplayStyle.None;

    // var _loadGameMenuButton = blok.Q<Button>("LoadGameBtn");
    // _loadGameMenuButton.clickable.clicked += () =>
    // {
    //   ClickLoadGameButton();
    // };
    // _loadGameMenuButton.style.display = DisplayStyle.None;

    // // _userInfoBlok.style.display = DisplayStyle.None;
    // if (_gameManager.StateManager.dataGame.completed.Count == 0 && _gameManager.StateManager.dataGame.levels.Count == 0)
    // {
    //   _newGameButton.style.display = DisplayStyle.Flex;
    // }
    // else
    // // if (_gameManager.StateManager.dataGame.levels.Count != 0 || _gameManager.StateManager.dataGame.sl.Count != 0)
    // {
    //   _loadGameMenuButton.style.display = DisplayStyle.Flex;
    //   // _userInfoBlok.style.display = DisplayStyle.Flex;
    // }


    // // Set short info user.
    // var configCoin = _gameManager.ResourceSystem.GetAllEntity().Find(t => t.typeEntity == TypeEntity.Coin);

    // _userCoin = blok.Q<Label>("UserCoin");
    // _userCoin.text = _gameManager.StateManager.stateGame.coins.ToString();
    // var userCoinImg = blok.Q<VisualElement>("UserCoinImg");
    // userCoinImg.style.backgroundImage = new StyleBackground(configCoin.sprite);

    // _userRate = blok.Q<Label>("UserRate");
    // _userRate.text = _gameManager.StateManager.stateGame.rate.ToString();
    // var userRateImg = blok.Q<VisualElement>("UserRateImg");
    // userRateImg.style.backgroundImage = new StyleBackground(_gameSetting.spriteRate);

    // var userName = blok.Q<Label>("UserName");
    // userName.text = await Helpers.GetName();

    // userCoinImg.style.unityBackgroundImageTintColor = _gameManager.Theme.colorSecondary;
    // userRateImg.style.unityBackgroundImageTintColor = _gameManager.Theme.colorSecondary;

    // // load avatar
    // string placeholder = _gameManager.AppInfo.UserInfo.photo;
    // var imgAva = blok.Q<VisualElement>("Ava");
    // Texture2D avatarTexture = await Helpers.LoadTexture(placeholder);
    // if (avatarTexture != null)
    // {
    //   imgAva.style.backgroundImage = new StyleBackground(avatarTexture);
    // }
    // else
    // {
    //   imgAva.style.backgroundImage = new StyleBackground(_gameSetting.spriteUser);
    //   imgAva.style.unityBackgroundImageTintColor = _gameManager.Theme.colorSecondary;
    // }

    // _userInfoBlok.Clear();
    // _userInfoBlok.Add(blok);
    // base.Initialize(_userInfoBlok);

    // // nameFile.style.color = new StyleColor(_gameManager.Theme.colorAccent);
  }


  private async void ClickLoadGameButton()
  {
    AudioManager.Instance.Click();

    await LocalizationSettings.InitializationOperation.Task;

    // StateManager.OnChangeState -= SetValue;

    // _result.isOk = true;

    // _processCompletionSource.SetResult(_result);

    // var operations = new Queue<ILoadingOperation>();
    // operations.Enqueue(new GameInitOperation());
    // await _gameManager.LoadingScreenProvider.LoadAndDestroy(operations);


    // var activeLastWord = _gameManager.StateManager.dataGame.lastWord;

    // if (string.IsNullOrEmpty(activeLastWord))
    // {
    //   GameLevel firstLevel = _gameManager.GameSettings.GameLevels.Where(t => t.locale.Identifier.Code == LocalizationSettings.SelectedLocale.Identifier.Code).OrderBy(t => t.minRate).FirstOrDefault();

    //   activeLastWord = firstLevel != null && firstLevel.levelWords.Count > 0 ? firstLevel.levelWords.ElementAt(0) : "";
    // }

    // _gameManager.ChangeState(GameState.LoadLevel);

    // _gameManager.LevelManager.InitLevel(activeLastWord).Forget();

    // // var activeLastWord = _gameManager.StateManager.dataGame.lastWord;

    // // _gameManager.LevelManager.InitLevel(activeLastWord);

    // // _gameManager.ChangeState(GameState.LoadLevel);

  }

  // private async void ClickNewGameButton()
  // {
  //   AudioManager.Instance.Click();

  //   StateManager.OnChangeState -= SetValue;

  //   await LocalizationSettings.InitializationOperation.Task;

  //   var operations = new Queue<ILoadingOperation>();
  //   operations.Enqueue(new GameInitOperation());
  //   await _gameManager.LoadingScreenProvider.LoadAndDestroy(operations);

  //   // GameLevel firstLevel = _gameSetting.GameLevels.Where(t => t.locale.Identifier.Code == LocalizationSettings.SelectedLocale.Identifier.Code).OrderBy(t => t.minRate).FirstOrDefault();
  //   // string activeLastLevelWord = "";
  //   // if (firstLevel != null)
  //   // {
  //   //   activeLastLevelWord = firstLevel.levelWords.ElementAt(0);
  //   // }

  //   // _gameManager.LevelManager.InitLevel(activeLastLevelWord);

  //   // _gameManager.ChangeState(GameState.LoadLevel);

  //   _result.isOk = true;

  //   _processCompletionSource.SetResult(_result);
  // }

  // private async void ClickClassicGame()
  // {
  //   AudioManager.Instance.Click();

  //   var gameType = _gameManager.Settings.GamesTypes.Find(t => t.typeGame == TypeGame.Classic);
  //   if (gameType == null)
  //   {
  //     _result.isOk = false;
  //     _processCompletionSource.SetResult(_result);
  //     return;
  //   }

  //   _gameManager.SetGameType(gameType);

  //   var dialog = new UINewGameOperation();
  //   _result = await dialog.ShowAndHide();
  //   if (_result.isOk)
  //   {
  //     _gameManager.StateManager = new StateManager();
  //     _gameManager.StateManager.Init(null);
  //     if (_result.isNew)
  //     {
  //       _gameManager.StateManager.CreateDataNewGame(_gameManager.GameType.typeGame);
  //     }
  //     else if (_result.isLoad)
  //     {
  //       _gameManager.StateManager.CreateDataLoadGame(_gameManager.GameType.typeGame);
  //     }

  //     _processCompletionSource.SetResult(_result);

  //     var operations = new Queue<ILoadingOperation>();
  //     operations.Enqueue(new GameInitOperation());
  //     await _gameManager.LoaderBarProvider.LoadAndDestroy(operations);
  //   }
  // }

  // private async void ClickArcadaGame()
  // {
  //   AudioManager.Instance.Click();

  //   var gameType = _gameManager.Settings.GamesTypes.Find(t => t.typeGame == TypeGame.Arcada);
  //   if (gameType == null)
  //   {
  //     _result.isOk = false;
  //     _processCompletionSource.SetResult(_result);
  //     return;
  //   }

  //   _gameManager.SetGameType(gameType);

  //   var dialog = new UINewGameOperation();
  //   _result = await dialog.ShowAndHide();
  //   if (_result.isOk)
  //   {
  //     _gameManager.StateManager = new StateManager();
  //     _gameManager.StateManager.Init(null);
  //     if (_result.isNew)
  //     {
  //       _gameManager.StateManager.CreateDataNewGame(_gameManager.GameType.typeGame);
  //     }
  //     else if (_result.isLoad)
  //     {
  //       _gameManager.StateManager.CreateDataLoadGame(_gameManager.GameType.typeGame);
  //     }

  //     _processCompletionSource.SetResult(_result);

  //     var operations = new Queue<ILoadingOperation>();
  //     operations.Enqueue(new GameInitOperation());
  //     await _gameManager.LoaderBarProvider.LoadAndDestroy(operations);
  //   }
  // }


  // private async void ClickSpeedGame()
  // {
  //   AudioManager.Instance.Click();

  //   var gameType = _gameManager.Settings.GamesTypes.Find(t => t.typeGame == TypeGame.Speed);
  //   if (gameType == null)
  //   {
  //     _result.isOk = false;
  //     _processCompletionSource.SetResult(_result);
  //     return;
  //   }

  //   _gameManager.SetGameType(gameType);

  //   var dialog = new UINewGameOperation();
  //   _result = await dialog.ShowAndHide();
  //   if (_result.isOk)
  //   {
  //     _gameManager.StateManager = new StateManager();
  //     _gameManager.StateManager.Init(null);
  //     if (_result.isNew)
  //     {
  //       _gameManager.StateManager.CreateDataNewGame(_gameManager.GameType.typeGame);
  //     }
  //     else if (_result.isLoad)
  //     {
  //       _gameManager.StateManager.CreateDataLoadGame(_gameManager.GameType.typeGame);
  //     }

  //     _processCompletionSource.SetResult(_result);

  //     var operations = new Queue<ILoadingOperation>();
  //     operations.Enqueue(new GameInitOperation());
  //     await _gameManager.LoaderBarProvider.LoadAndDestroy(operations);
  //   }
  // }

  // private void ClickExitButton()
  // {
  //   AudioManager.Instance.Click();
  //   Debug.Log("ClickExitButton");
  // }

  // private async void ShowSettings()
  // {
  //   AudioManager.Instance.Click();

  //   _gameManager.InputManager.Disable();
  //   var settingsDialog = new UISettingsOperation();
  //   await settingsDialog.ShowAndHide();
  //   _gameManager.InputManager.Enable();
  // }

}

// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using Loader;
// using UnityEngine;
// using UnityEngine.UI;

// public class UIDashboard : MonoBehaviour
// {
//   private GameManager _gameManager => GameManager.Instance;
//   [SerializeField] private Button _buttonRun;
//   [SerializeField] private TMPro.TextMeshProUGUI _buttonRunText;
//   private TaskCompletionSource<DataDialogResult> _processCompletionSource;
//   private DataDialogResult _result;

//   private void Awake()
//   {
//     _buttonRun.image.color = _gameManager.Theme.colorBgButton;
//     _buttonRunText.color = _gameManager.Theme.colorTextInput;

//     _buttonRun.onClick.AddListener(StartGame);
//   }

//   private void OnDestroy()
//   {
//     _buttonRun.onClick.RemoveListener(StartGame);
//   }

//   public async UniTask<DataDialogResult> ProcessAction()
//   {
//     _result = new DataDialogResult();

//     _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

//     return await _processCompletionSource.Task;
//   }

//   private async void StartGame()
//   {
//     AudioManager.Instance.Click();
//     _result.isOk = true;

//     _processCompletionSource.SetResult(_result);

//     var operations = new Queue<ILoadingOperation>();
//     operations.Enqueue(new GameInitOperation());
//     await _gameManager.LoaderBarProvider.LoadAndDestroy(operations);

//     Debug.Log("Start game");
//   }
// }
