using Mikalai2006.VoxelBase;
using UnityEngine;

public class VoxelPrefab : PoolEntity
{
    GameManager gameManager => GameManager.Instance;
    public bool IsActive { get; private set; }

    public GPUInstanceEnabler gPUInstanceEnabler;
    void Awake()
    {
        gPUInstanceEnabler = GetComponent<GPUInstanceEnabler>();

        // Init();
    }

    public void Init() // SOVoxelData sOVoxelData
    {
        
        // var maxBoundsSize = Mathf.Max(sOVoxelData.Bounds.x, sOVoxelData.Bounds.y, sOVoxelData.Bounds.z);
        // transform.localScale = new Vector3(1f/maxBoundsSize, 1f/maxBoundsSize, 1f/maxBoundsSize);

        // transform.localScale = new Vector3(gameManager.Settings.scaleObjects, gameManager.Settings.scaleObjects, gameManager.Settings.scaleObjects);

    }

    public void SetColor(Color color)
    {
        if (gPUInstanceEnabler)
        {
            gPUInstanceEnabler.SetColor(color);
        }
    }

    protected override void OnSpawn()
    {
        IsActive = true;
    }

    protected override void OnDespawn()
    {
        IsActive = false;
    }
}
