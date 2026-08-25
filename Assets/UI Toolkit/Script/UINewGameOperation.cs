using Assets;

using Cysharp.Threading.Tasks;

public class UINewGameOperation : LocalAssetLoader
{
  public async UniTask<DataDialogResult> ShowAndHide()
  {
    var window = await Load();
    var result = await window.ProcessAction();
    Unload();
    return result;
  }

  public UniTask<UINewGame> Load()
  {
    return LoadInternal<UINewGame>(ConstantsApp.UILabels.UI_NEWGAME);
  }

  public void Unload()
  {
    UnloadInternal();
  }
}