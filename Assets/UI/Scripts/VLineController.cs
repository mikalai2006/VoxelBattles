using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class VLineController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    UIGameViewController uIGameViewController;
    public VehicleAssembler target;
    //public Dictionary<VLine, BaseMuzzle> vlines;
    public VisualElement DynamicWrapper;
    private CancellationTokenSource cancelTokenSource;

    void Awake()
    {
        cancelTokenSource = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        if (!cancelTokenSource.Token.IsCancellationRequested)
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
        }
    }

    public void Init(UIGameViewController _uIGameViewController)
    {
        //vlines = new ();

        uIGameViewController = _uIGameViewController;

        DynamicWrapper = _uIGameViewController.UIGameView.m_Wrapper.Q<VisualElement>(UINames.DynamicWrapper);
    }

    public void SetTarget(VehicleAssembler _baseMachine)
    {
        target = _baseMachine;

        // for (int i = 0; i < target.Towers.Count; i++)
        // {
        //     for (int j = 0; j < target.Towers[i].Muzzles.Count; j++)
        //     {
        //         VLine line = new VLine()
        //         {
        //             name = UINames.LineTrajectoryMuzzle,
        //         };

        //         DynamicWrapper.Add(line);

        //         vlines.Add(line, target.Towers[i].Muzzles[j]);
        //     }
        // }
    }

    //async UniTask UpdateLines(CancellationToken token)
    //{
    //    //while(vlines.Count > 0 && !token.IsCancellationRequested)
    //    //{
    //    //    for (int j = 0; j < vlines.Count; j++)
    //    //    {
    //    //        Vector2 pointStart = RuntimePanelUtils.CameraTransformWorldToPanel(vlines.ElementAt(j).Key.panel, vlines.ElementAt(j).Value.PointEffects.transform.position, levelManager.Camera);
    //    //        Vector2 pointEnd = RuntimePanelUtils.CameraTransformWorldToPanel(vlines.ElementAt(j).Key.panel, vlines.ElementAt(j).Value.decal.transform.position, levelManager.Camera);

    //    //        vlines.ElementAt(j).Key.pointStart = pointStart;
    //    //        vlines.ElementAt(j).Key.pointEnd = pointEnd;

    //    //        // Debug.LogWarning($"{new Vector2(Screen.width / 2f, Screen.height * 0.7f)}, {panelPosition}");
    //    //    }

    //    //    await UniTask.DelayFrame(1, cancellationToken: token);//Delay(System.TimeSpan.FromSeconds(0.10f), cancellationToken: token);
    //    //}
    //}

    // void Update()
    // {
    //     if (target == null)
    //     {
    //         return;
    //     }

    //     if (vlines.Count > 0)
    //     {
    //         for (int j = 0; j < vlines.Count; j++)
    //         {
    //             Vector2 pointStart = RuntimePanelUtils.CameraTransformWorldToPanel(vlines.ElementAt(j).Key.panel, vlines.ElementAt(j).Value.PointEffects.transform.position, levelManager.Camera);
    //             Vector2 pointEnd = RuntimePanelUtils.CameraTransformWorldToPanel(vlines.ElementAt(j).Key.panel, vlines.ElementAt(j).Value.decal.transform.position, levelManager.Camera);

    //             vlines.ElementAt(j).Key.pointStart = pointStart;
    //             vlines.ElementAt(j).Key.pointEnd = pointEnd;

    //             // Debug.LogWarning($"{new Vector2(Screen.width / 2f, Screen.height * 0.7f)}, {panelPosition}");
    //         }
    //     }
    // }

    //public void CreateLines(BaseTower baseTower)
    //{
    //    for (int j = 0; j < baseTower.Muzzles.Count; j++)
    //    {
    //        VLine line = new VLine()
    //        {
    //            name = UINames.LineTrajectoryMuzzle,
    //            color = Color.green,
    //            lineWidth = 2,
    //        };

    //        DynamicWrapper.Add(line);

    //        vlines.Add(line, baseTower.Muzzles[j]);
    //    }


    //    UpdateLines(cancelTokenSource.Token).Forget();
    //}
}
