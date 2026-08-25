using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UIToolkitLibrary
{
    public class UIShopView : UIView
    {
        static class ClassNames
        {
            public static string Name_Machine = "NameMachine";
            public static string Cost_Machine = "CostMachine";
        }

        Button m_Button_Prev;
        Button m_Button_Next;
        Button m_Button_Buy;
        VisualElement m_VisualElementInfoBox;
        List<VisualElement> m_SpritesArrow;

        public UIShopView(VisualElement topElement, LocalizedStringTable localization): base(topElement, localization)
        {


            // InventoryEvents.GearItemClicked += OnGearItemClicked;
            // InventoryEvents.InventorySetup += OnInventorySetup;
            // InventoryEvents.InventoryUpdated += OnInventoryUpdated;

            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            
            // m_GearItemAsset = Resources.Load("GearItem") as VisualTreeAsset;

            UIEvents.UIShopFocusMachine += FocusMachineInShop;
        }

        public override void Dispose()
        {
            base.Dispose();
            // InventoryEvents.GearItemClicked -= OnGearItemClicked;
            // InventoryEvents.InventorySetup -= OnInventorySetup;
            // InventoryEvents.InventoryUpdated -= OnInventoryUpdated;
            
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            
            UnregisterButtonCallbacks();

            UIEvents.UIShopFocusMachine -= FocusMachineInShop;
        }

        private void FocusMachineInShop(VehiclePresetAsset machine)
        {
            if (m_VisualElementInfoBox == null) {
                m_VisualElementInfoBox = m_TopElement.Q<VisualElement>(UINames.VisualElementInfoBox);
            }

            // отрисовка информации об активной машине.
            m_VisualElementInfoBox.Clear();

            VisualElement visualElementInfoMachine = new VisualElement();
            visualElementInfoMachine.style.flexDirection = FlexDirection.Row;
            
            VisualElement BoxNameMachine = new VisualElement();
            BoxNameMachine.AddToClassList("bg-accent");
            visualElementInfoMachine.Add(BoxNameMachine);
            
            Label nameMachine = new Label();
            nameMachine.AddToClassList("font");
            nameMachine.AddToClassList("text-primary");
            nameMachine.AddToClassList("text-lg");
            nameMachine.AddToClassList("font-bold");
            nameMachine.text = $"({machine.name})"; // machine.text.title.GetLocalizedString() + 
            BoxNameMachine.Add(nameMachine);
            
            Label costMachine = new Label();
            costMachine.AddToClassList("font");
            costMachine.AddToClassList("text-primary");
            costMachine.AddToClassList("text-lg");
            costMachine.AddToClassList("font-bold");
            costMachine.text = "1000";
            visualElementInfoMachine.Add(costMachine);

            m_VisualElementInfoBox.Add(visualElementInfoMachine);

            Theming(m_VisualElementInfoBox);
        }

        void OnSelectedLocaleChanged(Locale obj)
        {
            UpdateLocalizedText();
        }
        
        
        protected override void SetVisualElements()
        {
            base.SetVisualElements();
            
            m_Button_Next = m_TopElement.Q<Button>(UINames.ButtonNext);
            m_Button_Prev = m_TopElement.Q<Button>(UINames.ButtonPrev);
            m_Button_Buy = m_TopElement.Q<Button>(UINames.ButtonBuy);
            // m_ImageRenderTexture = m_TopElement.Q<Image>(UINames.ImageRenderTexture);
            // if (m_ImageRenderTexture != null)
            // {
            //     StyleBackground styleBackground = m_ImageRenderTexture.resolvedStyle.backgroundImage;
            //     renderTexture = styleBackground.value.renderTexture;
            // }

            
            UQueryBuilder<VisualElement> builder = new UQueryBuilder<VisualElement>(m_TopElement);
            m_SpritesArrow = builder.Name(UINames.SpriteArrow).ToList();

            // // create tabs.
            // m_Tabs = Root.Q<VisualElement>("Tabs");
            // var tabMachines = new Button();
            // tabMachines.AddToClassList("tabs-button");
            // tabMachines.text = await Helpers.GetLocaledString("tab_machines");
            // m_Tabs.Add(tabMachines);

            // m_MachineBox = m_TopElement.Q(ClassNames.MachineBox);
            // m_MachineBox.style.maxWidth = 128 * 5;
            // m_MachineBody = m_TopElement.Q(ClassNames.MachineBody);
            // m_MachineCaterpillar = m_TopElement.Q(ClassNames.MachineCaterpillar);
            // m_MachineTowers = m_TopElement.Q(ClassNames.MachineTowers);

            // for (int i = 0; i < _gameManager.Settings.machines.Count; i++)
            // {
            //     var machine = _gameManager.Settings.machines[i];

            //     var mBox = new Button();
            //     mBox.RegisterCallback<ClickEvent>((ClickEvent evt) => OnClickMachineItem(evt, machine));
            //     mBox.AddToClassList("list-item");
            //     // VisualElement GarageWrapper = Root.Q<VisualElement>("GarageWrapper");
            //     // Debug.Log($"{GarageWrapper.style.width.value.value}");
            //     // mBox.style.width = GarageWrapper.style.width.value.value / 2;
            //     mBox.style.marginBottom = _gameManager.Theme.margin / 2;
            //     mBox.style.marginTop = _gameManager.Theme.margin / 2;
            //     mBox.style.marginLeft = _gameManager.Theme.margin / 2;
            //     mBox.style.marginRight = _gameManager.Theme.margin / 2;
            //     mBox.style.paddingTop = _gameManager.Theme.padding;
            //     mBox.style.paddingBottom = _gameManager.Theme.padding;
            //     mBox.style.paddingLeft = _gameManager.Theme.padding;
            //     mBox.style.paddingRight = _gameManager.Theme.padding;
            //     mBox.style.flexDirection = FlexDirection.Row;
            //     mBox.style.backgroundColor = new StyleColor(_gameManager.Theme.colorListItemBg);

            //     if (i == 2)
            //     {
            //         Color colActive = _gameManager.Theme.colorActive;
            //         mBox.style.borderLeftColor = new StyleColor(colActive);
            //         mBox.style.borderRightColor = new StyleColor(colActive);
            //         mBox.style.borderTopColor = new StyleColor(colActive);
            //         mBox.style.borderBottomColor = new StyleColor(colActive);
            //         colActive.a = 0.1f;
            //         mBox.style.backgroundColor = new StyleColor(colActive);
            //     }



            //     // VisualElement mach = DrawCurrentMachine(machine);
            //     // // mach.style.marginTop = 25;
            //     // // mach.style.marginRight = 25;
            //     // mBox.Add(mach);

            //     // add rank image.
            //     VisualElement rankImage = new VisualElement();
            //     rankImage.style.position = Position.Absolute;
            //     rankImage.style.top = 5;
            //     rankImage.style.right = 0;
            //     rankImage.style.width = 40;
            //     rankImage.style.height = 40;
            //     rankImage.style.backgroundImage = new StyleBackground(machine.minRank.sprite);
            //     mBox.Add(rankImage);

            //     // draw machine description.
            //     var mBoxInfoMachine = new VisualElement();
            //     mBoxInfoMachine.style.position = Position.Absolute;
            //     mBoxInfoMachine.style.left = 5;
            //     mBoxInfoMachine.style.bottom = 0;
            //     var mBoxTitle = new Label();
            //     mBoxTitle.AddToClassList("list-item-title");
            //     mBoxTitle.text = machine.text.title.GetLocalizedString();
            //     mBoxInfoMachine.Add(mBoxTitle);
            //     // var mBoxDescription = new Label();
            //     // mBoxDescription.AddToClassList("description");
            //     // mBoxDescription.text = machine.text.description.GetLocalizedString();
            //     // mBoxInfoMachine.Add(mBoxDescription);
            //     mBox.Add(mBoxInfoMachine);
                
            //     if (i == 3)
            //     {
            //         Color colActive = _gameManager.Theme.colorBg;
            //         colActive.a = 0.9f;
            //         var blockElement = new VisualElement();
            //         blockElement.style.position = Position.Absolute;
            //         blockElement.style.left = -3;
            //         blockElement.style.right = -3;
            //         blockElement.style.top = -3;
            //         blockElement.style.bottom = -3;
            //         blockElement.style.backgroundColor = new StyleColor(colActive);
            //         blockElement.style.borderLeftColor = new StyleColor(colActive);
            //         blockElement.style.borderRightColor = new StyleColor(colActive);
            //         blockElement.style.borderTopColor = new StyleColor(colActive);
            //         blockElement.style.borderBottomColor = new StyleColor(colActive);
            //         blockElement.style.backgroundColor = new StyleColor(colActive);
            //         mBox.Add(blockElement);
            //     }

            //     m_MachineBox.Add(mBox);

            // }

            // m_InventoryPanel = m_TopElement.Q("inventory__screen");
            // m_InventoryRarityDropdown = m_TopElement.Q<DropdownField>("inventory__rarity-dropdown");
            // m_InventorySlotTypeDropdown = m_TopElement.Q<DropdownField>("inventory__slot-type-dropdown");

            // // define row elements under the scrollview
            // m_ScrollViewParent = m_TopElement.Q<ScrollView>("inventory__scrollview");

            UpdateLocalizedText();
            Theming();
        }

        private void OnClickMachineItem(ClickEvent evt, VehiclePresetAsset machine)
        {
            Debug.Log($"Click on the {machine.name}");
            
            GarageUIEvents.MachineItemClicked?.Invoke(machine);
        }

        private VisualElement DrawCurrentMachine(VehiclePresetAsset machine)
        {
            VisualElement mBox = new VisualElement();
            mBox.style.flexGrow = 0;
            mBox.style.flexShrink = 0;
            mBox.style.justifyContent = Justify.Center;
            mBox.style.alignContent = Align.Center;
            mBox.style.alignItems = Align.Center;
            mBox.style.alignSelf = Align.Center;
            mBox.name = "Machine";

            return mBox;
        }

        void ClickBuy(ClickEvent evt)
        {
            UIEvents.UIShopClickBuyMachine?.Invoke();
        }

        void ClickPrev(ClickEvent evt)
        {
            UIEvents.UIShopPrevMachine?.Invoke();
        }

        void ClickNext(ClickEvent evt)
        {
            UIEvents.UIShopNextMachine?.Invoke();
        }

        protected override void RegisterButtonCallbacks()
        {
            m_Button_Next.RegisterCallback<ClickEvent>(ClickNext);
            m_Button_Prev.RegisterCallback<ClickEvent>(ClickPrev);
            m_Button_Buy.RegisterCallback<ClickEvent>(ClickBuy);

            // if (m_ImageRenderTexture != null)
            // {
            //     m_ImageRenderTexture.RegisterCallback<PointerDownEvent>(OnPointerDownHandler);
            //     m_ImageRenderTexture.RegisterCallback<PointerUpEvent>(OnPointerUpHandler);
            //     m_ImageRenderTexture.RegisterCallback<ClickEvent>(OnClickHandler);
            //     m_ImageRenderTexture.RegisterCallback<PointerEnterEvent>(OnPointerEnterHandler);
            //     m_ImageRenderTexture.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveHandler);
            //     m_ImageRenderTexture.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            // }
            // m_InventoryBackButton.RegisterCallback<ClickEvent>(CloseWindow);

            // register callbacks when value in a dropdown field changes
            // m_InventoryRarityDropdown.RegisterValueChangedCallback(UpdateFilters);
            // m_InventorySlotTypeDropdown.RegisterValueChangedCallback(UpdateFilters);
        }

        // Optional: Unregistering the button callbacks is not strictly necessary
        // in most cases and depends on your application's lifecycle management.
        // You can choose to unregister them if needed for specific scenarios.
        protected void UnregisterButtonCallbacks()
        {
            m_Button_Next.UnregisterCallback<ClickEvent>(ClickNext);
            m_Button_Prev.UnregisterCallback<ClickEvent>(ClickPrev);
            m_Button_Buy.UnregisterCallback<ClickEvent>(ClickBuy);
            // m_InventoryBackButton.UnregisterCallback<ClickEvent>(CloseWindow);

            // register callbacks when value in a dropdown field changes
            // m_InventoryRarityDropdown.UnregisterValueChangedCallback(UpdateFilters);
            // m_InventorySlotTypeDropdown.UnregisterValueChangedCallback(UpdateFilters);
            
            // if (m_ImageRenderTexture != null)
            // {
            //     m_ImageRenderTexture.UnregisterCallback<PointerDownEvent>(OnPointerDownHandler);
            //     m_ImageRenderTexture.UnregisterCallback<PointerUpEvent>(OnPointerUpHandler);
            //     m_ImageRenderTexture.UnregisterCallback<ClickEvent>(OnClickHandler);
            //     m_ImageRenderTexture.UnregisterCallback<PointerEnterEvent>(OnPointerEnterHandler);
            //     m_ImageRenderTexture.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveHandler);
            //     m_ImageRenderTexture.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            // }
        }

        void CloseWindow(ClickEvent evt)
        {
            Hide();
        }

        public override void Hide()
        {
            base.Hide();

            // AudioManager.PlayDefaultButtonSound();

            // // set the selected Gear, notify the InventoryScreenController
            // if (m_SelectedGear != null)
            //     InventoryEvents.GearSelected?.Invoke(m_SelectedGear.GearData);

            // m_SelectedGear = null;

        }

        // event handling methods
        void OnSetup()
        {
            SetVisualElements();
            RegisterButtonCallbacks();
        }


        // private void OnPointerDownHandler(PointerDownEvent evt)
        // {
        //     Debug.Log("Pointer Down on element! Target: " + evt.target);
        //     // You can access pointer details like button index (0 for left, 1 for right) or position
        //     if (evt.button == 0) // Left mouse button
        //     {
        //         // Do something
        //     }
        // }
        
        // private void OnPointerUpHandler(PointerUpEvent evt)
        // {
        //     Debug.Log("Pointer Up on element! Target: " + evt.target);
        // }

        // private void OnClickHandler(ClickEvent evt)
        // {
        //     Debug.Log("Click Event fired! Target: " + evt.target);
        // }
        
        // private void OnPointerEnterHandler(PointerEnterEvent evt)
        // {
        //     Debug.Log("Pointer entered element!");
        // }

        // private void OnPointerLeaveHandler(PointerLeaveEvent evt)
        // {
        //     Debug.Log("Pointer left element!");
        // }
        // private void OnPointerMoveEvent(PointerMoveEvent evt)
        // {
        //     Debug.Log("Move! Target: " + evt.position);
        //     Vector2 localPosition = m_ImageRenderTexture.WorldToLocal(evt.position);
        //     Vector2 remappedPosition = RemapToRenderTextureSpace(localPosition);
        //     Debug.Log($"Move! localPosition: {localPosition}, remappedPosition: {remappedPosition}");
        // }

        // private Vector2 RemapToRenderTextureSpace(Vector2 localPosition)
        // {
        //     float elementWidth = m_ImageRenderTexture.resolvedStyle.width;
        //     float elementHeight = m_ImageRenderTexture.resolvedStyle.height;
        //     int renderTextureWidth = renderTexture.width;
        //     int renderTextureHeight = renderTexture.height;

        //     // Calculate scaling
        //     float scaleX = renderTextureWidth / elementWidth;
        //     float scaleY = renderTextureHeight / elementHeight;

        //     // Remap coordinates (UI Toolkit uses top-left origin, camera/render texture might use bottom-left. 
        //     // A common issue is needing to flip the Y axis)
        //     Vector2 remapped = new Vector2(
        //         localPosition.x * scaleX,
        //         renderTextureHeight - (localPosition.y * scaleY) // Flip Y
        //     );

        //     return remapped;
        // }

        void UpdateLocalizedText()
        {
            
            
            // if (m_InventoryRarityDropdown == null || m_InventorySlotTypeDropdown == null)
            //     return;

            // // Update Rarity dropdown using an extension method
            // string[] rarityChoices = new string[]
            // {
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_All"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Common"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Rare"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Special")
            // };
            // m_InventoryRarityDropdown.UpdateLocalizedChoices(rarityChoices, RarityKeys[m_InventoryRarityDropdown.index], RarityKeys);

            // // Update Slot Type dropdown using an extension method
            // string[] slotTypeChoices = new string[]
            // {
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_All"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Weapon"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Shield"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Helmet"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Boots"),
            //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Gloves")
            // };
            // m_InventorySlotTypeDropdown.UpdateLocalizedChoices( slotTypeChoices, SlotTypeKeys[m_InventorySlotTypeDropdown.index], SlotTypeKeys);

        }
        
        private void Theming()
        {
            foreach (var item in m_SpritesArrow)
            {
                item.style.backgroundImage = new StyleBackground(_gameManager.Theme.spriteArrow);
            }
        }
    }
}