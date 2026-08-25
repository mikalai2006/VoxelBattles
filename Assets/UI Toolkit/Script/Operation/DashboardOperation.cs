using System;
using Assets;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Settings;

namespace Loader
{
  public class DashboardOperation : LocalAssetLoader
  {
    // public async UniTask Load(Action<float> onProgress, Action<string> onSetNotify)
    // {

    //   await LocalizationSettings.InitializationOperation;

    //   var t = await Helpers.GetLocaledString("loading");
    //   onSetNotify?.Invoke(t);

    //   onProgress?.Invoke(0.9f);

    //   var environment = await GameManager.Instance.AssetProvider.LoadAsset(Constants.UILabels.UI_DASHBOARD);

    //   if (environment.TryGetComponent(out UIDashboard component) == false)
    //     throw new NullReferenceException("Object of type UIDashboard is null");

    //   onProgress?.Invoke(1f);
    // }
    public async UniTask<DataDialogResult> ShowAndHide()
    {
      var dashboardWindow = await Load();
      var result = await dashboardWindow.ProcessAction();
      Unload();
      return result;
    }

    public UniTask<UIDashboard> Load()
    {
      return LoadInternal<UIDashboard>(ConstantsApp.UILabels.UI_DASHBOARD);
    }

    public void Unload()
    {
      UnloadInternal();
    }
  }
}