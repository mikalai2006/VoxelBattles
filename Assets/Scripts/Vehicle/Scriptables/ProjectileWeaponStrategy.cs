using Cysharp.Threading.Tasks;
using UnityEngine;

// Реализация конкретного оружия — обычные снаряды/пули
[CreateAssetMenu(fileName = "NewProjectileWeapon", menuName = "Vehicle System/Weapon Strategy/Projectile")]
public class ProjectileWeaponStrategy : WeaponStrategy
{
    public BulletSettingsAsset bulletSettings; // Перетаскиваем созданный SO настроек сюда

    public override void ExecuteAttack(Transform firePoint, GameObject owner, LevelManager levelManager)
    {
        if (levelManager == null) return;

        ExecuteAttackAsync(firePoint, owner, levelManager).Forget();
    }

    async UniTask ExecuteAttackAsync(Transform firePoint, GameObject owner, LevelManager levelManager)
    {
        //GameObject bullet = await levelManager.PoolBullet.GetObject();
        //bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
        ////Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        //if (bullet.TryGetComponent<Rigidbody>(out var rb))
        //{
        //    rb.linearVelocity = firePoint.forward * bulletSettings.speed;
        //}
        //Debug.Log($"[Выстрел] {weaponName} выпустил снаряд.");
        // Достаем пулю из пула
        if (levelManager.PoolBullet == null)
        {
            Debug.LogWarning("Не найден пул снарядов!");
            return;
        }

        GameObject bulletObj = await levelManager.PoolBullet.GetObject();

        if (bulletObj != null)
        {
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();

            if (bulletScript != null)
            {
                // Передаем направление, сам ассет настроек (bulletSettings), машину и менеджер уровней
                bulletScript.Initialize(firePoint, bulletSettings, owner, levelManager);
            }
        }
    }
}