using Assets;
using Cysharp.Threading.Tasks;

namespace User
{
  public class InitUserProvider : LocalAssetLoader
  {
    public async UniTask<AppInfoContainer> ShowAndHide()
    {
      var loginWindow = await Load();
      var result = await loginWindow.ProcessLogin();
      Unload();
      return result;
    }

    public UniTask<InitUserWindow> Load()
    {
      return LoadInternal<InitUserWindow>(ConstantsApp.UILabels.UI_INIT_USER);
    }

    public void Unload()
    {
      UnloadInternal();
    }
  }
}