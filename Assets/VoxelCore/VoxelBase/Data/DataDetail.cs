using UnityEngine;

namespace Mikalai2006.VoxelBase
{
  [System.Serializable]
  public class DataDetail
  {
    public string ido;
    public string parentId;
    public int number;
    public string nameConfig;
        public VehicleDetailType type;
        // [NonSerialized] public Vector3 Bounds;
        public Vector3 offset;
    public SerializeVector3 destroyVoxels;
    public DataDetail()
    {
      destroyVoxels = new();
    }
  }
}