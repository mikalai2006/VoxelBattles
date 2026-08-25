using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

public class UISettings : UIBase
{
  public static event Action OnChangeLocale;
  // public static event Action OnChangeIsSymbols;
  private VisualElement _languageBlock;
  // private Toggle _isSymbols;
  // private Button _toMenuAppButton;
  private Button _okButton;
  private DropdownField _dropdownLanguage;
  private SliderInt _sliderTimeDelay;
  private Slider _sliderVolumeMusic;
  private Slider _sliderVolumeEffect;
  private DropdownField _dropdownTheme;

  private TaskCompletionSource<DataDialogResult> _processCompletionSource;
  private DataDialogResult _result;

  public override async void Start()
  {
    base.Start();

    Title.text = await Helpers.GetLocaledString("settings");

    _languageBlock = Wrapper.Q<VisualElement>("LanguageBlock");

    var menuBlok = Wrapper.Q<VisualElement>("Menu");
    _dropdownLanguage = menuBlok.Q<DropdownField>("Language");
    _sliderTimeDelay = menuBlok.Q<SliderInt>("TimeDelay");
    _sliderVolumeMusic = menuBlok.Q<Slider>("VolumeMusic");
    _sliderVolumeEffect = menuBlok.Q<Slider>("VolumeEffect");
    _dropdownTheme = menuBlok.Q<DropdownField>("Theme");

    _okButton = Wrapper.Q<Button>("BtnSaveOptions");

    _okButton.clickable.clicked += () =>
    {
      CloseSettings();
    };

    CloseButton.clickable.clicked += () =>
    {
      CloseSettings();
    };

    // _toMenuAppButton = Wrapper.Q<Button>("BtnToStartMenu");
    // _toMenuAppButton.clickable.clicked += () =>
    // {
    //   ClickToStartMenuButton();
    // };

    CreateListSettings();

    ChangeTheme(null);

    base.Initialize(Wrapper);
  }


  public async UniTask<DataDialogResult> ProcessAction()
  {
    _result = new DataDialogResult();

    _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

    return await _processCompletionSource.Task;
  }

  private void ChangeTheme(ChangeEvent<string> evt)
  {
    if (evt != null)
    {
      var allThemes = _gameManager.ResourceSystem.GetAllTheme();
      GameTheme chooseTheme = allThemes.Find(t => t.name == evt.newValue);

      _gameManager.SetTheme(chooseTheme);
      _gameManager.AppInfo.SaveSettings();
    }

    base.Theming(Wrapper);
  }

  // private void ClickToStartMenuButton()
  // {

  //   if (_gameManager.LevelManager == null)
  //   {
  //     CloseSettings();
  //   }
  //   else
  //   {
  //     AudioManager.Instance.Click();

  //     GameManager.Instance.ChangeState(GameState.CloseLevel);


  //     _result.isOk = false;

  //     _processCompletionSource.SetResult(_result);

  //     var dialogDashboard = new UIDashboardOperation();
  //     dialogDashboard.ShowAndHide().Forget();
  //   }

  // }

  // private void ClickOpenMenuButton()
  // {
  //   AudioManager.Instance.Click();
  //   _gameManager.InputManager.Disable();
  //   ShowMenu();
  //   CreateMenu();
  // }

  private async void CreateListSettings()
  {
    await LocalizationSettings.InitializationOperation.Task;

    var userSettings = _gameManager.AppInfo.setting;

    _sliderVolumeEffect.value = userSettings.auv;
    _audioManager.EffectSource.volume = _sliderVolumeEffect.value;
    _sliderVolumeEffect.RegisterValueChangedCallback((ChangeEvent<float> evt) =>
    {
      _audioManager.EffectSource.volume = evt.newValue;
      userSettings.auv = evt.newValue;
      _gameManager.AppInfo.SaveSettings();
    });

      string nameCurrent = LocalizationSettings.SelectedLocale.Identifier.CultureInfo.NativeName;
      string nameCurrentCapitalize = char.ToUpper(nameCurrent[0]) + nameCurrent.Substring(1);
      _dropdownLanguage.value = nameCurrentCapitalize;
      _dropdownLanguage.choices.Clear();
      for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
      {
        Locale locale = LocalizationSettings.AvailableLocales.Locales[i];
        string nameL = locale.Identifier.CultureInfo.NativeName;
        string nameLCapitalize = char.ToUpper(nameL[0]) + nameL.Substring(1);
        _dropdownLanguage.choices.Add(nameLCapitalize);
      }
      _dropdownLanguage.RegisterValueChangedCallback(ChooseLanguage);

    // if (_gameManager.LevelManager == null)
    // {
    // }
    // else
    // {
    //   _languageBlock.style.display = DisplayStyle.None;
    // }

  }

  private async void ChooseLanguage(ChangeEvent<string> evt)
  {
    string nameLanguage = evt.newValue;

    var userSettings = _gameManager.AppInfo.setting;

    await LocalizationSettings.InitializationOperation.Task;

    // Debug.Log($"Choose lang={nameLanguage} | {selectedLocale} | {selectedLocale == nameLanguage}");
    Locale currentLocale = LocalizationSettings.AvailableLocales.Locales.Find(t =>
    {
      string nameCurrent = t.Identifier.CultureInfo.NativeName;
      string nameCurrentCapitalize = char.ToUpper(nameCurrent[0]) + nameCurrent.Substring(1);
      return nameCurrentCapitalize == nameLanguage;
    });
    if (currentLocale.Identifier.Code != LocalizationSettings.SelectedLocale.Identifier.Code)
    {
      LocalizationSettings.SelectedLocale = currentLocale;//LocalizationSettings.AvailableLocales.Locales[indexLocale];
      userSettings.lang = currentLocale.Identifier.Code;
      _gameManager.AppInfo.SaveSettings();
      base.Initialize(Wrapper);
      Title.text = await Helpers.GetLocaledString("settings");
      // // Words words = _gameManager.WordsAll.Find(t => t.locale.Identifier.Code == LocalizationSettings.SelectedLocale.Identifier.Code);
      // await _gameManager.SetActiveWords();

      // await _gameManager.StateManager.SetActiveDataGame();

      // // await UniTask.Delay(500);
    }


    OnChangeLocale?.Invoke();
  }

  private void CloseSettings()
  {
    AudioManager.Instance.Click();

    _result.isOk = true;

    _processCompletionSource.SetResult(_result);

    // _gameManager.InputManager.Enable();
  }
}
