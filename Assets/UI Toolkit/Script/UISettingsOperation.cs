using Assets;

using Cysharp.Threading.Tasks;

public class UISettingsOperation : LocalAssetLoader
{
  // public UISettingsOperation()
  // {
  // }

  public async UniTask<DataDialogResult> ShowAndHide()
  {
    var window = await Load();
    var result = await window.ProcessAction();
    Unload();
    return result;
  }

  public UniTask<UISettings> Load()
  {
    return LoadInternal<UISettings>(ConstantsApp.UILabels.UI_SETTINGS);
  }

  public void Unload()
  {
    UnloadInternal();
  }
}