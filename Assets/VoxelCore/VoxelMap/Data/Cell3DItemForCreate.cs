using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    [System.Serializable]
    public struct Cell3DItemForCreate
    {
        public GameObject wrapper;
        public Cell3D cell3D;
        public Tile3DGroup tile3DGroup;
    }
}