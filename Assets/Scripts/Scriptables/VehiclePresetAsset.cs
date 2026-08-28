using UnityEngine;

/// <summary>
/// Scriptable Object, хранящий готовую заводскую комплектацию машины.
/// Объединяет все необходимые детали в единый пресет (рецепт сборки).
/// </summary>
[CreateAssetMenu(fileName = "New_Vehicle_Preset", menuName = "Vehicle System/Vehicle Preset")]
public class VehiclePresetAsset : ScriptableObject
{
    [Header("Информация о пресете")]
    [Tooltip("Техническое название пресета техники")]
    public string presetName = "Default Tank";

    [Header("Комплектация Ходовой")]
    [Tooltip("Выбранный ассет подвески/рамы")]
    public ChassisAsset chassis;

    [Header("Комплектация Оружия")]
    [Tooltip("Выбранная оружейная башня")]
    public TowerPartAsset tower;

    [Tooltip("Выбранный тип ствола пушки")]
    public MuzzlePartAsset muzzle;

    [Header("Конфигурация ходовых слотов подвески")]
    [Tooltip("Пресет колес")]
    public VehiclePresetWheelAsset wheelsPreset;

    [Header("Настройки машины")]
    [Tooltip("Макс. скорость")]
    public float maxSpeed;
}
