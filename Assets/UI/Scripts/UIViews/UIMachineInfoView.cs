// using System;
// using UnityEngine;
// using UnityEngine.Localization;
// using UnityEngine.Localization.Settings;
// using UnityEngine.UIElements;

// namespace UIToolkitLibrary
// {
//     /// <summary>
//     /// Manages inventory UI including filtering and selection state.
//     /// Filter dropdowns use localized strings.
//     /// </summary>
//     public class UIMachineInfoView : UIView
//     {
//         static class ClassNames
//         {
//             public static string InfoBox = "InfoBox";
//             public static string MachineInfoWrapper = "MachineInfoWrapper";
//             public static string ButtonClose = "ButtonClose";
//         }
//         VisualElement m_InfoBox;
//         VisualElement m_MachineInfoWrapper;
//         Button m_ButtonClose;

//         public UIMachineInfoView(VisualElement topElement, LocalizedStringTable localization) : base(topElement, localization)
//         {
//             // InventoryEvents.GearItemClicked += OnGearItemClicked;
//             // InventoryEvents.InventorySetup += OnInventorySetup;
//             // InventoryEvents.InventoryUpdated += OnInventoryUpdated;
//             GarageUIEvents.MachineItemClicked += OnClickMachineItem;
//             LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

//             // m_GearItemAsset = Resources.Load("GearItem") as VisualTreeAsset;
//         }

//         private void OnClickMachineItem(GameMachine machine)
//         {
//             Show();

//             var machineInfo = DrawCurrentMachine(machine);
//             m_InfoBox.Clear();
//             m_InfoBox.Add(machineInfo);
//             Color col = _gameManager.Theme.colorListItemBg;
//             // m_InfoBox.style.height = Screen.height;
//             // m_InfoBox.style.width = Screen.width;
//             // col.a = 0.7f;
//             m_InfoBox.style.backgroundColor = new StyleColor(col);

//             Vector2 centerScreen = new Vector2(Mathf.Min(1200+100, Screen.width)/2, 300); //new Vector2(Mathf.Min(1200, Screen.width)/2, Mathf.Min(800,Screen.height)/2);

//             // VisualElement boxBtn = new VisualElement();
//             // boxBtn.style.position = Position.Relative;
//             int numPoints = 6;
//             float radius = centerScreen.y-50;
//             // Рисование кнопок по окружности.
//             for (int i = 0; i < numPoints; i++)
//             {
//                 double angle = 2 * Math.PI * i / numPoints;
//                 int x = (int)(centerScreen.x + radius * Math.Cos(angle));
//                 int y = (int)(centerScreen.y + radius * Math.Sin(angle));

//                 // Создание кнопки.
//                 Button btn = new Button();
//                 btn.style.position = Position.Absolute;
//                 btn.style.left = x - 2;
//                 btn.style.top = y - 2;
//                 // btn.style.translate =new Translate(new Length(x, LengthUnit.Pixel), new Length(y, LengthUnit.Pixel));
//                 btn.style.width = 100;
//                 btn.style.height = 100;
//                 // g.DrawRectangle(Pens.Red, x - 2, y - 2, 5, 5);
//             Debug.Log($"{x}, {y}, {angle}");
//                 m_InfoBox.Add(btn);
//             }
//                 // m_InfoBox.Add(boxBtn);


//             Debug.Log($"{Screen.width}, {Screen.height}, {centerScreen}");
//         }

//         void OnSelectedLocaleChanged(Locale obj)
//         {
//             UpdateLocalizedText();
//         }

//         public override void Dispose()
//         {
//             base.Dispose();
//             // InventoryEvents.GearItemClicked -= OnGearItemClicked;
//             // InventoryEvents.InventorySetup -= OnInventorySetup;
//             // InventoryEvents.InventoryUpdated -= OnInventoryUpdated;
            
//             GarageUIEvents.MachineItemClicked -= OnClickMachineItem;
//             LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            
//             UnregisterButtonCallbacks();
//         }
        
        
//         protected override void SetVisualElements()
//         {
//             base.SetVisualElements();

//             m_InfoBox = Root.Q<VisualElement>(ClassNames.InfoBox);

//             m_MachineInfoWrapper = Root.Q<VisualElement>(ClassNames.MachineInfoWrapper);
//             m_ButtonClose = Root.Q<Button>(ClassNames.ButtonClose);
//             m_ButtonClose.RegisterCallback<ClickEvent>(OnClickClose);


//             UpdateLocalizedText();
//         }

//         private void OnClickClose(ClickEvent evt)
//         {
//             Hide();
//             Debug.Log($"Click on the close button");
//         }

//         private VisualElement DrawCurrentMachine(GameMachine machine)
//         {
//             VisualElement mBox = new VisualElement();
//             mBox.style.flexGrow = 0;
//             mBox.style.flexShrink = 0;
//             mBox.style.justifyContent = Justify.Center;
//             mBox.style.alignContent = Align.Center;
//             mBox.style.alignItems = Align.Center;
//             mBox.style.alignSelf = Align.Center;
//             mBox.name = "Machine";

//             // float pixelsPerUnit = machine.body.spriteBody.pixelsPerUnit;
//             // var widthMachine = machine.body.spriteBody.bounds.size.x * pixelsPerUnit;
//             // var heightMachine = machine.body.spriteBody.bounds.size.y * pixelsPerUnit;

//             // // draw caterpillar.
//             // for (int j = 0; j < machine.catterpillars.Count; j++)
//             // {
//             //     var catConfig = machine.catterpillars[j];
//             //     VisualElement vC = new VisualElement();
//             //     vC.name = "Caterpillar";
//             //     vC.style.justifyContent = Justify.Center;
//             //     vC.style.alignContent = Align.Center;
//             //     vC.style.alignItems = Align.Center;
//             //     vC.style.alignSelf = Align.Center;
//             //     vC.style.flexShrink = 0;
//             //     vC.style.flexGrow = 0;
//             //     vC.style.justifyContent = Justify.Center;
//             //     vC.style.alignContent = Align.Center;

//             //     vC.style.backgroundImage = new StyleBackground(catConfig.Config.sprite);
//             //     float pixelsPerUnit3 = catConfig.Config.sprite.pixelsPerUnit;
//             //     vC.style.width = catConfig.Config.sprite.bounds.size.x * pixelsPerUnit3;
//             //     vC.style.height = catConfig.Config.sprite.bounds.size.y * pixelsPerUnit3;
//             //     vC.style.position = Position.Absolute;
//             //     vC.style.translate = new Translate(
//             //         new Length((catConfig.offsetCat.x * pixelsPerUnit3), LengthUnit.Pixel),
//             //         new Length(catConfig.offsetCat.y * pixelsPerUnit3, LengthUnit.Pixel)
//             //     );
//             //     vC.style.unityBackgroundImageTintColor = new StyleColor(catConfig.colorCat);

//             //     mBox.Add(vC);
//             // }

//             // // draw body.
//             // VisualElement mBody = new VisualElement();
//             // mBody.name = "Body";
//             // mBody.style.backgroundImage = new StyleBackground(machine.body.spriteBody);
//             // mBody.style.width = mBox.style.width = widthMachine;
//             // mBody.style.height = mBox.style.height = heightMachine;
//             // mBody.style.unityBackgroundImageTintColor = new StyleColor(machine.colorBody);
//             // mBox.Add(mBody);

//             // // draw towers.
//             // for (int i = 0; i < machine.towers.Count; i++)
//             // {
//             //     var towerConfig = machine.towers[i];
//             //     VisualElement vE = new VisualElement();
//             //     vE.name = "Tower";
//             //     vE.style.flexShrink = 0;
//             //     vE.style.flexGrow = 0;
//             //     vE.style.justifyContent = Justify.Center;
//             //     vE.style.alignContent = Align.Center;

//             //     vE.style.backgroundImage = new StyleBackground(towerConfig.Config.spriteTower);
//             //     float pixelsPerUnit2 = towerConfig.Config.spriteTower.pixelsPerUnit;
//             //     vE.style.width = towerConfig.Config.spriteTower.bounds.size.x * pixelsPerUnit2;
//             //     vE.style.height = towerConfig.Config.spriteTower.bounds.size.y * pixelsPerUnit2;
//             //     vE.style.position = Position.Absolute;
//             //     // vE.style.left = 0;
//             //     // vE.style.right = 0;
//             //     // vE.style.top = 0;
//             //     // vE.style.bottom = 0;
//             //     var xTower = towerConfig.offsetTower.x * pixelsPerUnit2;
//             //     var yTower = towerConfig.offsetTower.y * pixelsPerUnit2;
//             //     vE.style.translate = new Translate(new Length(xTower, LengthUnit.Pixel), new Length(yTower, LengthUnit.Pixel));
//             //     vE.style.unityBackgroundImageTintColor = new StyleColor(towerConfig.colorTower);


//             //     for (int j = 0; j < towerConfig.muzzles.Count; j++)
//             //     {
//             //         var muzzleConfig = towerConfig.muzzles[j];
//             //         VisualElement vM = new VisualElement();
//             //         vM.name = "Muzzle";
//             //         vM.style.flexShrink = 0;
//             //         vM.style.flexGrow = 0;
//             //         vM.style.justifyContent = Justify.Center;
//             //         vM.style.alignContent = Align.Center;

//             //         vM.style.backgroundImage = new StyleBackground(muzzleConfig.Config.spriteMuzzle);
//             //         float pixelsPerUnit3 = muzzleConfig.Config.spriteMuzzle.pixelsPerUnit;
//             //         vM.style.width = muzzleConfig.Config.spriteMuzzle.bounds.size.x * pixelsPerUnit3;
//             //         vM.style.height = muzzleConfig.Config.spriteMuzzle.bounds.size.y * pixelsPerUnit3;
//             //         Debug.Log($"muzzle: {muzzleConfig.Config.spriteMuzzle.bounds.size}/{muzzleConfig.Config.spriteMuzzle.pixelsPerUnit}");
//             //         vM.style.position = Position.Absolute;
//             //         vM.style.translate = new Translate(
//             //             new Length((muzzleConfig.offsetMuzzle.x * pixelsPerUnit3) + (towerConfig.Config.spriteTower.bounds.size.x /2 * pixelsPerUnit2), LengthUnit.Pixel),
//             //             new Length(muzzleConfig.offsetMuzzle.y * pixelsPerUnit3, LengthUnit.Pixel)
//             //         );
//             //         vM.style.unityBackgroundImageTintColor = new StyleColor(muzzleConfig.Config.color);

//             //         vE.Add(vM);
//             //     }


//             //     mBox.Add(vE);
//             // }

//             return mBox;
//         }

//         protected override void RegisterButtonCallbacks()
//         {
//             // m_InventoryBackButton.RegisterCallback<ClickEvent>(CloseWindow);

//             // register callbacks when value in a dropdown field changes
//             // m_InventoryRarityDropdown.RegisterValueChangedCallback(UpdateFilters);
//             // m_InventorySlotTypeDropdown.RegisterValueChangedCallback(UpdateFilters);
//         }

//         // Optional: Unregistering the button callbacks is not strictly necessary
//         // in most cases and depends on your application's lifecycle management.
//         // You can choose to unregister them if needed for specific scenarios.
//         protected void UnregisterButtonCallbacks()
//         {
//             // m_InventoryBackButton.UnregisterCallback<ClickEvent>(CloseWindow);

//             // register callbacks when value in a dropdown field changes
//             // m_InventoryRarityDropdown.UnregisterValueChangedCallback(UpdateFilters);
//             // m_InventorySlotTypeDropdown.UnregisterValueChangedCallback(UpdateFilters);
//         }

//         // // convert string to Rarity enum
//         // Rarity GetRarity(string rarityString)
//         // {

//         //     Rarity rarity = Rarity.Common;

//         //     if (!Enum.TryParse<Rarity>(rarityString, out rarity))
//         //     {
//         //         Debug.Log("String " + rarityString + " failed to convert");
//         //     }
//         //     return rarity;
//         // }

//         // convert string to EquipmentType enum
//         // EquipmentType GetGearType(string gearTypeString)
//         // {

//         //     EquipmentType gearType = EquipmentType.Weapon;

//         //     if (!Enum.TryParse<EquipmentType>(gearTypeString, out gearType))
//         //     {
//         //         Debug.LogWarning("Converted " + gearTypeString + " failed to convert");
//         //     }
//         //     return gearType;
//         // }

       
//         /// <summary>
//         /// Updates filters based on dropdown selection. Uses array indices rather than string values
//         /// to maintain correct mapping to localized display text.
//         /// </summary>
//         // void UpdateFilters(ChangeEvent<string> evt)
//         // {
//         //     string gearTypeKey = SlotTypeKeys[m_InventorySlotTypeDropdown.index];
//         //     string rarityKey = RarityKeys[m_InventoryRarityDropdown.index];
        
//         //     EquipmentType gearType = GetGearType(gearTypeKey);
//         //     Rarity rarity = GetRarity(rarityKey);
        
//         //     InventoryEvents.GearFiltered?.Invoke(rarity, gearType);
//         // }

//         // loop through the available slots and create a button for each gear item
//         // void ShowGearItems(List<EquipmentSO> gearToShow)
//         // {

//         //     // Find the element under the ScrollView to store gear item buttons and clear existing inventory
//         //     VisualElement contentContainer = m_ScrollViewParent.Q<VisualElement>("unity-content-container");
//         //     contentContainer.Clear();

//         //     for (int i = 0; i < gearToShow.Count; i++)
//         //     {
//         //         CreateGearItemButton(gearToShow[i], contentContainer);
//         //     }
//         // }

//         // generate one item for the inventory and add a clickable button to select it
//         // void CreateGearItemButton(EquipmentSO gearData, VisualElement container)
//         // {
//         //     if (container == null)
//         //     {
//         //         Debug.Log("InventoryScreen.CreateGearItemButton: missing parent element");
//         //         return;
//         //     }

//         //     TemplateContainer gearUIElement = m_GearItemAsset.Instantiate();
//         //     gearUIElement.AddToClassList("gear-item-spacing");

//         //     GearItemComponent gearItem = new GearItemComponent(gearData);

//         //     // set visual element for gearItemComponent
//         //     gearItem.SetVisualElements(gearUIElement);
//         //     gearItem.SetGameData(gearUIElement);
//         //     gearItem.RegisterButtonCallbacks();

//         //     // add to the parent element
//         //     container.Add(gearUIElement);
//         // }

//         // select or deselect an item
//         // void SelectGearItem(GearItemComponent gearItem, bool state)
//         // {
//         //     if (gearItem == null)
//         //         return;

//         //     m_SelectedGear = (state) ? gearItem : null;
//         //     gearItem.CheckItem(state);
//         // }

//         // methods to hide and show the screen
//         public override void Show()
//         {
//             base.Show();

//             // add short transition
//             m_MachineInfoWrapper.style.scale = new Vector3(0.1f, 0.1f, 0.1f);
//             m_MachineInfoWrapper.experimental.animation.Scale(1f, 200);
//         }

//         void CloseWindow(ClickEvent evt)
//         {
//             Hide();
//         }

//         public override void Hide()
//         {
//             base.Hide();

//             // AudioManager.PlayDefaultButtonSound();

//             // // set the selected Gear, notify the InventoryScreenController
//             // if (m_SelectedGear != null)
//             //     InventoryEvents.GearSelected?.Invoke(m_SelectedGear.GearData);

//             // m_SelectedGear = null;

//         }

//         // event handling methods
//         void OnInventorySetup()
//         {
//             SetVisualElements();
//             RegisterButtonCallbacks();
//         }

//         // Load a list of Equipment ScriptableObjects to show in the Inventory
//         // void OnInventoryUpdated(List<EquipmentSO> gearToLoad)
//         // {
//         //     ShowGearItems(gearToLoad);
//         // }

//         // Add a check mark on a GearItem to show selection
//         // void OnGearItemClicked(GearItemComponent gearItem)
//         // {

//         //     AudioManager.PlayAltButtonSound();

//         //     // deselect previously selected
//         //     SelectGearItem(m_SelectedGear, false);

//         //     // select the new gear item
//         //     SelectGearItem(gearItem, true);
//         // }

//         void UpdateLocalizedText()
//         {
            
            
//             // if (m_InventoryRarityDropdown == null || m_InventorySlotTypeDropdown == null)
//             //     return;

//             // // Update Rarity dropdown using an extension method
//             // string[] rarityChoices = new string[]
//             // {
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_All"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Common"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Rare"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_Rarity_Special")
//             // };
//             // m_InventoryRarityDropdown.UpdateLocalizedChoices(rarityChoices, RarityKeys[m_InventoryRarityDropdown.index], RarityKeys);

//             // // Update Slot Type dropdown using an extension method
//             // string[] slotTypeChoices = new string[]
//             // {
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_All"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Weapon"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Shield"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Helmet"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Boots"),
//             //     LocalizationSettings.StringDatabase.GetLocalizedString("SettingsTable", "Inventory_SlotType_Gloves")
//             // };
//             // m_InventorySlotTypeDropdown.UpdateLocalizedChoices( slotTypeChoices, SlotTypeKeys[m_InventorySlotTypeDropdown.index], SlotTypeKeys);

//         }
        
//     }
// }