using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    public static class WFCHelpers
    {
        /// <summary>
        /// Преобразование 3D-координат (x, y, z) в 1D индекс
        /// </summary>
        /// <param name="row">Значение строки в 3-х-мерном массиве</param>
        /// <param name="depth">Значение глубины в 3-х-мерном массива</param>
        /// <param name="col">Значение столбца в 3-х-мерном массиве</param>
        /// <param name="rowsCount">Количество строк в 3-х-мерном массиве</param>
        /// <param name="depthMax">Глубина 3-х-мерного массива</param>
        /// <param name="colsCount">Количество столбцов в 3-х-мерном массиве</param>
        /// <returns>1D index calulation</returns>
        public static int From3DTo1D(int row, int depth, int col, Vector3Int size)
        {
            //return (z * xMax * yMax) + (y * xMax) + x;

            // Пример преобразования 3D-координат (d, r, c) в 1D индекс
            // int d = 0; // Индекс глубины
            // int r = 1; // Индекс строки
            // int c = 2; // Индекс столбца

            return (depth * size.x * size.z) + (row * size.z) + col;
        }

        
        /// <summary>
        /// Переводит индекс элемента массива из одномерного в двумерный
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>1D index calulation</returns>
        public static Vector2Int From1DTo2D(int index, int width)
        {
            var x = (int) System.Math.Floor((decimal)index / width);
            var y = index % width;
            return new Vector2Int(x, y);
        }
        
        public static Tile3D GetRandomTile(Tile3D[] availableTiles)
        {
            List<float> chances = new List<float>();
            for (int i = 0; i < availableTiles.Length; i++)
            {
                chances.Add(availableTiles[i].Weight);
            }

            float value = UnityEngine.Random.Range(0, chances.Sum());
            float sum = 0;

            for (int i = 0; i < chances.Count; i++)
            {
                sum += chances[i];
                if (value < sum)
                {
                    return availableTiles[i];
                }
            }

            return availableTiles[availableTiles.Length - 1];
        }
    }
    
}
