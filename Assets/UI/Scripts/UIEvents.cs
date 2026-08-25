using System;
using System.Collections.Generic;

namespace UIToolkitLibrary
{
    public static class UIEvents 
    {
        public static Action NeedCloseDialogs;

        public static Action<StateMachinePlayer> OnFocusMachineInGarage;
        public static Action<VehiclePresetAsset> ClickButtonBuyInShop;
        public static Action ClickButtonRepair;
        public static Action ClickButtonRepairByAdv;
        public static Action ClickButtonTower;
        public static Action ClickButtonTowerClose;
        public static Action ClickButtonBuyActiveTower;
        public static Action ClickButtonPrevTower;
        public static Action ClickButtonNextTower;
        //public static Action<GameTowerShop>  FocusTower;
        public static Action ClickShopButtonNotMenuBar;
        public static Action UIShopPrevMachine;
        public static Action UIShopNextMachine;
        public static Action UIShopClickBuyMachine;
        public static Action<VehiclePresetAsset> UIShopFocusMachine;
    }
}
