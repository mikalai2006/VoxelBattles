using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletSettings_New", menuName = "Vehicle System/Weapon/Bullet Settings")]
public class BulletSettingsAsset : ScriptableObject
{
    //[Space(5)]
    //[Header("Основная информация")]
    //public TextLocalize text;

    [Space(5)]
    [Header("Настройки меша")]
    //public MeshConfig MeshConfig;
    public SOVoxelData SOVoxelData;
    public List<ColorsModify> colorsModifies;


    [Space(5)]
    [Header("Основные параметры")]
    [Tooltip("Скорость полета снаряда в метрах Unity")]
    public float speed = 50f;

    [Tooltip("Радиус сферы детекции (размер мяча)")]
    public float radius = 0.15f;

    [Tooltip("Максимальное время жизни пули в секундах, если она никуда не попала")]
    public float maxLifetime = 5f;

    [Tooltip("Радиус разрушения")]
    public int radiusExplode = 5;
}
