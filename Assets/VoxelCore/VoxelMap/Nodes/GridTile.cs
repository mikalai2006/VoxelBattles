using System;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    public class GridTile<TGridObject>
    {

        private readonly int _rows;
        private readonly int _cols;
        private readonly int _depth;
        private readonly float _cellSize;
        private readonly TGridObject[,,] _gridArray;

        public int SizeGrid { get { return _rows * _cols; } private set { } }

        public GridTile(int rows, int depth, int cols, float cellSize, GridTileHelper gridTileHelper, Func<GridTile<TGridObject>, GridTileHelper, int, int, int, TGridObject> createValue)
        {
            _rows = rows;
            _cols = cols;
            _depth = depth;
            _cellSize = cellSize;

            _gridArray = new TGridObject[rows, depth, cols];

            for (int __depth = 0; __depth < _gridArray.GetLength(1); __depth++)
            {
                for (int __row = 0; __row < _gridArray.GetLength(0); __row++)
                {
                    for (int __col = 0; __col < _gridArray.GetLength(2); __col++)
                    {
                        _gridArray[__row, __depth, __col] = createValue(this, gridTileHelper, __row, __depth, __col);
                    }
                }
            }

            Debug.Log($"Создано дерево сетки карты! Количество ячеек - {_gridArray.Length}\r\nКол-во строк={_gridArray.GetLength(0)}\r\nГлубина={_gridArray.GetLength(1)}\r\nКол-во столбцов={_gridArray.GetLength(2)}");

            // return gridArray;
        }

        // private Vector3 GetWorldPosition(int x, int y)
        // {
        //     return new Vector3(x, 0, y) * _cellSize;
        // }

        public void SetValue(int row, int col, int depth, TGridObject value)
        {
            _gridArray[row, depth, col] = value;
        }

        public TGridObject[,,] GetGrid()
        {
            return _gridArray;
        }

        public TGridObject GetGridObject(int row, int depth, int col)
        {
            // Debug.Log($"GetGridObject {row},{depth},{col}");
            return row >= 0 &&
                row < _rows &&
                depth >= 0 &&
                depth < _depth &&
                col >= 0 &&
                col < _cols ?
                    _gridArray[row, depth, col] : default;
        }

        public TGridObject GetGridObjectByVector(Vector3Int pos)
        {
            return GetGridObject(pos.x, pos.y, pos.z);
        }
        // public TGridObject GetGridObject(Vector3Int pos)
        // {
        //     Debug.Log($"GetGrid {x},{z}: {GetWorldPosition(x, z)}");
        //     return pos.x >= 0 && pos.y >= 0 && pos.x < _rows && pos.y < _cols && pos.z >= 0 && pos.z < _depth ? _gridArray[pos.x, pos.z, pos.y] : default;
        // }

        public int GetHeight()
        {
            return _cols;
        }

        public int GetWidth()
        {
            return _rows;
        }

        public int GetDepth()
        {
            return _depth;
        }
    }
}