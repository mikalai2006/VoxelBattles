using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIToolkitLibrary;
using UnityEngine;
using UnityEngine.UIElements;

public class IndicatorManager: MonoBehaviour
{
    GameManager _gameManager => GameManager.Instance;
    [SerializeField] LevelManager LevelManager;
    [SerializeField] private Dictionary<VehicleAssembler, IndicatorComponent> indicators;
    [SerializeField] UIGameView m_UIGameView;
    [SerializeField] private Vector3 offset;
    public VehicleAssembler Target;
    private CancellationTokenSource cancelTokenSource;

    void Awake()
    {
        cancelTokenSource = new CancellationTokenSource();

        offset = _gameManager.Settings.offsetHud;

        indicators = new();
    }

    private void OnDestroy()
    {
        if (!cancelTokenSource.Token.IsCancellationRequested)
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
        }
    }

    async UniTask RefreshIndicators(CancellationToken token)
    {
        while(indicators.Count > 0 && !token.IsCancellationRequested)
        {
            for (int i = 0; i < indicators.Count; i++)
            {
                // float minX = 30;
                // float maxX = Screen.width - minX;

                // float minY = 30;
                // float maxY = Screen.height - minY;

                // Vector2 screenPos = LevelManager.Camera.WorldToScreenPoint(indicators.ElementAt(i).Key.transform.position + offset);

                // // if (Vector3.Dot((indicators.ElementAt(i).Key.transform.position - Target.transform.position), Target.Towers[0].transform.forward) < 0)
                // // {
                // //     // if (pos.x < Screen.width / 2)
                // //     // {
                // //     //     pos.x = maxX;
                // //     // } else
                // //     // {
                // //     //     pos.x = minX;
                // //     // }
                // //     pos.y = maxY;
                // // }

                // screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
                // screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

                // Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(m_UIGameView.m_UIDoc.rootVisualElement.panel, screenPos);
                
                // 2.
                // Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                //     m_UIGameView.m_UIDoc.rootVisualElement.panel,
                //     indicators.ElementAt(i).Key.transform.position,
                //     LevelManager.Camera
                // );

                // indicators.ElementAt(i).Value.style.translate = new StyleTranslate(new Translate(panelPosition.x, panelPosition.y));

                // 3.
                // Vector3 screenPos = LevelManager.Camera.WorldToScreenPoint(indicators.ElementAt(i).Key.transform.position + offset);

                // // Проверка: перед камерой ли объект
                // if (screenPos.z >= 0)
                // {
                //     SetHideIndicator(indicators.ElementAt(i).Key);
                //     return;
                // }
                // SetShowIndicator(indicators.ElementAt(i).Key);

                // // UI Toolkit использует Y, направленный вниз, Unity ScreenPoint - вверх
                // float uiX = screenPos.x;
                // float uiY = Screen.height - screenPos.y;

                // // marker.style.left = uiX - (marker.layout.width / 2);
                // // marker.style.top = uiY - (marker.layout.height / 2);
                // indicators.ElementAt(i).Value.style.translate = new StyleTranslate(new Translate(
                //     uiX - (indicators.ElementAt(i).Value.layout.width / 2),
                //     uiY - (indicators.ElementAt(i).Value.layout.height / 2)
                // ));

                // 4.
            float minX = 0; // indicators.ElementAt(i).Value.layout.width / 2.5f;
            float maxX = Screen.width - indicators.ElementAt(i).Value.layout.width * 1.5f;

            float minY = 0;
            float maxY = Screen.height - indicators.ElementAt(i).Value.layout.height * 1.5f;

            Vector3 screenPos = LevelManager.Camera.WorldToScreenPoint(indicators.ElementAt(i).Key.transform.position + offset);

            // if (Vector3.Dot((indicators.ElementAt(i).Key.transform.position - Target.transform.position), Target.Towers[0].transform.forward) < 0)
            // {
            //     // if (pos.x < Screen.width / 2)
            //     // {
            //     //     pos.x = maxX;
            //     // } else
            //     // {
            //     //     pos.x = minX;
            //     // }
            //     screenPos.y = minY;
            // }
            // Проверка, находится ли объект за спиной камеры
            if (screenPos.z < 0)
            {
                screenPos *= -1; // Инвертируем, чтобы маркер был снизу
            }

            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
            screenPos.y = Mathf.Clamp(Screen.height - screenPos.y, minY, maxY);

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(m_UIGameView.m_UIDoc.rootVisualElement.panel, screenPos);

            indicators.ElementAt(i).Value.style.translate = new StyleTranslate(new Translate(
                panelPosition.x,// - (indicators.ElementAt(i).Value.layout.width / 2),
                panelPosition.y// - (indicators.ElementAt(i).Value.layout.height / 2)
            ));
            
            indicators.ElementAt(i).Value.UpdateText((int)(Vector3.Distance(indicators.ElementAt(i).Key.transform.position, Target.transform.position) / _gameManager.Settings.scaleObjects));

                // Vector3 screenPos = LevelManager.Camera.WorldToScreenPoint(indicators.ElementAt(i).Key.transform.position + offset);
            
                // // Проверка, находится ли объект за спиной камеры
                // if (screenPos.z < 0)
                // {
                //     screenPos *= -1; // Инвертируем, чтобы маркер был снизу
                // }

                // // Логика ограничения маркера границами экрана
                // Vector2 clampedPos = new Vector2(
                //     Mathf.Clamp(screenPos.x, 50, Screen.width - 50),
                //     Mathf.Clamp(Screen.height - screenPos.y, 50, Screen.height - 50) // Инверсия Y для UI Toolkit
                // );

                // indicators.ElementAt(i).Value.style.translate = new StyleTranslate(new Translate(
                //     clampedPos.x,// - (indicators.ElementAt(i).Value.layout.width / 2),
                //     clampedPos.y// - (indicators.ElementAt(i).Value.layout.height / 2)
                // ));
            }
            
            await UniTask.DelayFrame(_gameManager.Settings.playerOptions.delayFrameRefreshHud, cancellationToken: token);//Delay(System.TimeSpan.FromSeconds(0.10f), cancellationToken: token);
        }
    }

    public void Init()
    {
        RefreshIndicators(cancelTokenSource.Token).Forget();
    }

    public void AddIndicator(VehicleAssembler baseMachine)
    {
        if (!indicators.ContainsKey(baseMachine))
        {
            IndicatorComponent visualElement = m_UIGameView.AddIndicator();

            indicators.Add(baseMachine, visualElement);
        }
    }

    public void RemoveIndicator(VehicleAssembler baseMachine)
    {
        if (indicators.ContainsKey(baseMachine))
        {
            m_UIGameView.RemoveIndicator(indicators[baseMachine]);
            
            indicators.Remove(baseMachine);
        }
    }

    public void SetShowIndicator(VehicleAssembler baseMachine)
    {
        indicators[baseMachine].style.display = DisplayStyle.Flex;
    }
    public void SetHideIndicator(VehicleAssembler baseMachine)
    {
        indicators[baseMachine].style.display = DisplayStyle.None;
    }

    public void SetTarget(VehicleAssembler targetIndicator)
    {
        Target = targetIndicator;
    }
}