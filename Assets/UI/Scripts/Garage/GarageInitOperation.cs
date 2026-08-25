using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Settings;

namespace Loader
{
  public class GarageInitOperation : ILoadingOperation
  {
    public async UniTask Load(Action<float> onProgress, Action<string> onSetNotify)
    {
      var _gameManager = GameManager.Instance;

      await LocalizationSettings.InitializationOperation.Task;

      onProgress?.Invoke(0f);

      var t = await Helpers.GetLocaledString("loadingGameScene");
      onSetNotify?.Invoke(t);
      var environment = await _gameManager.AssetProvider.LoadSceneAdditive(ConstantsApp.Scenes.SCENE_GARAGE);
      // var rootObjects = environment.Scene.GetRootGameObjects();
      _gameManager.currentScene = environment;

      // LevelManager LevelManager = GameObject.FindGameObjectWithTag("LevelManager")?.GetComponent<LevelManager>();

      // if (LevelManager != null)
      // {
      //   _gameManager.InitGameGrid(LevelManager, environment);
      //   // GameManager.Instance.LevelManager = LevelManager;
      //   // GameManager.Instance.environment = environment;
      //   // // LevelManager.CreateLevel();
      // }


      // onProgress?.Invoke(.15f);
    }
  }
}