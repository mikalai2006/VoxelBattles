using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class PricelController : MonoBehaviour
{
    private GameManager _gameManager => GameManager.Instance;
    [SerializeField] private LevelManager levelManager;
    UIGameViewController uIGameViewController;
    public VehicleAssembler target;
    public CirclePricel pricel;
    public Label labelDistance;
    private StringBuilder _sbDistance = new StringBuilder(64);

    public void Init(UIGameViewController _uIGameViewController)
    {
        uIGameViewController = _uIGameViewController;

        pricel = _uIGameViewController.UIGameView.m_Wrapper.Q<CirclePricel>(UINames.MachinePricel);

        labelDistance = _uIGameViewController.UIGameView.m_Wrapper.Q<Label>(UINames.LabelDistance);
        
        pricel.style.alignSelf = Align.FlexStart;

    }

    public void SetTarget(VehicleAssembler _baseMachine)
    {
        target = _baseMachine;
    }

    public void SetDistance(float distance)
    {
        _sbDistance.Clear(); // Очищаем буфер без выделения новой памяти
        _sbDistance.Append("~ ");
        _sbDistance.Append(Mathf.Round(distance / _gameManager.Settings.scaleObjects));
        _sbDistance.Append(" м");
        _sbDistance.Append("(");
        _sbDistance.Append(distance);
        _sbDistance.Append(")");
        
        labelDistance.text = _sbDistance.ToString();  // ToString() здесь необходим
    }

    //void Update()
    //{
    //    if (target == null)
    //    {
    //        return;
    //    }

    //    if (target.Towers.Count > 0)
    //    {
    //        // float minX = 0; // indicators.ElementAt(i).Value.layout.width / 2.5f;
    //        // float maxX = Screen.width - 50;

    //        // float minY = 0;
    //        // float maxY = Screen.height - 50;

    //        // Vector3 screenPos = levelManager.Camera.WorldToScreenPoint(target.Towers.ElementAt(0).Muzzles.ElementAt(0).MaxDistanceObject.transform.position);

    //        // screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
    //        // screenPos.y = Mathf.Clamp(Screen.height - screenPos.y, minY, maxY);
    //        // screenPos.z = 0;


    //        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(pricel.panel, target.Towers.ElementAt(0).Muzzles.ElementAt(0).decal.transform.position, levelManager.Camera);

    //        pricel.style.translate = new StyleTranslate(new Translate(
    //            panelPosition.x,// - (indicators.ElementAt(i).Value.layout.width / 2),
    //            panelPosition.y// - (indicators.ElementAt(i).Value.layout.height / 2)
    //        ));

    //        // Debug.LogWarning($"{new Vector2(Screen.width / 2f, Screen.height * 0.7f)}, {panelPosition}");
    //    }
    //}
}
