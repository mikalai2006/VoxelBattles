using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class UINewGame : UIBase
{
  private Button _btnLoad;
  private Button _btnNew;
  private Button _btnBack;
  private TaskCompletionSource<DataDialogResult> _processCompletionSource;
  private DataDialogResult _result;

  public override void Start()
  {
    base.Start();

    // Title.text = "New Game";

    // var gameState = JsonUtility.FromJson<StateGame>(PlayerPrefs.GetString(_gameManager.Settings.nameSaveData));
    // bool isSavedGame = false;
    // if (gameState != null && gameState.games.Count > 0)
    // {
    //   isSavedGame = gameState.games.Find(t => t.typeGame == _gameManager.GameType.typeGame) != null;
    // }

    // _btnLoad = Wrapper.Q<Button>("BtnLoadGame");
    // _btnLoad.clickable.clicked += () =>
    // {
    //   ClickLoadGame();
    // };
    // if (!isSavedGame)
    // {
    //   _btnLoad.style.display = DisplayStyle.None;
    // }

    // _btnNew = Wrapper.Q<Button>("BtnNewGame");
    // _btnNew.clickable.clicked += () =>
    // {
    //   ClickNewGame();
    // };

    // _btnBack = Wrapper.Q<Button>("BtnBackToStartMenu");
    // _btnBack.clickable.clicked += () =>
    // {
    //   ClickBackToStartMenu();
    // };
    base.Initialize(Wrapper);
  }


  public async UniTask<DataDialogResult> ProcessAction()
  {
    _result = new DataDialogResult();

    _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

    return await _processCompletionSource.Task;
  }

  private void ClickNewGame()
  {
    AudioManager.Instance.Click();

    _result.isNew = true;
    _result.isOk = true;

    _processCompletionSource.SetResult(_result);

  }

  private void ClickLoadGame()
  {
    AudioManager.Instance.Click();

    _result.isLoad = true;
    _result.isOk = true;

    _processCompletionSource.SetResult(_result);
  }

  private void ClickBackToStartMenu()
  {
    AudioManager.Instance.Click();

    _result.isOk = false;

    _processCompletionSource.SetResult(_result);
  }
}
