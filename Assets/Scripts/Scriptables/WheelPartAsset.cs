using UnityEngine;

/// <summary>
/// Ассет для создания конкретного типа колеса, диска или гусеничного сегмента.
/// </summary>
[CreateAssetMenu(fileName = "Wheel_New", menuName = "Vehicle System/Part Asset/Modular Wheel")]
public class WheelPartAsset : VehiclePartAsset
{
    // Класс чист, так как все базовые воксельные свойства унаследованы от VehiclePartAsset,
    // а логику движения рассчитывает сама подвеска (ChassisAsset).
}
