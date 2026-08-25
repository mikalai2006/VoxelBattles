using Mikalai2006.VoxelBase;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    
    [System.Serializable]
    public struct Cell3DData
    {
        public string uid;
        public Vector3Int position;
        public float RotationY;
        public TypeEntity typeCell;
        // public int stateNode;
        // public int top; // 0 - false, 1 - true
    }
}