using Cysharp.Threading.Tasks;
using Mikalai2006.VoxelBase;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private BulletSettingsAsset settings;
    private Vector3 direction;
    private GameObject owner;
    private LevelManager levelManagerRef;
    private bool isInitialized = false;
    private float currentLifetime = 0f;

    [Header("Визуальные компоненты префаба")]
    [Tooltip("Компонент рендера вокселей пули")]
    [SerializeField] private VoxelMeshRender voxelMeshRender;

    private static readonly RaycastHit[] hitsCache = new RaycastHit[2];
    private static readonly Collider[] overlapCache = new Collider[4];

    [SerializeField] private LayerMask excludeMask;
    [SerializeField] private LayerMask includeMask;
    private LayerMask layerMaskForSphereCast => includeMask & ~excludeMask;

    private void Awake()
    {
        // ИСПРАВЛЕНИЕ: Ищем компонент в дочерних объектах, если поле в инспекторе осталось пустым
        if (voxelMeshRender == null)
        {
            voxelMeshRender = GetComponentInChildren<VoxelMeshRender>(true);
        }

        //excludelayerMask = ~(1 << LayerMask.NameToLayer("Bullet") | LayerMask.NameToLayer("Platform"));
    }

    /// <summary>
    /// Инициализация пули из пула. Динамически подгружает воксельные данные из Scriptable Object.
    /// </summary>
    public void Initialize(Transform shooterTransform, BulletSettingsAsset bulletSettings, GameObject shooter, LevelManager levelManager)
    {

        settings = bulletSettings;
        direction = shooterTransform.forward.normalized;
        Vector3 pos = shooterTransform.position + direction * 0.5f; ;
        pos.y = 0.3f;
        transform.position = pos;
        owner = shooter;
        levelManagerRef = levelManager;

        isInitialized = true;
        currentLifetime = 0f;

        // Настройка воксельного рендера данными из переданного ассета настроек пули
        if (voxelMeshRender != null)
        {
            //voxelMeshRender.SetActive(true);
            voxelMeshRender.SetSOVoxelData(bulletSettings.SOVoxelData);
            voxelMeshRender.SetColorsModify(bulletSettings.colorsModifies);
            voxelMeshRender.Init();
        }
    }
    private void Update()
    {
        if (!isInitialized || settings == null) return;

        currentLifetime += Time.deltaTime;
        if (currentLifetime >= settings.maxLifetime)
        {
            DestroyBullet();
            return;
        }

        // --- ЭТАП 1: ПРОВЕРКА НА НИЗКИХ СКОРОСТЯХ (Overlap) ---
        int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, settings.radius, overlapCache, layerMaskForSphereCast, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = overlapCache[i];

            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Пропускаем свой собственный коллайдер
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            // Защита от самострела (пропускаем машину)
            if (col.gameObject == owner || col.transform.IsChildOf(owner.transform)) continue;

            OnStaticCollision(col);
            return;
        }

        // --- ЭТАП 2: ПРОСЧЁТ ДВИЖЕНИЯ ВПЕРЁД (SphereCast) ---
        float frameDistance = settings.speed * Time.deltaTime;
        Vector3 origin = transform.position;

        int hitCount = Physics.SphereCastNonAlloc(origin, settings.radius, direction, hitsCache, frameDistance, layerMaskForSphereCast, QueryTriggerInteraction.Ignore);

        if (hitCount > 0)
        {
            SortHitsByDistance(hitsCache, hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitsCache[i];

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Пропускаем свой собственный коллайдер
                if (hit.collider == null || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;

                // Защита от самострела (пропускаем машину)
                if (hit.collider.gameObject == owner || hit.collider.transform.IsChildOf(owner.transform)) continue;

                transform.position = hit.point;
                OnBulletCollision(hit);
                return;
            }
        }

        transform.position += direction * frameDistance;
    }

    private void SortHitsByDistance(RaycastHit[] hits, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (hits[j].distance < hits[i].distance)
                {
                    RaycastHit temp = hits[i];
                    hits[i] = hits[j];
                    hits[j] = temp;
                }
            }
        }
    }

    private void OnBulletCollision(RaycastHit hit)
    {
        isInitialized = false;

        // ТУТ ВАША ПРЕДСТОЯЩАЯ ЛОГИКА УРОНА
        //Debug.LogWarning($"OnBulletCollision: {name}");

        // Проверяем, есть ли на объекте, в который мы врезались, компонент Container
        HelperVoxel.ProcessVoxelHit(hit.collider, hit.point, hit.normal, gameObject, settings.radiusExplode);

        DestroyBullet();
    }

    private void OnStaticCollision(Collider otherCollider)
    {
        //Debug.LogWarning($"OnStaticCollision: {name}");
        //isInitialized = false;
        //DestroyBullet();
    }

    private void DestroyBullet()
    {
        isInitialized = false;
        currentLifetime = 0f;
        settings = null;

        //if (voxelMeshRender != null)
        //{
        //    voxelMeshRender.SetActive(false);
        //}

        System.Array.Clear(hitsCache, 0, hitsCache.Length);
        System.Array.Clear(overlapCache, 0, overlapCache.Length);

        if (levelManagerRef != null && levelManagerRef.PoolBullet != null)
        {
            levelManagerRef.PoolBullet.ReturnObject(gameObject).Forget();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
