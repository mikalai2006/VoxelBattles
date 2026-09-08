using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ассет для создания башни или поворотного основания пушки.
/// </summary>
[CreateAssetMenu(fileName = "Tower_Asset", menuName = "Vehicle System/Part Asset/Modular Tower")]
public class TowerPartAsset : VehiclePartAsset
{
    //[Space(5)]
    //[Header("Настройки крепления стволов")]
    //[Tooltip("Список координат (в вокселях) куда сборщик должен установить стволы относительно центра башни")]
    //public List<Vector3Int> barrelMountOffsets = new List<Vector3Int>();

    [Tooltip("Выбранный тип ствола пушки")]
    public List<MuzzlePartAsset> muzzles;
}
