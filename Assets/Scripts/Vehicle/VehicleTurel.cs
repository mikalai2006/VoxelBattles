using UnityEngine;

public class VehicleTurel : MonoBehaviour, IShootable
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;

    private float nextFireTime;
    public bool CanShoot => Time.time >= nextFireTime;

    public void Shoot()
    {
        if (!CanShoot) return;

        nextFireTime = Time.time + fireRate;
        //Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Debug.Log($"{gameObject.name} произвел выстрел!");
    }
}
