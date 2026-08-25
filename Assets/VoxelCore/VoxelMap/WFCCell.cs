#if UNITY_EDITOR
using Mikalai2006.VoxelBase;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    [System.Serializable]
    public class WFCCell
    {
        public bool collapsed;
        public bool disabled;
        public int height;
        public int groupNumber;
        public TypeEntity typeCell;
        public Tile3D[] tileOptions;
        public Vector3Int position;
        public Tile3D MBObject;
        public Tile3DDebugCell MBDebug;

        public WFCCell(bool collapseState, Tile3D[] tiles, Vector3Int pos, int _height)
        {
            collapsed = collapseState;
            tileOptions = tiles;
            position = pos;
            height = _height;
        }

        public void SetGroup(int _groupNumber)
        {
            groupNumber = _groupNumber;
            
            RefreshDebugText();
        }

        public void RecreateCell(Tile3D[] tiles)
        {
            tileOptions = tiles;

            if (MBDebug)
            {
                RefreshDebugText();
            }
        }

        public void SetMBObject(Tile3D tile3D)
        {
            MBObject = tile3D;
            
            RefreshDebugText();
        }

        public void SetTypeCell(TypeEntity _typeCell)
        {
            typeCell = _typeCell;

            RefreshDebugText();
        }

        private void RefreshDebugText()
        {
            if (MBDebug)
            {
                MBDebug.text.text = $"{collapsed}/{disabled}/{groupNumber}\r\ntil:{tileOptions.Length}\r\nh:{height},ty:{typeCell}";
            }
        }
    }
}
#endif