using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    [Serializable]
    public class GridTileHelper
    {
        private const int MOVE_STRAIGHT_COST = 10;
        private const int MOVE_DIAGONAL_COST = 14;

        [SerializeField] private GridTile<GridTileNode> _gridTile;
        public GridTile<GridTileNode> GridTile
        {
            get { return _gridTile; }
            private set { }
        }

        public GridTileHelper(int rows, int depth, int cols, float cellSize = 1)
        {
            _gridTile = new GridTile<GridTileNode>(rows, depth, cols, cellSize, this, (GridTile<GridTileNode> grid, GridTileHelper gridHelper, int row, int depth, int col) => new GridTileNode(grid, gridHelper, row, depth, col));
        }


        public List<GridTileNode> GetAllGridNodes()
        {
            List<GridTileNode> list = new List<GridTileNode>(_gridTile.GetWidth() * _gridTile.GetHeight());
            if (_gridTile != null)
            {
                for (int depth = 0; depth < _gridTile.GetDepth(); depth++)
                {
                    for (int row = 0; row < _gridTile.GetWidth(); row++)
                    {
                        for (int col = 0; col < _gridTile.GetHeight(); col++)
                        {
                            list.Add(_gridTile.GetGridObject(row, depth, col));
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Get random node.
        /// </summary>
        /// <returns></returns>
        public GridTileNode GetRandomTileNode(List<GridTileNode> listNodes)
        {
            int maxIndex = UnityEngine.Random.Range(0, listNodes.Count);
            int indexRandomNode = UnityEngine.Random.Range(0, maxIndex);
            //Debug.Log($"AllCount {openList.Count}[close={closedList.Count}] | Count {listNodes.Count} rand index {indexRandomNode} ::: {listNodes[indexRandomNode].ToString()}");
            return listNodes[indexRandomNode];
        }

        public GridTileNode GetRandomTileNode(Dictionary<Vector3Int, GridTileNode> listNodes)
        {
            //List<GridTileNode> listNodesx = listNodes.Values.Where(x => x.countRelatedNeighbors > 2).ToList();
            //Debug.Log($"count list oc= {listNodesx.Count}");
            int maxIndex = UnityEngine.Random.Range(0, listNodes.Count);
            int indexRandomNode = UnityEngine.Random.Range(0, maxIndex);
            //Debug.Log($"AllCount {openList.Count}[close={closedList.Count}] | Count {listNodes.Count} rand index {indexRandomNode} ::: {listNodes[indexRandomNode].ToString()}");
            return listNodes.ElementAt(indexRandomNode).Value;
        }

        public List<GridTileNode> FindPath(Vector3Int start, Vector3Int end, bool isDiagonal = false, bool ignoreNotWalkable = false)
        {
            // var flag = LevelManager.Instance.ActivePlayer == null
            //     ? NoskyMask.None
            //     : ((NoskyMask)(1 << LevelManager.Instance.ActivePlayer.DataPlayer.team));
            // var nosky = LevelManager.Instance.Level.nosky;
            // GameManager.Instance.MapManager.ResetTestTileMap();

            GridTileNode startNode = _gridTile.GetGridObjectByVector(start);
            // GridTileNode startNodeTrigger = startNode;
            // GridTileNode endNodeF = _gridTile.GetGridObject(end);
            GridTileNode endNode = _gridTile.GetGridObjectByVector(end);

            BinaryHeap<GridTileNode> openSet = new BinaryHeap<GridTileNode>(_gridTile.GetWidth() * _gridTile.GetHeight());
            HashSet<GridTileNode> closedSet = new HashSet<GridTileNode>();
            openSet.Add(startNode);

            for (int x = 0; x < _gridTile.GetWidth(); x++)
            {
                for (int z = 0; z < _gridTile.GetHeight(); z++)
                {
                    GridTileNode GridTileNode = _gridTile.GetGridObjectByVector(new Vector3Int(x, z));
                    GridTileNode.gCost = int.MaxValue;
                    GridTileNode.CalculateFCost();
                    GridTileNode.cameFromNode = null;
                    //GridTileNode.typeTile = TypeTile.Zero;
                }
            }

            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(startNode, endNode);
            startNode.CalculateFCost();

            var prevNode = startNode;
            while (openSet.Count > 0)
            {
                GridTileNode currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                //GridTileNode currentNode = GetLowestCostNode(openList);
                // GameManager.Instance.MapManager.SetColorForTest(currentNode.position, Color.blue);
                // if (currentNode.ProtectedUnit != null
                //     && currentNode.ProtectedUnit == endNode.ProtectedUnit
                //     && currentNode.OccupiedUnit == null)
                // {
                //     // final protected node
                //     List<GridTileNode> pathList = CalculatePath(currentNode);
                //     return pathList;
                // }


                if (currentNode == endNode)
                {
                    // final
                    List<GridTileNode> pathList = CalculatePath(endNode);
                    return pathList;
                }

                foreach (GridTileNode neighbourNode in GetNeighbourList(currentNode, isDiagonal))
                {
                    if (closedSet.Contains(neighbourNode)) continue;

                    // // Entry point processing.
                    // if (
                    //     neighbourNode == endNode
                    //     && endNode.OccupiedUnit != null
                    //     )
                    // {
                    //     // if (endNode.OccupiedUnit.ScriptableData.TypeEntity != TypeEntity.MapObject)
                    //     // {
                    //     //     continue;
                    //     // }
                    //     var data = endNode.OccupiedUnit.ConfigData as ScriptableEntityMapObject;
                    //     if (data == null) continue;
                    //     if (
                    //         (data.RulesInput.Count > 0
                    //         && !currentNode.StateNode.HasFlag(StateNode.Input)
                    //         && currentNode.InputNode != endNode)
                    //         ||
                    //         (data.RulesInput.Count > 0 && currentNode.StateNode.HasFlag(StateNode.Input) && currentNode.InputNode != endNode)
                    //         )
                    //         // Debug.Log(
                    //         //     $"NO \n" +
                    //         //     $"{neighbourNode.position}[need{end}](input={prevNode.StateNode.HasFlag(StateNode.Input)})\n" +
                    //         //     $"currentNode={currentNode.position}(input={currentNode.StateNode.HasFlag(StateNode.Input)})"
                    //         //     );
                    //         continue;
                    // }

                    // // Exit point processing.
                    // if (currentNode == startNode && currentNode.OccupiedUnit != null)
                    // {
                    //     var data = (ScriptableEntityMapObject)currentNode.OccupiedUnit.ConfigData;
                    //     if (
                    //         (!neighbourNode.StateNode.HasFlag(StateNode.Input)
                    //         && data.RulesInput.Count > 0
                    //         && neighbourNode.InputNode != startNode)
                    //         ||
                    //         (data.RulesInput.Count > 0 && neighbourNode.StateNode.HasFlag(StateNode.Input) && neighbourNode.InputNode != startNode)
                    //    )
                    //     {
                    //         // Debug.Log(
                    //         //     $"NO START \n" +
                    //         //     $"{neighbourNode.position}[need{end}](input={prevNode.StateNode.HasFlag(StateNode.Input)})\n" +
                    //         //     $"currentNode={currentNode.position}(input={currentNode.StateNode.HasFlag(StateNode.Input)})"
                    //         //     );
                    //         continue;
                    //     }
                    // }

                    bool walk = (
                        // // Check open sky.
                        // (
                        //     (nosky.ContainsKey(neighbourNode.position)
                        //     && nosky[neighbourNode.position].HasFlag(flag))
                        //     ||
                        //     nosky.Count == 0
                        // )
                        // // neighbourNode.Empty
                        // // && neighbourNode.Enable
                        // &&
                        neighbourNode.StateNode.HasFlag(StateNode.Empty)
                        // && (
                        //     !neighbourNode.StateNode.HasFlag(StateNode.Protected)
                        //     || (
                        //         neighbourNode.StateNode.HasFlag(StateNode.Protected)
                        //         && neighbourNode.ProtectedUnit == endNode.ProtectedUnit
                        //         )
                        //     )
                        )
                        || (
                        neighbourNode.StateNode.HasFlag(StateNode.Occupied)
                        // && neighbourNode.Enable
                        // && endNode.ProtectedUnit == neighbourNode.ProtectedUnit
                        // && neighbourNode.OccupiedUnit == endNode.OccupiedUnit // TODO
                        )
                        || ignoreNotWalkable;
                    if ((
                        !walk
                        || neighbourNode.StateNode.HasFlag(StateNode.Disable)
                        // ||
                        // (
                        //     neighbourNode.StateNode.HasFlag(StateNode.Guested)
                        //     && neighbourNode != endNode
                        //     // && (
                        //     //     neighbourNode.GuestedUnit != null
                        //     //     && startNode.GuestedUnit != null
                        //     //     && neighbourNode.GuestedUnit.Entity.Player == startNode.GuestedUnit.Entity.Player
                        //     //     )
                        //     )

                        ) && !ignoreNotWalkable)
                    {
                        closedSet.Add(neighbourNode);
                        continue;
                    }

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);

                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNode = currentNode;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                        neighbourNode.CalculateFCost();

                        neighbourNode.levelPath += currentNode.levelPath;

                        if (!openSet.Contains(neighbourNode))
                        {
                            openSet.Add(neighbourNode);
                        }
                    }
                }
                prevNode = currentNode;
            }
            Debug.Log($"Not found path");

            return null; // CalculatePath(endNode);
        }

        // public List<GridTileNode> IsExistExit(
        //     GridTileNode node,
        //     StateNode triggerExit = (StateNode.Teleport | StateNode.Town),
        //     StateNode excludeState = StateNode.Disable
        //     )
        // {
        //     BinaryHeap<GridTileNode> openSet = new BinaryHeap<GridTileNode>(_gridTile.GetWidth() * _gridTile.GetHeight());
        //     HashSet<GridTileNode> closedSet = new HashSet<GridTileNode>();
        //     HashSet<GridTileNode> removedSet = new HashSet<GridTileNode>();
        //     openSet.Add(node);


        //     // List<TypeEntity> exitTriggers = new List<TypeEntity> { TypeEntity.Town, TypeEntity.Hero };
        //     // new List<TypeMapObject> { TypeMapObject.Monolith, TypeMapObject.Town, TypeMapObject.Hero };

        //     while (openSet.Count > 0)
        //     {
        //         GridTileNode currentNode = openSet.RemoveFirst();
        //         closedSet.Add(currentNode);

        //         foreach (GridTileNode neighbourNode in GetNeighbourList(currentNode, true))
        //         {
        //             if ((neighbourNode.StateNode & excludeState) == excludeState)//.HasFlag(StateNode.Disable)
        //             {
        //                 removedSet.Add(neighbourNode);
        //                 continue;
        //             }

        //             if (removedSet.Contains(neighbourNode)) continue;

        //             if (neighbourNode.KeyArea != node.KeyArea)
        //             {
        //                 removedSet.Add(neighbourNode);
        //                 continue;
        //             }

        //             if (neighbourNode.OccupiedUnit != null && neighbourNode != node)
        //             {
        //                 // if (exitTriggers.Contains(neighbourNode.OccupiedUnit.ScriptableData.TypeEntity))
        //                 if (
        //                     // (StateNode.Teleport & neighbourNode.StateNode) == StateNode.Teleport
        //                     // ||
        //                     // (StateNode.Town & neighbourNode.StateNode) == StateNode.Town

        //                     (neighbourNode.StateNode & triggerExit) != 0
        //                     )
        //                 {
        //                     //Debug.Log($"Exit OccupiedUnit::: {neighbourNode.OccupiedUnit.typeUnit}");
        //                     return new List<GridTileNode> { neighbourNode };
        //                 }
        //             }

        //             if (closedSet.Contains(neighbourNode)) continue;

        //             if (!openSet.Contains(neighbourNode))
        //             {
        //                 openSet.Add(neighbourNode);
        //             }
        //             //else
        //             //{
        //             //    openSet.UpdateItem(neighbourNode);
        //             //}


        //         }

        //     }

        //     return closedSet.ToList();
        // }

        // public List<GridTileNode> GetNeighboursAtDistance(GridTileNode startNode, int distance, bool force = true)
        // {
        //     List<GridTileNode> openListNodes = new List<GridTileNode>() { startNode };
        //     List<GridTileNode> closedListNodes = new List<GridTileNode>();
        //     //List<GridTileNode> removedListNodes = new List<GridTileNode>();

        //     while (openListNodes.Count > 0)
        //     {
        //         GridTileNode currentNode = openListNodes[0];

        //         openListNodes.Remove(currentNode);
        //         closedListNodes.Add(currentNode);

        //         foreach (GridTileNode neighbourNode in GetNeighbourList(currentNode, true))
        //         {
        //             //if (neighbourNode.Disable && !force)
        //             //{
        //             //    removedListNodes.Add(neighbourNode);
        //             //    continue;
        //             //}

        //             //if (removedListNodes.Contains(neighbourNode)) continue;
        //             if (closedListNodes.Contains(neighbourNode)) continue;

        //             //if (neighbourNode.keyArea != node.keyArea )
        //             //{
        //             //    removedListNodes.Add(neighbourNode);
        //             //    continue;
        //             //}

        //             //if (neighbourNode.OccupiedUnit != null && neighbourNode != node)
        //             //{
        //             //    if (exitTriggers.Contains(neighbourNode.OccupiedUnit.typeUnit))
        //             //    {
        //             //        //Debug.Log($"Exit OccupiedUnit::: {neighbourNode.OccupiedUnit.typeUnit}");
        //             //        return new List<GridTileNode> { neighbourNode };
        //             //    }
        //             //}


        //             if (
        //                 !openListNodes.Contains(neighbourNode)
        //                 && GetDistanceBetweeenPoints(startNode.position, neighbourNode.position) <= distance
        //                 )
        //             {
        //                 openListNodes.Add(neighbourNode);
        //             }
        //         }

        //     }

        //     return closedListNodes;
        // }

        public List<GridTileNode> GetNeighbourList(GridTileNode currentNode, bool isDiagonal = false)
        {
            List<GridTileNode> neighbourList = new List<GridTileNode>();

            if (currentNode.X - 1 >= 0)
            {
                //left
                neighbourList.Add(GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z));
                if (isDiagonal)
                {
                    // left down
                    if (currentNode.Z - 1 >= 0) neighbourList.Add(GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z - 1));
                    // left up
                    if (currentNode.Z + 1 < _gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z + 1));
                }
            }

            if (currentNode.X + 1 < _gridTile.GetWidth())
            {
                // right
                neighbourList.Add(GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z));
                if (isDiagonal)
                {
                    // right down
                    if (currentNode.Z - 1 >= 0) neighbourList.Add(GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z - 1));
                    //right up
                    if (currentNode.Z + 1 < _gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z + 1));
                }
            }

            // down
            if (currentNode.Z - 1 >= 0) neighbourList.Add(GetNode(currentNode.X, currentNode.Y, currentNode.Z - 1));
            // up
            if (currentNode.Z + 1 < _gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.X, currentNode.Y, currentNode.Z + 1));

            return neighbourList;
        }

        public List<GridTileNode> GetNeighbourListWithTiled(GridTileNode currentNode, bool isDiagonal)
        {
            List<GridTileNode> neighbourList = new List<GridTileNode>();

            if (currentNode.X - 1 >= 0)
            {
                //left
                GridTileNode left = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z);
                if (left.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(left);
                if (isDiagonal)
                {
                    // left down
                    // if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
                    var leftDown = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z - 1);
                    if (leftDown != default && leftDown.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(leftDown);
                    // left up
                    // if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
                    var leftUp = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z + 1);
                    if (leftUp != default && leftUp.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(leftUp);
                }
            }

            if (currentNode.X + 1 < _gridTile.GetWidth())
            {
                // right
                GridTileNode right = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z);
                if (right.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(right);
                if (isDiagonal)
                {
                    //// right down
                    //GridTileNode rightDown = GetNode(currentNode.x + 1, currentNode.y);
                    //if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                    var rightDown = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z - 1);
                    if (rightDown != default && rightDown.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(rightDown);
                    ////right up
                    //if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
                    var rightUp = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z + 1);
                    if (rightUp != default && rightUp.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(rightUp);
                }
            }

            // down
            if (currentNode.Z - 1 >= 0)
            {
                GridTileNode down = GetNode(currentNode.X, currentNode.Y, currentNode.Z - 1);
                if (down.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(down);
            }
            // up
            if (currentNode.Z + 1 < _gridTile.GetHeight())
            {

                GridTileNode up = GetNode(currentNode.X, currentNode.Y, currentNode.Z + 1);
                if (up.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop)) neighbourList.Add(up);
            }

            // // top.
            // if (currentNode.Y + 1 >= 0)
            // {
            //     GridTileNode top = GetNode(currentNode.X, currentNode.Y + 1, currentNode.Z);
            //     if (top.HasAnyState(StateNode.Tiled | StateNode.TiledInner)) neighbourList.Add(top);
            // }

            // // bottom.
            // if (currentNode.Y - 1 >= 0)
            // {
            //     GridTileNode bottom = GetNode(currentNode.X, currentNode.Y - 1, currentNode.Z);
            //     if (bottom.HasAnyState(StateNode.Tiled | StateNode.TiledInner)) neighbourList.Add(bottom);
            // }

            return neighbourList;
        }

        public GridTileNode GetNeighbourTiledTop(GridTileNode currentNode)
        {
            if (currentNode.Y + 1 >= 0)
            {
                GridTileNode top = GetNode(currentNode.X, currentNode.Y + 1, currentNode.Z);
                if (top.HasAnyState(StateNode.Tiled | StateNode.TiledInner | StateNode.TiledTop | StateNode.TiledInnerTop))
                {
                    return top;
                }
                ;
            }

            return default;
        }

        public List<GridTileNode> GetNeighbourListWithTypeGroundOrNone(GridTileNode currentNode)
        {
            List<GridTileNode> neighbourList = new List<GridTileNode>();

            if (currentNode.X - 1 >= 0)
            {
                //left
                GridTileNode left = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z);
                if (currentNode.TypeGround == left.TypeGround || left.TypeGround == TypeGround.None) neighbourList.Add(left);
                //// left down
                //if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
                //// left up
                //if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
            }

            if (currentNode.X + 1 < _gridTile.GetWidth())
            {
                // right
                GridTileNode right = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z);
                if (currentNode.TypeGround == right.TypeGround || right.TypeGround == TypeGround.None) neighbourList.Add(right);
                //// right down
                //GridTileNode rightDown = GetNode(currentNode.x + 1, currentNode.y);
                //if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                ////right up
                //if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
            }

            // down
            if (currentNode.Y - 1 >= 0)
            {
                GridTileNode down = GetNode(currentNode.X, currentNode.Y - 1, currentNode.Z);
                if (currentNode.TypeGround == down.TypeGround || down.TypeGround == TypeGround.None) neighbourList.Add(down);
            }
            // up
            if (currentNode.Y + 1 < _gridTile.GetHeight())
            {

                GridTileNode up = GetNode(currentNode.X, currentNode.Y + 1, currentNode.Z);
                if (currentNode.TypeGround == up.TypeGround || up.TypeGround == TypeGround.None) neighbourList.Add(up);
            }

            return neighbourList;
        }

        public List<GridTileNode> GetEmptyNodes()
        {
            List<GridTileNode> result = GetAllGridNodes()
                .Where(n =>
                    !n.StateNode.HasFlag(StateNode.Disable)
                    && !n.StateNode.HasFlag(StateNode.Occupied)
                )
                .ToList();

            return result;
        }

        public List<GridTileNode> GetNeighbourListWithTypeGround(GridTileNode currentNode)
        {
            List<GridTileNode> neighbourList = new List<GridTileNode>();

            if (currentNode.X - 1 >= 0)
            {
                //left
                GridTileNode left = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z);
                if (currentNode.TypeGround == left.TypeGround) neighbourList.Add(left);
                //// left down
                //if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
                //// left up
                //if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
            }

            if (currentNode.X + 1 < _gridTile.GetWidth())
            {
                // right
                GridTileNode right = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z);
                if (currentNode.TypeGround == right.TypeGround) neighbourList.Add(right);
                //// right down
                //GridTileNode rightDown = GetNode(currentNode.x + 1, currentNode.y);
                //if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                ////right up
                //if (currentNode.y + 1 < gridTile.GetHeight()) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
            }

            // down
            if (currentNode.Y - 1 >= 0)
            {
                GridTileNode down = GetNode(currentNode.X, currentNode.Y - 1, currentNode.Z);
                if (currentNode.TypeGround == down.TypeGround) neighbourList.Add(down);
            }
            // up
            if (currentNode.Y + 1 < _gridTile.GetHeight())
            {

                GridTileNode up = GetNode(currentNode.X, currentNode.Y + 1, currentNode.Z);
                if (currentNode.TypeGround == up.TypeGround) neighbourList.Add(up);
            }
            return neighbourList;
        }

        public NeighboursNature GetDisableNeighbours(GridTileNode currentNode)
        {
            NeighboursNature neighbours = new NeighboursNature();
            neighbours.left = new List<GridTileNode>();
            neighbours.right = new List<GridTileNode>();
            neighbours.top = new List<GridTileNode>();
            neighbours.bottom = new List<GridTileNode>();

            if (currentNode.X - 1 >= 0)
            {
                //left
                GridTileNode left = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z);
                if (left.StateNode.HasFlag(StateNode.Disable)) neighbours.left.Add(left);
                // left down
                if (currentNode.Y - 1 >= 0)
                {
                    GridTileNode leftdown = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z - 1);
                    if (leftdown.StateNode.HasFlag(StateNode.Disable))
                    {
                        neighbours.left.Add(leftdown);
                        neighbours.bottom.Add(leftdown);
                    }
                }
                // left up
                if (currentNode.Y + 1 < _gridTile.GetHeight())
                {
                    GridTileNode leftup = GetNode(currentNode.X - 1, currentNode.Y, currentNode.Z + 1);
                    if (leftup.StateNode.HasFlag(StateNode.Disable))
                    {
                        neighbours.top.Add(leftup);
                        neighbours.left.Add(leftup);
                    }
                }
            }

            if (currentNode.X + 1 < _gridTile.GetWidth())
            {
                // right
                GridTileNode right = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z);
                if (right.StateNode.HasFlag(StateNode.Disable)) neighbours.right.Add(right);
                // right down
                if (currentNode.Y - 1 >= 0)
                {
                    GridTileNode rightDown = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z - 1);
                    if (rightDown.StateNode.HasFlag(StateNode.Disable))
                    {
                        neighbours.right.Add(rightDown);
                        neighbours.bottom.Add(rightDown);
                    }
                }
                //right up
                if (currentNode.Y + 1 < _gridTile.GetHeight())
                {
                    GridTileNode rightUp = GetNode(currentNode.X + 1, currentNode.Y, currentNode.Z + 1);
                    if (rightUp.StateNode.HasFlag(StateNode.Disable))
                    {
                        neighbours.right.Add(rightUp);
                        neighbours.top.Add(rightUp);
                    }
                }
            }

            // down
            if (currentNode.Y - 1 >= 0)
            {
                GridTileNode down = GetNode(currentNode.X, currentNode.Y, currentNode.Z - 1);
                if (down.StateNode.HasFlag(StateNode.Disable)) neighbours.bottom.Add(down);
            }
            // up
            if (currentNode.Y + 1 < _gridTile.GetHeight())
            {
                GridTileNode up = GetNode(currentNode.X, currentNode.Y, currentNode.Z + 1);
                if (up.StateNode.HasFlag(StateNode.Disable)) neighbours.top.Add(up);
            }
            neighbours.count = neighbours.bottom.Count + neighbours.top.Count
                + neighbours.left.Count + neighbours.right.Count;
            return neighbours;
        }

        public GridTileNode GetNode(int __row, int __depth, int __col)
        {
            // Debug.Log($"GetNode {__row}, {__depth}, {__col}");
            return _gridTile.GetGridObject(__row, __depth, __col);
        }
        
        // public GridTileNode GetNode(Vector3Int pos)
        // {
        //     return _gridTile.GetGridObject(pos);
        // }

        private List<GridTileNode> CalculatePath(GridTileNode endNode)
        {
            List<GridTileNode> path = new List<GridTileNode>();
            path.Add(endNode);

            GridTileNode currentNode = endNode;
            while (currentNode.cameFromNode != null)
            {
                path.Add(currentNode.cameFromNode);
                currentNode = currentNode.cameFromNode;
            }
            path.Reverse();
            return path;
        }

        private int CalculateDistanceCost(GridTileNode a, GridTileNode b)
        {
            int xDistance = Mathf.Abs(a.X - b.X);
            int yDistance = Mathf.Abs(a.Y - b.Y);
            int remaining = Mathf.Abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * a.koofPath * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * b.koofPath * remaining;
        }

        private GridTileNode GetLowestCostNode(List<GridTileNode> GridTileNodeList)
        {
            GridTileNode lowestFCostNode = GridTileNodeList[0];
            for (int i = 1; i < GridTileNodeList.Count; i++)
            {
                if (GridTileNodeList[i].fCost < lowestFCostNode.fCost)
                {
                    lowestFCostNode = GridTileNodeList[i];
                }
            }
            return lowestFCostNode;
        }

        public float GetDistanceBetweeenPoints(Vector3Int a, Vector3Int b)
        {
            float xCoord = b.x - a.x;
            float yCoord = b.y - a.y;
            float remaining = Mathf.Pow(xCoord, 2) + Mathf.Pow(yCoord, 2);
            return Mathf.Sqrt(remaining);
        }

        // public int CalculateNeighbours(GridTileNode node)
        // {
        //     List<GridTileNode> neighbours = GetNeighbourList(node, true);
        //     int countWalkableNeighbours = 0;
        //     for (int i = 0; i < neighbours.Count; i++)
        //     {
        //         // if (neighbours[i].Empty && neighbours[i].Enable && !neighbours[i].Protected)
        //         if (
        //             neighbours[i].StateNode.HasFlag(StateNode.Empty)
        //             // && !neighbours[i].StateNode.HasFlag(StateNode.Protected)
        //             )
        //         {
        //             countWalkableNeighbours++;
        //         }
        //     }

        //     return countWalkableNeighbours;
        // }

        /// <summary>
        /// Mark node as disable
        /// </summary>
        /// <param name="node"></param>
        /// <param name="listNoPath"></param>
        /// <param name="color"></param>
        public void SetDisableNode(GridTileNode node, Color color)
        {
            color = color == null ? Color.black : color;

            //node.SetState(TypeStateNode.Disabled);
            node.SetDisableNode();
            // if (LevelManager.Instance.ConfigGameSettings.showBgColorMapObject)
            // {
            //     GameManager.Instance.MapManager.SetColorForTile(node.position, color);
            // }

            // if (listNoPath == null) return;

            // if (listNoPath.Count > 0)
            // {
            //     List<GridTileNode> list = GetNodeListAsNoPath(node, listNoPath);
            //     if (list.Count > 0)
            //     {
            //         foreach (GridTileNode nodePath in list)
            //         {
            //             SetDisableNode(nodePath, null, color);
            //         }
            //     }
            // }
        }
    }

    [System.Flags]
    public enum NoskyMask
    {
        None = 1 << 0,
        One = 1 << 1,
        Two = 1 << 2,
        Three = 1 << 3,
        Four = 1 << 4,
        Five = 1 << 5,
        Six = 1 << 6,
        Seven = 1 << 7,
        Eight = 1 << 8,
        Nine = 1 << 9,
        Ten = 1 << 10
    }

    public struct NeighboursNature
    {
        public List<GridTileNode> left;
        public List<GridTileNode> right;

        public List<GridTileNode> top;
        public List<GridTileNode> bottom;
        public int count;
    }
}