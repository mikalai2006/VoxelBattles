using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Loader;
using System.Collections;
using Cysharp.Threading.Tasks;

public class LoaderBar : MonoBehaviour
{
  protected GameManager _gameManager => GameManager.Instance;

  [SerializeField] private Image _progressBar;
  [SerializeField] private Image _bgBar;
  [SerializeField] private TMPro.TextMeshProUGUI _loaderText;
  [SerializeField] private float _barSpeed;
  private float _progressFill;
  private float _targetProgress;
  private float _maxValueProgress;
  private bool isVisibleBar;

  private void Awake()
  {
    _maxValueProgress = _bgBar.rectTransform.rect.width;
    _loaderText.color = _gameManager.Theme.colorPrimary;
    _progressBar.color = _gameManager.Theme.colorAccent;
    SetActiveBar(false);
  }


  public async UniTask Load(Queue<ILoadingOperation> loadingOperations)
  {
    SetActiveBar(true);

    try
    {
      var settings = GameManager.Instance.Theme;
      if (settings == null)
      {
        settings = GameManager.Instance.Settings.ThemeDefault;
      }

      SetProgressValue(0);
    }
    catch (Exception e)
    {
      Debug.LogWarning($"{name} error: \n {e}");
    }

    isVisibleBar = true;

    StartCoroutine(UpdateProgressBar());
    foreach (var operation in loadingOperations)
    {
      await operation.Load(OnProgress, OnSetNotify);
      await WaitForBarFill();
    }

    isVisibleBar = false;
    SetActiveBar(false);
  }


  private void OnProgress(float progress)
  {
    _targetProgress = progress * 100f / _maxValueProgress;
  }


  private void OnSetNotify(string notify)
  {
    _loaderText.text = notify;
  }

  private IEnumerator UpdateProgressBar()
  {
    // Debug.Log($"UpdateProgressBar::: [{isVisibleBar}]{_progressFill}/{_targetProgress}");
    while (isVisibleBar == true)
    {
      if (_progressFill < _targetProgress)
      {
        _progressFill += Time.deltaTime * _barSpeed;
        // Debug.Log($"Value=[{_loaderText.text}]{_progressFill}/{_barSpeed}");
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


  private void SetProgressValue(float value)
  {
    _progressBar.rectTransform.sizeDelta = new Vector3(value, _progressBar.rectTransform.rect.height);
  }

  private void SetActiveBar(bool status)
  {
    _bgBar.gameObject.SetActive(status);
    _progressBar.gameObject.SetActive(status);
    _loaderText.gameObject.SetActive(status);
  }

}

