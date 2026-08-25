using System.Collections.Generic;
using UnityEngine;


namespace Mikalai2006.VoxelMap {
    [System.Serializable]
    public class Cell3D // : MonoBehaviour
    {
        public bool collapsed;
        public Tile3D[] tileOptions;
        public Vector3Int position;
        public Vector3 rotation;
        public bool isTop;

        public void CreateCell(bool collapseState, List<Tile3D> tiles, Vector3Int _position, Vector3 _rotation)
        {
            collapsed = collapseState;
            tileOptions = tiles.ToArray();
            position = _position;
            rotation = _rotation;
        }

        public void RecreateCell(Tile3D[] tiles)
        {
            tileOptions = tiles;
        }
    }
}