using System;
using UIToolkitLibrary;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UIToolkitDemo
{
    // Shows the menu buttons
    public class MenuBarView : UIView
    {
        // Classes/selectors for toggling between active and inactive states
        const string k_LabelInactiveClass = "menu__label";
        const string k_LabelActiveClass = "menu__label--active";

        const string k_IconInactiveClass = "menu__icon";
        const string k_IconActiveClass = "menu__icon--active";

        const string k_ButtonInactiveClass = "menu__button";
        const string k_ButtonActiveClass = "menu__button--active";

        // Constants for marker movement animation
        const int k_MoveTime = 150;
        const float k_Spacing = 100f;
        const float k_yOffset = -8f;

        // UI Buttons
        Button m_GameScreenMenuButton;
        // Button m_HomeScreenMenuButton;
        Button m_CharScreenMenuButton;
        Button m_InfoScreenMenuButton;
        Button m_ShopScreenMenuButton;
        Button m_GarageScreenMenuButton;

        Button m_ActiveButton;
        bool m_InterruptAnimation;

        // Represents the currently active menu
        VisualElement m_MenuMarker;
        VisualElement m_SpriteAngar;
        VisualElement m_SpriteBattle;
        VisualElement m_SpriteShop;

        public MenuBarView(VisualElement topElement, LocalizedStringTable localization) : base(topElement, localization)
        {
            // Listen for aspect ratio / viewport changes
            // MediaQueryEvents.AspectRatioUpdated += OnAspectRatioUpdated;

            // Update the active menu marker if clicking the OptionsBarView's gold/gem icons
            MainMenuUIEvents.OptionsBarShopScreenShown += OnOptionsBarShopScreenShown;
            UIEvents.ClickShopButtonNotMenuBar += ShowShop;
        }

        public override void Dispose()
        {
            base.Dispose();

            // Unsubscribe from events
            // MediaQueryEvents.AspectRatioUpdated -= OnAspectRatioUpdated;
            MainMenuUIEvents.OptionsBarShopScreenShown -= OnOptionsBarShopScreenShown;
            UIEvents.ClickShopButtonNotMenuBar -= ShowShop;

            UnregisterButtonCallbacks();
        }

        // Set up Menu elements
        protected override void SetVisualElements()
        {
            base.SetVisualElements();

            m_GameScreenMenuButton = m_TopElement.Q<Button>("menu__game-button");
            // m_HomeScreenMenuButton = m_TopElement.Q<Button>("menu__home-button");
            // m_CharScreenMenuButton = m_TopElement.Q<Button>("menu__char-button");
            // m_InfoScreenMenuButton = m_TopElement.Q<Button>("menu__info-button");
            m_ShopScreenMenuButton = m_TopElement.Q<Button>("menu__shop-button");
            m_GarageScreenMenuButton = m_TopElement.Q<Button>("menu__garage-button");

            m_SpriteShop = m_TopElement.Q<VisualElement>(UINames.SpriteShop);
            m_SpriteAngar = m_TopElement.Q<VisualElement>(UINames.SpriteAngar);
            m_SpriteBattle = m_TopElement.Q<VisualElement>(UINames.SpriteBattle);

            m_MenuMarker = m_TopElement.Q("menu__current-marker");

            Theming();
        }

        // Register callbacks for button clicks
        protected override void RegisterButtonCallbacks()
        {
            base.RegisterButtonCallbacks();

            // Register action when each button is clicked
            m_GameScreenMenuButton.RegisterCallback<ClickEvent>(ClickGameButton);
            // m_HomeScreenMenuButton.RegisterCallback<ClickEvent>(ClickHomeButton);
            // m_CharScreenMenuButton.RegisterCallback<ClickEvent>(ClickCharButton);
            // m_InfoScreenMenuButton.RegisterCallback<ClickEvent>(ClickInfoButton);
            m_ShopScreenMenuButton.RegisterCallback<ClickEvent>(ClickShopButton);
            m_GarageScreenMenuButton.RegisterCallback<ClickEvent>(ClickGarageButton);

            // Waits for interface to build (GeometryChangedEvent), otherwise marker can miss target
            // m_MenuMarker.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        // Optional: Unregistering the button callbacks is not strictly necessary
        // in most cases and depends on your application's lifecycle management.
        // You can choose to unregister them if needed for specific scenarios.
        protected void UnregisterButtonCallbacks()
        {
            // m_HomeScreenMenuButton.UnregisterCallback<ClickEvent>(ClickHomeButton);
            // m_CharScreenMenuButton.UnregisterCallback<ClickEvent>(ClickCharButton);
            // m_InfoScreenMenuButton.UnregisterCallback<ClickEvent>(ClickInfoButton);
            m_ShopScreenMenuButton.UnregisterCallback<ClickEvent>(ClickShopButton);
            m_GarageScreenMenuButton.UnregisterCallback<ClickEvent>(ClickGarageButton);

            // m_MenuMarker.UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        // Notify the MainMenuUIManager to show a MenuScreen. Highlight the clicked Button and
        // move the active marker to that button

        void ClickHomeButton(ClickEvent evt)
        {
            // ActivateButton(m_HomeScreenMenuButton);
            MainMenuUIEvents.HomeScreenShown?.Invoke();
          
            // MoveMarkerToClick(evt);
            _gameManager.ChangeState(GameState.ChangeMenuItem);
        }

        void ClickGameButton(ClickEvent evt)
        {
            // ActivateButton(m_HomeScreenMenuButton);
            
            // Показываем сцену игрового процесса.
            MainMenuUIEvents.GameScreenShown?.Invoke();
          
            // MoveMarkerToClick(evt);
            _gameManager.ChangeState(GameState.ChangeMenuItem);
        }
        void ClickCharButton(ClickEvent evt)
        {
            ActivateButton(m_CharScreenMenuButton);
            MainMenuUIEvents.CharScreenShown?.Invoke();
            // MoveMarkerToClick(evt);

        }
        void ClickInfoButton(ClickEvent evt)
        {
            ActivateButton(m_InfoScreenMenuButton);
            MainMenuUIEvents.InfoScreenShown?.Invoke();
            // MoveMarkerToClick(evt);
        }

        void ClickShopButton(ClickEvent evt)
        {
            ShowShop();
        }

        void ShowShop()
        {
            MainMenuUIEvents.ShopScreenShown?.Invoke();
            ActivateButton(m_ShopScreenMenuButton);
            // MoveMarkerToClick(evt);
            
            _gameManager.ChangeState(GameState.ChangeMenuItem);
        }

        void ClickGarageButton(ClickEvent evt)
        {
            MainMenuUIEvents.GarageScreenShown?.Invoke();
            ActivateButton(m_GarageScreenMenuButton);
            // MoveMarkerToClick(evt);

            _gameManager.ChangeState(GameState.ChangeMenuItem);
        }

        // Activate a button, highlight its label and icon
        void ActivateButton(Button menuButton)
        {
            // Store this to refresh the marker if switching aspect ratios
            m_ActiveButton = menuButton;

            HighlightElement(menuButton, k_ButtonInactiveClass, k_ButtonActiveClass, m_TopElement);

            // Enable the label and disable others
            Label label = menuButton.Q<Label>(className: k_LabelInactiveClass);
            HighlightElement(label, k_LabelInactiveClass, k_LabelActiveClass, m_TopElement);

            // Enable the icon and disable others
            VisualElement icon = menuButton.Q<VisualElement>(className: k_IconInactiveClass);
            HighlightElement(icon, k_IconInactiveClass, k_IconActiveClass, m_TopElement);

        }

        //  Move the marker when a target VisualElement is clicked
        void MoveMarkerToClick(ClickEvent evt)
        {
            // Move the marker when we click the target VisualElement and the ClickEvent has already been processed; wait for the "BubbleUp" phase
            // to make sure the target element has fully processed the event
            if (evt.propagationPhase == PropagationPhase.BubbleUp)
            {
                MoveMarkerToElement(evt.target as VisualElement);
            }
            // AudioManager.PlayDefaultButtonSound();
        }

        // Move the active marker to a target VisualElement
        void MoveMarkerToElement(VisualElement targetElement)
        {

            // world space position
            Vector2 targetInWorldSpace = targetElement.parent.LocalToWorld(targetElement.layout.position);

            // convert to local space of menu marker's parent
            Vector3 targetInRootSpace = m_MenuMarker.parent.WorldToLocal(targetInWorldSpace);

            // difference between image sizes
            Vector3 offset = new Vector3(0f, targetElement.parent.layout.height - targetElement.layout.height + k_yOffset, 0f);

            Vector3 newPosition = targetInRootSpace - offset;

            // If the aspect ratio/theme changes, use a duration of 0; otherwise, calculation based on distance
            int duration = m_InterruptAnimation ? 0 : CalculateDuration(newPosition);

            // Use the tweening tool for animation
            m_MenuMarker.experimental.animation.Position(targetInRootSpace - offset, duration);

        }

        // Calculate duration for marker animation based on distance
        int CalculateDuration(Vector3 newPosition)
        {
            // add extra animation time if moving more than one space 
            Vector3 delta = m_MenuMarker.resolvedStyle.translate - newPosition;

            float distanceInPixels = Mathf.Abs(delta.y / k_Spacing);

            int duration = Mathf.Clamp((int)distanceInPixels * k_MoveTime, k_MoveTime, k_MoveTime * 4);
            return duration;
        }

        // Enable the Highlight style for currently selected button/element
        void HighlightElement(VisualElement targetElement, string inactiveClass, string activeClass, VisualElement root)
        {
            if (targetElement == null)
                return;

            // Locate the currently active element 
            VisualElement currentSelection = root.Query<VisualElement>(className: activeClass);

            // If we have selected what is already currently active, do nothing
            if (currentSelection == targetElement)
            {
                return;
            }

            // De-highlight the currently active element
            if (currentSelection != null)
            {
                currentSelection.RemoveFromClassList(activeClass);
                currentSelection.AddToClassList(inactiveClass);
            }

            // Highlight the target element
            targetElement.RemoveFromClassList(inactiveClass);
            targetElement.AddToClassList(activeClass);
        }


        // Event-handling methods

        // When starting the scene or if switching aspect ratios
        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {

            if (m_ActiveButton == null)
                m_ActiveButton = m_GameScreenMenuButton;

            ActivateButton(m_ActiveButton);
            MoveMarkerToElement(m_ActiveButton);
        }

        // Interrupt animation and move the marker to active button when switching aspect ratios
        // void OnAspectRatioUpdated(MediaAspectRatio newAspectRatio)
        // {
        //     m_InterruptAnimation = true;

        //     if (m_ActiveButton != null)
        //         MoveMarkerToElement(m_ActiveButton);

        //     m_InterruptAnimation = false;
        // }

        // Open the ShopScreen from the OptionsBar. Activate the Button and move the marker without
        // a ClickEvent.
        private void OnOptionsBarShopScreenShown()
        {
            // Invoke the event to switch to the shop screen
            MainMenuUIEvents.ShopScreenShown?.Invoke();

            // Activate the corresponding menu button
            ActivateButton(m_ShopScreenMenuButton);

            // Move the marker to the 'Shop' button
            MoveMarkerToElement(m_ShopScreenMenuButton);
        }

        void Theming()
        {
            m_SpriteAngar.style.backgroundImage = new StyleBackground(_gameManager.Theme.spriteAngar);
            m_SpriteShop.style.backgroundImage = new StyleBackground(_gameManager.Theme.spriteShop);
            m_SpriteBattle.style.backgroundImage = new StyleBackground(_gameManager.Theme.spriteBattle);
        }

    }
}