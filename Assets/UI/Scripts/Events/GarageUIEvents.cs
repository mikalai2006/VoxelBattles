using System;

namespace UIToolkitLibrary
{
    /// <summary>
    /// Public static delegates associated with the InventoryScreen/InventoryController.
    ///
    /// Note: these are "events" in the conceptual sense and not the strict C# sense.
    /// </summary>
    public static class GarageUIEvents 
    {
        // Event triggered when a gear item is clicked
        public static Action<VehiclePresetAsset> MachineItemClicked;

        // Event triggered when the inventory screen appears
        public static Action ScreenEnabled;
        public static Action<ColorModifyItem> ClickByColor;
        
        // public static Action<BaseMachine> OnFocusMachine;
        public static Action ClickButtonPrevMachine;
        public static Action ClickButtonNextMachine;
        public static Action ClickButtonRotate;
        public static Action ClickButtonSellActiveMachine;
        public static Action OpenColors;
        public static Action CloseColors;
        public static Action FillOk;
        public static Action FillCancel;
        // // Event triggered when selecting a gear item
        // public static Action<EquipmentSO> GearSelected;

        // // Event for updating the filtered gear items
        // public static Action<Rarity, EquipmentType> GearFiltered;

        // // Event for initial setup
        // public static Action InventorySetup;

        // // Event when refreshing the inventory
        // public static Action<List<EquipmentSO>> InventoryUpdated;

        // // Event for auto-equipping from Character Screen
        // public static Action<List<EquipmentSO>> GearAutoEquipped;
    }
}
