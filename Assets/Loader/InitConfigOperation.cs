using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using User;

namespace Loader
{
  public class InitConfigOperation : ILoadingOperation
  {
    // [DllImport("__Internal")]
    // private static extern string GetLang();

    public async UniTask Load(Action<float> onProgress, Action<string> onSetNotify)
    {
      Debug.Log("Init config");

      await LocalizationSettings.InitializationOperation.Task;

      string langString = "";

      // #if ysdk
      //       langString = GetLang();
      // #endif

      if (string.IsNullOrEmpty(langString))
      {
        langString = LocalizationSettings.SelectedLocale.Identifier.Code;
      }

      AppInfoContainer playPrefData = new();

      // GameManager.Instance.SetLangCodePlayPref(langString);

      string namePlayPref = GameManager.Instance.Settings.nameSaveUserInfo; //.KeyPlayPref;

      if (PlayerPrefs.HasKey(namePlayPref))
      {
        playPrefData = JsonUtility.FromJson<AppInfoContainer>(PlayerPrefs.GetString(namePlayPref));
        langString = playPrefData.setting.lang;
      }

      if (!string.IsNullOrEmpty(langString))
      {
        Locale needSetLocale = LocalizationSettings.AvailableLocales.Locales.Find(t => t.Identifier.Code == langString);
        if (langString != LocalizationSettings.SelectedLocale.Identifier.Code)
        {
          LocalizationSettings.SelectedLocale = needSetLocale;
          Debug.Log($"needSetLocale={needSetLocale}");
        }
      }

      string t = await Helpers.GetLocaledString("loading");
      onSetNotify?.Invoke(t);

      onProgress?.Invoke(0.1f);
      // GameManager.Instance.ResourceSystem = ResourceSystem.Instance;
      // await ResourceSystem.Instance.LoadCollectionsAsset<GameEntity>(ConstantsApp.Labels.LABEL_ENTITY);

      // onProgress?.Invoke(0.2f);
      // GameManager.Instance.ResourceSystem = ResourceSystem.Instance;
      // await ResourceSystem.Instance.LoadCollectionsAsset<GameBonus>(ConstantsApp.Labels.LABEL_BONUS);

      onProgress?.Invoke(0.3f);
      GameManager.Instance.ResourceSystem = ResourceSystem.Instance;
      await ResourceSystem.Instance.LoadCollectionsAsset<GameTheme>(ConstantsApp.Labels.LABEL_THEME);
      onProgress?.Invoke(0.35f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameTowerShop>(ConstantsApp.Labels.SHOP_TOWER);
      //onProgress?.Invoke(0.4f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameTower>(ConstantsApp.Labels.TOWER);
      //onProgress?.Invoke(0.45f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameMuzzle>(ConstantsApp.Labels.MUZZLE);
      //onProgress?.Invoke(0.5f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameBody>(ConstantsApp.Labels.BODY);
      //onProgress?.Invoke(0.55f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameWheel>(ConstantsApp.Labels.WHEEL);
      //onProgress?.Invoke(0.6f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameCaterpillar>(ConstantsApp.Labels.CATERPILLAR);
      //onProgress?.Invoke(0.65f);
      //await ResourceSystem.Instance.LoadCollectionsAsset<GameMachine>(ConstantsApp.Labels.MACHINE);
      
      Debug.Log("Init config end");
    }
  }
}