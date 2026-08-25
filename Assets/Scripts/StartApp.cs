using System.Collections.Generic;
using UnityEngine;
using User;
using Loader;

public class StartApp : MonoBehaviour
{
  private LoaderBarProvider loaderBarProvider => GameManager.Instance.LoaderBarProvider;

  private async void Start()
  {
    // AppInfoContainer appInfoContainer = new();

    var loadingOperations = new Queue<ILoadingOperation>();
    loadingOperations.Enqueue(GameManager.Instance.AssetProvider);
    loadingOperations.Enqueue(new InitConfigOperation());
    loadingOperations.Enqueue(new InitUserOperation());
    // loadingOperations.Enqueue(new DashboardOperation());
    // GameManager.Instance.SetAppInfo(appInfoContainer);
    await loaderBarProvider.LoadAndDestroy(loadingOperations);

    // var dashBoard = new StartUIOperation();
    // await dashBoard.ShowAndHide();
    
    var uiManager = new UIManagerOperation();
    await uiManager.ShowAndHide();

    GameManager.Instance.ChangeState(GameState.StartApp);
  }
}
