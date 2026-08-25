using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class WeaponProgressController: MonoBehaviour
{
    public CircleProgress progress;
    private CancellationTokenSource cancelTokenSource;
    private float durationFullProgress = .3f;

    void Awake()
    {
        CreateToken();
    }

    private void OnDestroy()
    {
        CancelToken();
    }

    void CreateToken()
    {
        if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
        {
            CancelToken();
        }
        cancelTokenSource = new CancellationTokenSource();
    }

    void CancelToken()
    {
        if (!cancelTokenSource.IsCancellationRequested)
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
        }
    }

    public void SetDurationFillProgress(float _duration)
    {
        durationFullProgress = _duration;
    }

    public void SetValue(float _value)
    {
        progress.SetValue(_value);
        
        if (_value <= 0)
        {
            CreateToken();

            AutoFillProgress(cancelTokenSource.Token).Forget();
        }
    }

    async UniTask AutoFillProgress(CancellationToken token)
    {
        float elapsedTime = 0;

        while(elapsedTime <= durationFullProgress && !token.IsCancellationRequested)
        {
            elapsedTime += Time.deltaTime;

            progress.SetValue(Mathf.Lerp(0, 1, elapsedTime / durationFullProgress));

            await UniTask.DelayFrame(2, cancellationToken: token);
        }

        CancelToken();
    }

    public void Init(UIGameViewController uIGameViewController)
    {
        progress = uIGameViewController.UIGameView.m_Wrapper.Q<CircleProgress>(UINames.WeaponProgress);
    }
}