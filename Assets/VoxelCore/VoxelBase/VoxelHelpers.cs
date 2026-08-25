using System.Collections.Generic;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    public static class  VoxelHelpers
    {
        /// <summary>
        /// Takes 3D indexes and returns a 1D index based on them
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="xMax"></param>
        /// <param name="yMax"></param>
        /// <returns>1D index calulation</returns>
        public static int To1D(int x, int y, int z, int xMax, int yMax)
        {
            //return (z * xMax * yMax) + (y * xMax) + x;
            return x + xMax * (y + yMax * z);
        }

        /// <summary>
        /// округление координат вектора
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3Int RoundVector3(Vector3 vector)
        {
            return new Vector3Int(
                Mathf.RoundToInt(vector.x),
                Mathf.RoundToInt(vector.y),
                Mathf.RoundToInt(vector.z)
            );
        }
        
        /// <summary>
        /// Возвращает кол-во вокселей из списка воксельных конфигов.
        /// </summary>
        /// <param name="gameMachine"></param>
        /// <returns></returns>
        public static int GetCountVoxels(List<MeshConfig> configs)
        {
            int countVoxels = 0;

            foreach (var config in configs)
            {
                countVoxels += config.sOVoxelData.countVoxels;
            }

            return countVoxels;
        }
    }

}
