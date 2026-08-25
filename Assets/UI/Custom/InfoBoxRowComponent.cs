using Cysharp.Threading.Tasks;
using UIToolkitLibrary;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Пользовательский визуальный элемент, отображающий строку в информационном блоке
/// </summary>
[UxmlElement]
public partial class InfoBoxRowComponent : VisualElement, IStyles
{
    GameManager _gameManager => GameManager.Instance;
    public static class IDNames
    {
        public static string InfoBoxRowWrapper = "InfoBoxRowWrapper";
        public static string InfoBoxRowProgressTime = "InfoBoxRowProgressTime";
        public static string InfoBoxRowCell1 = "InfoBoxRowCell1";
        public static string InfoBoxRowCell2 = "InfoBoxRowCell2";
        public static string InfoBoxRowLabel1 = "InfoBoxRowLabel1";
        public static string InfoBoxRowLabel2 = "InfoBoxRowLabel2";
        public static string InfoBoxRowIco = "InfoBoxRowIco";
        
    }
    VisualElement m_Wrapper;
    VisualElement m_ProgressTime;
    VisualElement infoEl1;
    Label infoElLabel1;
    VisualElement infoEl2;
    Label infoElLabel2;
    VisualElement ico1;
    VisualElement ico2;
    float durationHide = .5f;
    public Vector3 targetTranslation = new Vector3(0, -20, 0);
    public Vector3 initialTranslation = new Vector3(0, 0, 0);

    public InfoBoxRowComponent()
    {
        m_Wrapper = new VisualElement {name = IDNames.InfoBoxRowWrapper};
        m_Wrapper.usageHints = UsageHints.GroupTransform & UsageHints.DynamicColor & UsageHints.DynamicTransform;
        m_Wrapper.pickingMode = PickingMode.Ignore;

        m_Wrapper.style.flexDirection = FlexDirection.Row;
        m_Wrapper.style.position = Position.Relative;
        m_Wrapper.style.marginTop = new StyleLength(2);
        // m_Wrapper.style.paddingLeft = new StyleLength(25);
        // m_Wrapper.style.paddingRight = new StyleLength(25);
        m_Wrapper.pickingMode = PickingMode.Ignore;
        Add(m_Wrapper);

#region Items
        infoEl1 = new VisualElement {name = IDNames.InfoBoxRowCell1};
        infoEl1.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        infoEl1.pickingMode = PickingMode.Ignore;
        infoEl1.style.flexGrow = 1;
        infoEl1.style.flexDirection = FlexDirection.Row;
        infoEl1.pickingMode = PickingMode.Ignore;
        // infoEl1.style.width = new StyleLength(250);
        m_Wrapper.Add(infoEl1);

        ico1 = new VisualElement{ name = IDNames.InfoBoxRowIco};
        ico1.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        ico1.pickingMode = PickingMode.Ignore;
        ico1.style.width = new StyleLength(25);
        ico1.style.height = new StyleLength(25);
        infoEl1.Add(ico1);
        
        infoElLabel1 = new Label{ name = IDNames.InfoBoxRowCell1};
        infoElLabel1.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        infoElLabel1.pickingMode = PickingMode.Ignore;
        infoElLabel1.text = "Begin text";
        infoEl1.Add(infoElLabel1);

        infoEl2 = new VisualElement {name = IDNames.InfoBoxRowCell2};
        infoEl2.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        infoEl2.pickingMode = PickingMode.Ignore;
        infoEl2.style.flexGrow = 1;
        infoEl2.style.flexDirection = FlexDirection.Row;
        infoEl2.pickingMode = PickingMode.Ignore;
        // infoEl1.style.width = new StyleLength(250);
        m_Wrapper.Add(infoEl2);

        ico2 = new VisualElement{ name = IDNames.InfoBoxRowIco};
        ico2.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        ico2.pickingMode = PickingMode.Ignore;
        ico2.style.width = new StyleLength(25);
        ico2.style.height = new StyleLength(25);
        infoEl2.Add(ico2);

        infoElLabel2 = new Label{ name = IDNames.InfoBoxRowLabel2};
        infoElLabel2.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        infoElLabel2.pickingMode = PickingMode.Ignore;
        infoElLabel2.text = "Begin text";
        infoEl2.Add(infoElLabel2);
#endregion

        m_ProgressTime = new VisualElement {name = IDNames.InfoBoxRowProgressTime};
        m_ProgressTime.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
        m_ProgressTime.pickingMode = PickingMode.Ignore;
        m_ProgressTime.style.position = Position.Absolute;
        m_ProgressTime.style.height = new StyleLength(2);
        
        m_Wrapper.Add(m_ProgressTime);

        UpdateStyles();
    }

    // public void OnEnable()
    // {
    //     // infoEl1 = this.Q<VisualElement>("Item1");
    //     // infoElLabel1 = infoEl1.Q<Label>("InfoItemLabel");
        
    //     // infoEl2 = this.Q<VisualElement>("Item2");
    //     // infoElLabel2 = infoEl2.Q<Label>("InfoItemLabel");
    // }

    public void Init(UIGameViewController gameViewController, AppInfoDamageData data)
    {
        //m_Wrapper.style.position = Position.Relative;

        //infoElLabel1.text = data.kto.MachineLevelData.name;
        //infoEl1.style.visibility = Visibility.Visible;

        //if (data.komy != null)
        //{
        //    infoElLabel2.text = data.komy.MachineLevelData.name;
        //    infoEl2.style.visibility = Visibility.Visible;
        //} else
        //{
        //    infoEl2.style.visibility = Visibility.Hidden;
        //}
        
        //HideByTime(gameViewController, data.duration).Forget();
    }

    async private UniTask HideByTime(UIGameViewController gameViewController, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Calculate the current translation based on time
            float t = elapsedTime / duration;

            // Vector3 currentTranslation = Vector3.Lerp(initialTranslation, targetTranslation, t);
            // Apply the translation to the VisualElement's style
            // style.translate = new Translate(currentTranslation.x, currentTranslation.y, currentTranslation.z);
            // style.opacity =  new StyleFloat(duration - elapsedTime);

            m_ProgressTime.style.width = new StyleLength(new Length((1 - t) * 100, LengthUnit.Percent));

            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }

        elapsedTime = 0;
        
        while (elapsedTime < durationHide)
        {
            float t = elapsedTime / durationHide;

            Vector3 currentTranslation = Vector3.Lerp(initialTranslation, targetTranslation, t);
            style.translate = new Translate(currentTranslation.x, currentTranslation.y, currentTranslation.z);
            style.opacity =  new StyleFloat(duration - elapsedTime);
            
            // if (t > 0.5f)
            // {
            //     style.position = Position.Absolute;
            // }

            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }

        style.translate = new Translate(initialTranslation.x, initialTranslation.y, initialTranslation.z);

        gameViewController.RemoveInfoItem(this);
    }

    public void UpdateStyles()
    {
        Color color = Color.black;
        color.a = 0.2f;
        m_Wrapper.style.backgroundColor = new StyleColor(_gameManager ? _gameManager.Theme.colorBgInfoRow : color);
        infoElLabel1.style.color = new StyleColor(_gameManager ? _gameManager.Theme.colorTextInfoRow : Color.white);
        infoElLabel2.style.color = new StyleColor(_gameManager ? _gameManager.Theme.colorTextInfoRow : Color.white);

        if (_gameManager)
        {
            ico1.style.backgroundImage = new StyleBackground(_gameManager.Settings.icoDefault);
        }
        if (_gameManager)
        {
            ico2.style.backgroundImage = new StyleBackground(_gameManager.Settings.icoDefault);
        }
        m_ProgressTime.style.backgroundColor = new StyleColor(_gameManager ? _gameManager.Theme.colorAccent : Color.blue);
    }
}
