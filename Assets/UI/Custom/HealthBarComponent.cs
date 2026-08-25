using Unity.Properties;
using UnityEngine.UIElements;

namespace UIToolkitLibrary
{
    /// <summary>
    /// A custom VisualElement that displays a health bar with a title, showing current and maximum health as a
    /// progress bar.
    /// </summary>
    [UxmlElement]
    public partial class HealthBarComponent : VisualElement
    {
        /// <summary>
        /// Class names for USS styling
        /// </summary>
       static class ClassNames
       {
           public static string HealthBarBackground = "health-bar__background";
           public static string HealthBarProgress = "health-bar__progress";
           public static string HealthBarTitle = "health-bar__title";
           public static string HealthBarLabel = "health-bar__label";
           public static string HealthBarContainer = "health-bar__container";
           public static string HealthBarTitleBackground = "health-bar__title_background";
       }
       public static class IDNames
       {
            public static string HealthBarWrapper = "HealthBarWrapper";
            public static string HealthBarRank = "HealthBarRank";
            public static string HealthBarBackground = "HealthBarBackground";
            public static string HealthBarProgress = "HealthBarProgress";
            public static string HealthBarTitle = "HealthBarTitle";
            public static string HealthBarLabel = "HealthBarLabel";
            public static string HealthBarContainer = "HealthBarContainer";
            public static string HealthBarTitleBackground = "HealthBarTitleBackground";
            public static string HealthBarStat = "HealthBarStat";
       }

        // Backing fields for health values
       string m_HealthBarTitle;

       readonly Label m_TitleLabel;
       readonly Label m_HealthStat;
       VisualElement m_Progress;
       VisualElement m_Background;
       VisualElement m_TitleBackground;
       VisualElement m_Wrapper;
       VisualElement m_Rank;

       HealthData m_HealthData;
       
       [UxmlAttribute]
       public string HealthBarTitle
       {
           get => m_HealthBarTitle;
           set => m_TitleLabel.text = value;
       }

       [CreateProperty]
       public HealthData HealthData
       {
           get => (HealthData)dataSource;
           set => dataSource = value;
       }

       // Constructor initializes the health bar elements
       public HealthBarComponent()
       {
            // Add container class for overall styling
            AddToClassList(ClassNames.HealthBarContainer);
            // this.style.flexShrink = 1;
            style.width = new StyleLength(250);
            
            m_Wrapper = new VisualElement {name = IDNames.HealthBarWrapper};
            m_Wrapper.usageHints = UsageHints.GroupTransform;
            m_Wrapper.pickingMode = PickingMode.Ignore;
            m_Wrapper.style.flexDirection = FlexDirection.Row;
            Add(m_Wrapper);
            

            m_Rank = new VisualElement {name = IDNames.HealthBarRank};
            m_Rank.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Rank.pickingMode = PickingMode.Ignore;
            m_Rank.style.width = 50;
            m_Rank.style.height = 50;
            m_Wrapper.Add(m_Rank);

            var generalCell = new VisualElement();
            generalCell.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            generalCell.pickingMode = PickingMode.Ignore;
            generalCell.style.flexGrow = 1;

            // Title background element
            m_TitleBackground = new VisualElement {name = IDNames.HealthBarTitleBackground};
            m_TitleBackground.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_TitleBackground.pickingMode = PickingMode.Ignore;
            m_TitleBackground.AddToClassList(ClassNames.HealthBarTitleBackground);
            generalCell.Add(m_TitleBackground);
           
            // Title label
            m_TitleLabel = new Label() {name = IDNames.HealthBarTitle};
            m_TitleLabel.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_TitleLabel.pickingMode = PickingMode.Ignore;
            m_TitleLabel.AddToClassList(ClassNames.HealthBarTitle);
            m_TitleLabel.text = "Character Name Character Name ";
            m_TitleBackground.Add(m_TitleLabel);
           
            // Background element of the health bar
            m_Background = new VisualElement {name = IDNames.HealthBarBackground};
            m_Background.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Background.pickingMode = PickingMode.Ignore;
            m_Background.AddToClassList(ClassNames.HealthBarBackground);
            // m_Background.style.flexShrink = 0;
            
           generalCell.Add(m_Background);

            // Progress bar element showing current health
            m_Progress = new VisualElement {name = IDNames.HealthBarProgress};
            m_Progress.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Progress.pickingMode = PickingMode.Ignore;
            m_Progress.AddToClassList(ClassNames.HealthBarProgress);
            m_Progress.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_Background.Add(m_Progress);

            // Label displaying current and maximum health
            m_HealthStat = new Label() {name = IDNames.HealthBarStat};
            m_HealthStat.usageHints = UsageHints.DynamicTransform & UsageHints.DynamicColor;
            m_HealthStat.pickingMode = PickingMode.Ignore;
            m_HealthStat.AddToClassList(ClassNames.HealthBarLabel);
            m_HealthStat.text = "200/300";
            m_Progress.Add(m_HealthStat);

            m_Wrapper.Add(generalCell);

           BindElements();
       }

       void BindElements()
       {
           m_HealthStat.SetBinding("text", new DataBinding
           {
               dataSourcePath = new PropertyPath(nameof(HealthData.HealthStatText)),
               bindingMode = BindingMode.ToTarget
           });
            
           m_Progress.SetBinding("style.width", new DataBinding
           {
               dataSourcePath = new PropertyPath(nameof(HealthData.HealthProgressStyleLength)),
               bindingMode = BindingMode.ToTarget
           });
       }
    }
}
