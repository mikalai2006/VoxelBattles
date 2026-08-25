using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitLibrary
{
    /// <summary>
    /// Пользовательский визуальный элемент, отображающий основной UI (TopSideBar), 
    /// на игровой сцене
    /// </summary>
    [UxmlElement]
    public partial class TopSideBarComponent : VisualElement, IStyles
    {
        GameManager _gameManager => GameManager.Instance;
       static class ClassNames
       {
           public static string TopSideBarWrapper = "top-side-bar__wrapper";
           public static string TopSideBarPlayerBox = "top-side-bar__player-box";
           public static string TopSideBarPlayerName = "top-side-bar__player-name";
           public static string TopSideBarProgressBackground = "top-side-bar__progress-background";
           public static string TopSideBarRank = "top-side-bar__rank";
           public static string TopSideBarProgress = "top-side-bar__progress";
           public static string TopSideBarBtnExit = "top-side-bar__btn-exit";
           
       }
       public static class IDNames
       {
           public static string TopSideBarWrapper = "TopSideBarWrapper";
           public static string TopSideBarPlayerBox = "TopSideBarPlayerBox";
           public static string TopSideBarPlayerName = "TopSideBarPlayerName";
           public static string TopSideBarProgressBackground = "TopSideBarProgressBackground";
           public static string TopSideBarRank = "TopSideBarRank";
           public static string TopSideBarProgress = "TopSideBarProgress";
           public static string TopSideBarBtnExit = "TopSideBarBtnExit";
           public static string TopSideBarInfoBox = "TopSideBarInfoBox";
           public static string TopSideBarBonusBox = "TopSideBarBonusBox";
           public static string TopSideBarStat = "TopSideBarStat";
           
       }

        // Резервные поля для значений PlayerName
       string m_TopSideBarPlayerName;

       readonly Label m_PlayerName;
       readonly Label m_TopSideBarStat;
       VisualElement m_Progress;
       VisualElement m_Progress_Background;
       VisualElement m_Wrapper;
       VisualElement m_PlayerBox;
       public VisualElement m_Rank;
       VisualElement m_InfoBox;
       Button m_ButtonExit;
       public Button ButtonExit => m_ButtonExit;

    //    TopSideBarData m_TopSideBarData;
       
       [UxmlAttribute]
       public string TopSideBarPlayerName
       {
           get => m_TopSideBarPlayerName;
           set => m_PlayerName.text = value;
       }

       [CreateProperty]
       public TopSideBarData TopSideBarData
       {
           get => (TopSideBarData)dataSource;
           set => dataSource = value;
       }

       public TopSideBarComponent()
       {
            // // Add container class for overall styling
            // AddToClassList(ClassNames.TopSideBarContainer);
            // // this.style.flexShrink = 1;
            // style.width = new StyleLength(250);

            m_Wrapper = new VisualElement {name = IDNames.TopSideBarWrapper};
            m_Wrapper.usageHints = UsageHints.GroupTransform;
            m_Wrapper.AddToClassList(ClassNames.TopSideBarWrapper);
            m_Wrapper.style.flexDirection = FlexDirection.Row;
            m_Wrapper.style.marginTop = new StyleLength(25);
            m_Wrapper.style.paddingLeft = new StyleLength(25);
            m_Wrapper.style.paddingRight = new StyleLength(25);
            m_Wrapper.pickingMode = PickingMode.Ignore;
            Add(m_Wrapper);

            // Rank
            m_Rank = new VisualElement {name = IDNames.TopSideBarRank};
            m_Rank.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Rank.pickingMode = PickingMode.Ignore;
            m_Rank.AddToClassList(ClassNames.TopSideBarRank);
            m_Rank.style.width = 50;
            m_Rank.style.height = 50;
            m_Wrapper.Add(m_Rank);

#region PlayerBox
            m_PlayerBox = new VisualElement {name = IDNames.TopSideBarPlayerBox};
            m_PlayerBox.pickingMode = PickingMode.Ignore;
            m_PlayerBox.AddToClassList(ClassNames.TopSideBarPlayerBox);
            m_PlayerBox.style.width = new StyleLength(250);
            m_Wrapper.Add(m_PlayerBox);

            var mGrid = new VisualElement {name = "Grid"};
            m_PlayerBox.Add(mGrid);
           
            // Name
            m_PlayerName = new Label() {name = IDNames.TopSideBarPlayerName};
            m_PlayerName.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_PlayerName.pickingMode = PickingMode.Ignore;
            m_PlayerName.AddToClassList(ClassNames.TopSideBarPlayerName);
            m_PlayerName.text = "Character Name Very Long";
            m_PlayerName.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            mGrid.Add(m_PlayerName);

            // Progress
            m_Progress_Background = new VisualElement {name = IDNames.TopSideBarProgressBackground};
            m_Progress_Background.pickingMode = PickingMode.Ignore;
            m_Progress_Background.AddToClassList(ClassNames.TopSideBarProgressBackground);
            m_Progress_Background.style.paddingBottom = 3;
            m_Progress_Background.style.paddingTop = 3;
            m_Progress_Background.style.paddingLeft = 3;
            m_Progress_Background.style.paddingRight = 3;
            m_Progress_Background.style.flexGrow = 1;
            mGrid.Add(m_Progress_Background);

            // Progress bar element showing current TopSideBar
            m_Progress = new VisualElement {name = IDNames.TopSideBarProgress};
            m_Progress.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Progress.pickingMode = PickingMode.Ignore;
            m_Progress.AddToClassList(ClassNames.TopSideBarProgress);
            m_Progress.style.flexGrow = 1;
            m_Progress.style.flexShrink = 0;
            m_Progress.style.height = 20;
            m_Progress.style.width = new StyleLength(Length.Percent((1 - 0.3f) * 100));
            m_Progress_Background.Add(m_Progress);

            // TODO remove
            m_TopSideBarStat = new Label() {name = IDNames.TopSideBarStat};
            m_TopSideBarStat.usageHints = UsageHints.DynamicTransform;
            m_TopSideBarStat.pickingMode = PickingMode.Ignore;
            m_TopSideBarStat.text = "200/300";
            m_Progress.Add(m_TopSideBarStat);

#endregion

            var mGridButtons = new VisualElement {name = "Buttons"};
            mGridButtons.style.flexGrow = 1;
            mGridButtons.style.flexShrink = 0;
            mGridButtons.style.alignItems = Align.FlexEnd;
            m_Wrapper.Add(mGridButtons);

#region Schema
            var mSchema = new VisualElement {name = "Schema"};
            mSchema.style.position = Position.Absolute;
            
            mSchema.style.flexGrow = 1;
            mSchema.style.flexShrink = 0;
            mSchema.style.alignItems = Align.FlexEnd;
            mGridButtons.Add(mSchema);
#endregion


#region Buttons
            // Button Exit.
            m_ButtonExit = new Button {name = IDNames.TopSideBarBtnExit};
            m_ButtonExit.text = "Выход";
            m_ButtonExit.AddToClassList("button");
            m_ButtonExit.AddToClassList("button-primary");
            m_ButtonExit.AddToClassList(ClassNames.TopSideBarBtnExit);
            m_ButtonExit.pickingMode = PickingMode.Position;
            mGridButtons.Add(m_ButtonExit);
#endregion

            var mRowSubWrapper = new VisualElement {name = "SubWrapper"};
            mRowSubWrapper.pickingMode = PickingMode.Ignore;
            mRowSubWrapper.style.flexDirection = FlexDirection.Row;
            mRowSubWrapper.style.flexGrow = 1;
            Add(mRowSubWrapper);

#region BonusBox
            m_InfoBox = new VisualElement {name = IDNames.TopSideBarBonusBox};
            m_InfoBox.usageHints = UsageHints.GroupTransform;
            m_InfoBox.pickingMode = PickingMode.Ignore;
            // m_Wrapper.AddToClassList(ClassNames.TopSideBarWrapper);
            m_InfoBox.style.flexDirection = FlexDirection.Column;
            // m_InfoBox.style.backgroundColor = new StyleColor(color);
            m_InfoBox.style.marginTop = new StyleLength(25);
            m_InfoBox.style.paddingLeft = new StyleLength(25);
            m_InfoBox.style.paddingRight = new StyleLength(25);
            mRowSubWrapper.Add(m_InfoBox);
#endregion

            var mRowSubWrapperBox = new VisualElement();
            mRowSubWrapperBox.pickingMode = PickingMode.Ignore;
            mRowSubWrapperBox.style.flexGrow = 1;
            mRowSubWrapper.Add(mRowSubWrapperBox);

#region InfoBox
            m_InfoBox = new VisualElement {name = IDNames.TopSideBarInfoBox};
            m_InfoBox.pickingMode = PickingMode.Ignore;
            // m_Wrapper.AddToClassList(ClassNames.TopSideBarWrapper);
            m_InfoBox.style.flexDirection = FlexDirection.Column;
            // m_InfoBox.style.backgroundColor = new StyleColor(color);
            m_InfoBox.style.marginTop = new StyleLength(25);
            m_InfoBox.style.paddingLeft = new StyleLength(25);
            m_InfoBox.style.paddingRight = new StyleLength(25);
            mRowSubWrapper.Add(m_InfoBox);
#endregion

           BindElements();
           UpdateStyles();
       }

        public void UpdateStyles()
        {
            Color colorBg = Color.black;
            colorBg.a = 0.2f;

            m_PlayerBox.style.backgroundColor = new StyleColor(_gameManager ? _gameManager.Theme.colorWrapperGameScreen : colorBg);
            
            // m_Wrapper.style.backgroundColor = new StyleColor(colorBg);

            m_Progress_Background.style.backgroundColor = new StyleColor(colorBg);
            
            m_PlayerName.style.color = new StyleColor(Color.white);
            m_PlayerName.style.fontSize = 16;

            m_TopSideBarStat.style.fontSize = 8;
            
            m_Progress.style.backgroundColor = new StyleColor(Color.green);
        }

       void BindElements()
       {
           m_TopSideBarStat.SetBinding("text", new DataBinding
           {
               dataSourcePath = new PropertyPath(nameof(TopSideBarData.HealthStatText)),
               bindingMode = BindingMode.ToTarget
           });
            
           m_Progress.SetBinding("style.width", new DataBinding
           {
               dataSourcePath = new PropertyPath(nameof(TopSideBarData.HealthProgressStyleLength)),
               bindingMode = BindingMode.ToTarget
           });
       }
    }
}
