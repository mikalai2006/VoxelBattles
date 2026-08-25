using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class HPProgressController : MonoBehaviour
{
    GameManager gameManager => GameManager.Instance;
    UIGameViewController uIGameViewController;
    public CircleProgress progress;
    private CancellationTokenSource cancelTokenSource;
    private float durationFullProgress = .3f;
    //[SerializeField] float m_LowHPPercent = 25;

    void Awake()
    {
        CreateToken();

        GameSceneEvents.RefreshHP += UpdateHealth;
        GameSceneEvents.SetHP += SetHealth;
    }

    private void OnDestroy()
    {
        GameSceneEvents.RefreshHP -= UpdateHealth;
        GameSceneEvents.SetHP -= SetHealth;

        CancelToken();
    }

    public void Init(UIGameViewController _uIGameViewController)
    {
        uIGameViewController = _uIGameViewController;

        progress = _uIGameViewController.UIGameView.m_Wrapper.Q<CircleProgress>(UINames.HPProgress);
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

    void SetHealth(VehicleAssembler bm)
    {
        if (progress == null)
        {
            return;
        }

        progress.SetValue(1);
    }

    void UpdateHealth(VehicleAssembler bm)
    {
        //float health = bm.Data.ContainerData.levelDestruction;
        //// if (m_OriginalHPImage == null)
        //// {
        ////     // Store the original background style to reset the health bar sprite
        ////     m_OriginalHPImage = m_HealthBar.Q<VisualElement>(k_HPFillImage).style.backgroundImage;
        //// }

        //float lowHealth = 1 * m_LowHPPercent / 100;
        //// VisualElement healthBarProgress = uIGameViewController.UIGameView.m_Wrapper.Q<VisualElement>(TopSideBarComponent.IDNames.TopSideBarProgress);

        //if (health < lowHealth)
        //{
        //    progress.SetColor(gameManager.Theme.colorCompleted);
        //    // // fill.style.backgroundImage = new StyleBackground(m_LowHPImage);
        //    // healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorAccent);
        //}
        //else
        //{
        //    progress.SetColor(gameManager.Theme.colorAccent);
        //    // healthBarProgress.style.unityBackgroundImageTintColor = new StyleColor(gameManager.Theme.colorCompleted);
        //    // // fill.style.backgroundImage = m_OriginalHPImage;
        //}

        //progress.SetValue(1 - health);
    }

    public void SetValue(float _value)
    {
        progress.SetValue(_value);
    }

    public void RefreshHP()
    {
        CreateToken();

        AutoFillProgress(cancelTokenSource.Token).Forget();
    }

    async UniTask AutoFillProgress(CancellationToken token)
    {
        float elapsedTime = 0;

        while (elapsedTime <= durationFullProgress && !token.IsCancellationRequested)
        {
            elapsedTime += Time.deltaTime;

            progress.SetValue(Mathf.Lerp(0, 1, elapsedTime / durationFullProgress));

            await UniTask.DelayFrame(2, cancellationToken: token);
        }

        CancelToken();
    }
}