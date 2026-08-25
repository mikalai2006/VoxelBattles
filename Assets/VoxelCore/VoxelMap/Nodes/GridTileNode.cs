
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    [Serializable]
    public class GridTileNode : IHeapItem<GridTileNode>
    {
        [NonSerialized] private readonly GridTile<GridTileNode> _grid;
        [NonSerialized] private readonly GridTileHelper _gridHelper;
        [SerializeField] public StateNode StateNode = StateNode.Empty;
        public int X;
        public bool isTop;
        public int Y;
        public int Z;
        public TypeGround TypeGround = TypeGround.None;

        [NonSerialized] public int level;
        [NonSerialized] public Vector3Int position;
        // [NonSerialized] private BaseMachine _ocuppiedUnit = null;
        // public BaseMachine OccupiedUnit => _ocuppiedUnit;
        // public GridTileNode leftDown => _grid.GetGridObject(new Vector3Int(position.x - 1, position.y - 1, 0));
        // public GridTileNode leftUp => _grid.GetGridObject(new Vector3Int(position.x - 1, position.y + 1, 0));
        // public GridTileNode rightDown => _grid.GetGridObject(new Vector3Int(position.x + 1, position.y - 1, 0));
        // public GridTileNode rightUp => _grid.GetGridObject(new Vector3Int(position.x + 1, position.y + 1, 0));
        public bool IsAllowSpawn =>
            StateNode.HasFlag(StateNode.Empty)
            && !StateNode.HasFlag(StateNode.Occupied);
        // (StateNode.Empty | ~StateNode.Protected | ~StateNode.Occupied) == (StateNode.Empty | ~StateNode.Protected | ~StateNode.Occupied);

        private int heapIndex;
        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        [NonSerialized] public int countRelatedNeighbors = 0;
        [NonSerialized] public GridTileNode cameFromNode;
        [NonSerialized] public int gCost;
        [NonSerialized] public int hCost;
        [NonSerialized] public float fCost;
        [NonSerialized] public int koofPath = 10;
        [NonSerialized] public int levelPath = 0;
        [NonSerialized] public bool isCreated = false;
        [NonSerialized] public bool isEdge = false;

        public Tile3D[] tileOptions;
        public bool isCollapsed;
        public void CreateTiles(bool collapseState, List<Tile3D> tiles)
        {
            isCollapsed = collapseState;
            tileOptions = tiles.ToArray();
        }

        public void RecreateTiles(Tile3D[] tiles)
        {
            tileOptions = tiles;
        }

        public GridTileNode(GridTile<GridTileNode> grid, GridTileHelper gridHelper, int rowNumber, int depthNumber, int colNumber)
        {
            position = new Vector3Int(rowNumber, depthNumber, colNumber);
            _gridHelper = gridHelper;
            _grid = grid;
            this.X = rowNumber;
            this.Y = depthNumber;
            this.Z = colNumber;
        }

        public Vector3Int positionXZ()
        {
            return new Vector3Int(X, Y, Z);
        }

        public bool HasAnyState(StateNode any)
        {
            return (StateNode & any) != 0;
        }

        public bool HasAllState(StateNode all)
        {
            return (StateNode & all) == all;
        }

        public int CompareTo(GridTileNode nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
                compare = hCost.CompareTo(nodeToCompare.hCost);
            return -compare;
        }

        public void SetDisableNode()
        {
            StateNode = StateNode.Disable;
        }

        public void AddStateNode(StateNode state)
        {
            StateNode |= state;
        }

        public void RemoveStateNode(StateNode state)
        {
            StateNode ^= state;
        }

        /// <summary>
        /// Set occupied entity for node.
        /// </summary>
        /// <param name="entity">Entity or null</param>
        public void SetOcuppiedUnit(bool status)
        {
            // _ocuppiedUnit = entity;
            if (status == true)
            {
                StateNode &= ~StateNode.Occupied;
                StateNode |= StateNode.Empty;
            }
            else
            {
                StateNode &= ~StateNode.Empty;
                StateNode |= StateNode.Occupied;
            }
        }
        
        public void CalculateFCost()
        {
            fCost = gCost + hCost;

        }


    #if UNITY_EDITOR
        public override string ToString()
        {
            return "GridTileNode:::" +
                // "keyArea=" + KeyArea + ",\n" +
                "[x" + position.x + ",y" + position.y + ",z" + position.z + "] \n" +
                "typeGround=" + TypeGround + ",\n" +
                // "OccupiedUnit=" + OccupiedUnit?.ToString() + ",\n" +
                // "GuestedUnit=" + _guestedUnit?.ToString() + ",\n" +
                "StateNode=" + Convert.ToString((int)StateNode, 2) + ",\n" +
                // "ProtectedUnit=" + ProtectedUnit?.ToString() + ",\n" +
                "countNeighbours=" + countRelatedNeighbors + ",\n" +
                "(gCost=" + gCost + ") (hCost=" + hCost + ") (fCost=" + fCost + ")";
        }

    #endif

    }
}