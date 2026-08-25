using Assets;

using Cysharp.Threading.Tasks;
using UIToolkitLibrary;

public class UIManagerOperation : LocalAssetLoader
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

  public UniTask<UIManager> Load()
  {
    return LoadInternal<UIManager>(ConstantsApp.UILabels.UI_MANAGER);
  }

  public void Unload()
  {
    UnloadInternal();
  }
}