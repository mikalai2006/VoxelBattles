using System.Collections.Generic;

namespace Mikalai2006.VoxelMap
{
    [System.Serializable]
    public struct DataGroupTiles
    {
        public int group;
        public int team;
        public List<Cell3DData> tiles;
    }
}