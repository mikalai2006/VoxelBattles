using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Loader;
using UnityEngine.Localization.Settings;
using System.Threading.Tasks;

namespace User
{
  public class InitUserOperation : ILoadingOperation
  {
    private AppInfoContainer _playPrefData;
    private TaskCompletionSource<UserInfo> _getUserInfoCompletionSource;
    private TaskCompletionSource<StatePlayer> _getUserDataCompletionSource;
    private TaskCompletionSource<UserSettings> _getUserSettingCompletionSource;
    private Action<float> _onProgress;
    private Action<string> _onSetNotify;

    public async UniTask Load(Action<float> onProgress, Action<string> onSetNotify)
    {
      Debug.Log("Init user");

      var _gameManager = GameManager.Instance;

      _playPrefData = new();

      _onProgress = onProgress;
      _onSetNotify = onSetNotify;

      var t = await Helpers.GetLocaledString("loading");
      _onSetNotify?.Invoke(t);
      _onProgress?.Invoke(0.3f);

      string namePlaypref = _gameManager.Settings.nameSaveUserInfo;

      _gameManager.DataManager.Init(_gameManager.Settings.nameSaveData);

      if (PlayerPrefs.HasKey(namePlaypref))
      {
        _playPrefData = JsonUtility.FromJson<AppInfoContainer>(PlayerPrefs.GetString(namePlaypref));
      }
      else
      {
        _playPrefData.uid = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); //DeviceInfo.GetDeviceId();

        await LocalizationSettings.InitializationOperation.Task;
        var setting = new UserSettings()
        {
          auv = _gameManager.Settings.Audio.volumeEffect,
          lang = LocalizationSettings.SelectedLocale.Identifier.Code,
          muv = _gameManager.Settings.Audio.volumeMusic,
          iss = true,
          // theme = _gameManager.Settings.ThemeDefault.name,
          // dod = true,
          // td = _gameManager.Settings.timeDelayOverChar // time delay
        };
        _playPrefData.setting = setting;
      }

      // Get user info.
      DataManager.OnLoadUserInfo += SetUserInfo;
      _playPrefData.UserInfo = await GetUserInfo();
      DataManager.OnLoadUserInfo -= SetUserInfo;


      var tgd = await Helpers.GetLocaledString("loadingData");
      _onSetNotify?.Invoke(tgd);
      _onProgress?.Invoke(0.7f);

      // Get game data.
      DataManager.OnLoadData += InitData;
      await GetUserData();
      DataManager.OnLoadData -= InitData;

      await _gameManager.SetAppInfo(_playPrefData);
      // Set user setting to PlayPref.
      // string jsonData = JsonUtility.ToJson(_playPrefData);
      // PlayerPrefs.SetString(namePlaypref, jsonData);

      _onProgress?.Invoke(.5f);

      Debug.Log("Init user end");
    }


    private void SetUserInfo(UserInfo userInfo)
    {
      _playPrefData.UserInfo = userInfo;
      _getUserInfoCompletionSource.SetResult(userInfo);
    }


    public async Task<UserInfo> GetUserInfo()
    {
      _getUserInfoCompletionSource = new TaskCompletionSource<UserInfo>();

#if android
      UserInfo userInfo = _playPrefData.UserInfo;
      if (string.IsNullOrEmpty(userInfo.name))
      {
        // Load form for input name.
        var result = await GameManager.Instance.InitUserProvider.ShowAndHide();
        userInfo = result.UserInfo;
      }
      SetUserInfo(userInfo);
#endif

      // #if ysdk
      //       GameManager.Instance.DataManager.LoadUserInfoAsYsdk();
      // #endif

      return await _getUserInfoCompletionSource.Task;
    }


    private async UniTask<StatePlayer> GetUserData()
    {

      _getUserDataCompletionSource = new();

#if android
      StatePlayer stateGame = await GameManager.Instance.DataManager.Load();
#endif

      // #if ysdk
      //       GameManager.Instance.DataManager.LoadAsYsdk();
      // #endif

      return await _getUserDataCompletionSource.Task;
    }

    private void InitData(StatePlayer stateGame)
    {
      var _gameManager = GameManager.Instance;
      _gameManager.StateManager = new();
      stateGame = _gameManager.StateManager.Init(stateGame);

      _getUserDataCompletionSource.SetResult(stateGame);
    }

  }
}