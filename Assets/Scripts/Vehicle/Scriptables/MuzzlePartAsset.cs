using UnityEngine;

/// <summary>
/// Ассет для создания стреляющего ствола. Сюда инкапсулирована боевая стратегия и аудио.
/// </summary>
[CreateAssetMenu(fileName = "Muzzle_Asset_", menuName = "Vehicle System/Part Asset/Modular Muzzle")]
public class MuzzlePartAsset : VehiclePartAsset
{
    [Space(5)]
    [Header("Боевые настройки ствола")]
    [Tooltip("Сменная стратегия атаки (Рейкаст, Снаряд, Лазер и т.д.)")]
    public WeaponStrategy attackStrategy;

    [Tooltip("Звук выстрела конкретно этого ствола")]
    public AudioClip soundShot;
}
