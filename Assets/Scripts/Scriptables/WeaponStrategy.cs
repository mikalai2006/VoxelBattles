using UnityEngine;

// Абстрактный базовый класс для логики атаки
public abstract class WeaponStrategy : ScriptableObject
{
    [Header("Базовые настройки оружия")]
    public string weaponName;
    public float fireRate = 0.5f;

    //public abstract void ExecuteAttack(Transform firePoint, GameObject owner, LevelManager levelManager);
}