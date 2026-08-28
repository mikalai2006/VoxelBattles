using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Vehicle_Preset_Wheels_", menuName = "Vehicle System/Wheels Preset")]
public class VehiclePresetWheelAsset : ScriptableObject
{
    [Space(5)]
    [Header("Основные параметры")]
    public float moveSpeed = 15f;
    public float rotationSpeed = 360f;

    [Space(5)]
    [Header("Конфигурация ходовых слотов подвески")]
    [Tooltip("Список индивидуально настраиваемых колесных слотов для этой подвески")]
    public List<WheelSlotConfig> wheelSlots = new List<WheelSlotConfig>();
}


/// <summary>
/// Ассет для создания рамы, моста или подвески ходовой части машины.
/// </summary>
[System.Serializable]
public struct WheelSlotConfig
{
    [Tooltip("(Смещение) Координаты крепления ходового модуля в вокселях от центра подвески")]
    public Vector3 offsetInVoxels;

    [Tooltip("Конкретный тип колеса или гусеничного модуля для ЭТОЙ точки крепления")]
    public WheelPartAsset wheelPartAsset;

    [Tooltip("Нужно ли визуально вращать это колесо (или катки внутри него) при движении машины?")]
    public bool isRotatable;
}