
// Базовый класс для всех деталей
using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using UnityEngine;

public abstract class VehiclePartAsset : ScriptableObject
{
    [Space(5)]
    [Header("Общая информация и Логика")]
    [Tooltip("Текст локализации для UI")]
    public TextLocalize text;

    public string partName;
    public int bonusHealth = 50;
    public float mass = 10f;

    [Header("Настройки позициционирования")]
    [Tooltip("Базовое смещение детали")]
    public Vector3 baseOffset;

    //[Space(5)]
    //[Header("Настройки Меша (Воксели)")]
    //[Tooltip("Данные вокселей для этой детали")]
    //public SOVoxelData sOVoxelData;
    [Space(5)]
    [Header("Конфигурация Рендерера и Физики")]
    [Tooltip("Полный набор настроек для генерации меша, колайдеров и Rigidbody")]
    public MeshConfig meshConfig;

    [Tooltip("Модификаторы цветов палитры (скины, кастомизация)")]
    public List<ColorsModify> colorsModifies;
}
