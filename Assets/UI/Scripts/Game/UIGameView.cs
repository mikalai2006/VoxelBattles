using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitLibrary
{
    public class UIGameView: MonoBehaviour
    {
        public UIDocument m_UIDoc;
        public VisualElement m_Wrapper {get; private set; }
        public Button m_ButtonExit {get; private set; }
        [SerializeField] VisualElement m_BonusBox;
        public VisualElement m_InfoBox {get; private set; }
        public VisualElement m_ScreenElements {get; private set; }
        List<VisualElement> indicators;

        void Awake()
        {
            indicators = new();
        }

        void Start()
        {
            m_Wrapper = m_UIDoc.rootVisualElement.Q<VisualElement>(UINames.VisualElementWrapper);
            m_ButtonExit = m_Wrapper.Q<Button>(UINames.ButtonExitGame);
            
            m_BonusBox = m_Wrapper.Q<VisualElement>(UINames.TopSideBarBonusBox);
            m_InfoBox = m_Wrapper.Q<VisualElement>(UINames.TopSideBarInfoBox);
            m_ScreenElements = m_Wrapper.Q<VisualElement>(UINames.ScreenElements);
        }

        public IndicatorComponent AddIndicator()
        {
           IndicatorComponent el = new IndicatorComponent() {
            name = $"Indicator_{indicators.Count}"
           };

           el.style.width = 50;
           el.style.height = 50;
           el.style.flexGrow = 0;
           el.style.flexShrink = 0;
           el.style.position = Position.Absolute;

        //    Label textField = new Label()
        //    {
        //        text = indicators.Count.ToString(),
        //    };
        //    el.Add(textField);

           m_Wrapper.Add(el);

           indicators.Add(el);

           return el;
        }

        public void RemoveIndicator(VisualElement element)
        {
            if (indicators.Contains(element))
            {
                indicators.Remove(element);
            }
        }
    }
}