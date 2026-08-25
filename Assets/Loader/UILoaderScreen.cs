using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Loader;
using System.Collections;
using Cysharp.Threading.Tasks;

public class UILoaderScreen : MonoBehaviour
{
  [SerializeField] private UIDocument _uiDoc;
  public UIDocument Root => _uiDoc;

  // Event called when Play Button is clicked.
  public UnityAction OnShowNewGame;

  // Event called when Play Button is clicked.
  public UnityAction OnQuit;

  private VisualElement progressBar;
  private Label progressBarText;
  private VisualElement progressBarSection;
  private VisualElement buttonsSection;

  private string NameProgressBarSection = "ProgressBarSection";
  private string NameProgressBarText = "ProgressBarText";
  private string NameProgressBar = "ProgressBar";

  [SerializeField]
  private float _barSpeed;
  private float _progressFill;
  private float _targetProgress;

  public async UniTask Load(Queue<ILoadingOperation> loadingOperations)
  {
    try
    {
      Root.rootVisualElement.Q<VisualElement>("OverBG").style.backgroundColor
        = new StyleColor(GameManager.Instance.Theme.bgColor);
      var settings = GameManager.Instance.Theme;
      if (settings == null)
      {
        settings = GameManager.Instance.Settings.ThemeDefault;
      }
      Camera.main.backgroundColor = settings.bgColor;
      // Root.rootVisualElement.Q<VisualElement>("OverBG").style.backgroundImage
      //   = new StyleBackground(settings.bgImage);
      progressBarSection = Root.rootVisualElement.Q<VisualElement>(NameProgressBarSection);
      progressBar = Root.rootVisualElement.Q<VisualElement>(NameProgressBar);
      progressBar.style.backgroundColor = new StyleColor(settings.colorAccent);
      progressBarText = Root.rootVisualElement.Q<Label>(NameProgressBarText);
      progressBarText.style.color = new StyleColor(settings.colorPrimary);
      SetProgressValue(0);
      //buttonsSection = MenuApp.rootVisualElement.Q<VisualElement>("ButtonsSection");

      //var newGameButton = MenuApp.rootVisualElement.Q<Button>("newgame");
      //newGameButton.clickable.clicked += () =>
      //{
      //    // GameManager.Instance.ChangeState(GameState.NewGame);
      //    OnShowNewGame?.Invoke();
      //};

      //var loadGameButton = MenuApp.rootVisualElement.Q<Button>("loadgame");
      //loadGameButton.clickable.clicked += () =>
      //{
      //    GameManager.Instance.ChangeState(GameState.LoadGame);
      //};

      //var btnQuit = MenuApp.rootVisualElement.Q<Button>("ButtonQuit");
      //btnQuit.clickable.clicked += () =>
      //{
      //    OnQuit?.Invoke();
      //};

      // progressBarSection.visible = false;


    }
    catch (Exception e)
    {
      Debug.LogWarning("Menu New Game error: \n" + e);
    }

    progressBarSection.visible = true;

    StartCoroutine(UpdateProgressBar());

    foreach (var operation in loadingOperations)
    {
      //ResetFill();
      // progressBarText.text = operation.Description;

      // Debug.Log($"operation: {operation.GetType()}");
      await operation.Load(OnProgress, OnSetNotify);
      await WaitForBarFill();
    }
    progressBarSection.visible = false;
  }

  // private void ResetFill()
  // {
  //   _targetProgress = 0f;
  //   _progressFill = 0f;
  // }

  private void OnProgress(float progress)
  {
    _targetProgress = progress * 100f;
  }
  private void OnSetNotify(string notify)
  {
    progressBarText.text = notify;
  }

  private IEnumerator UpdateProgressBar()
  {
    while (progressBarSection.visible == true)
    {
      if (_progressFill < _targetProgress)
      {
        _progressFill += Time.deltaTime * _barSpeed;
        // Debug.Log($"Value=[{progressBarText.text}]{_progressFill}/{_barSpeed}");
        SetProgressValue(_progressFill);
      }
      yield return null;
    }
  }
  private async UniTask WaitForBarFill()
  {
    while (_progressFill < _targetProgress)
    {
      await UniTask.Delay(1);
    }
    await UniTask.Delay(1); //  TimeSpan.FromSeconds(0.15f));
  }

  //public void InitNewGame()
  //{
  //    buttonsSection.visible = false;
  //    progressBarSection.visible = true;
  //}

  //public void SetProgressText(string text)
  //{
  //    progressBarText.text = text;
  //}

  private void SetProgressValue(float value)
  {
    progressBar.style.width = new StyleLength(new Length(value, LengthUnit.Percent)); ;
  }
}

