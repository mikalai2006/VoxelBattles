#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mikalai2006.VoxelBase;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public abstract class WFCCreator : MonoBehaviour
    {
        public WFCBuilder wFCBuilder;
        private Tile3D[] tileObjects;
        [SerializeField] protected List<WFCCell> gridComponents;
        public GameObject WrapperTiles;
        protected ParserHeight parserHeight;
            
        private bool auto;

        int iterations = 0;
        // public int maxCountRemoveEmptyCells;
        // int countRemoveEmptyCells = 0;
        int maxCountIterations;

        public virtual void Awake()
        {
            gridComponents = new List<WFCCell>();
            auto = false;
        }

        public virtual void SetConfig(ref ParserHeight parserHeight)
        {
            
        }

        public virtual void InitializeGrid(Tile3D[] prefabTiles, ParserHeight _parserHeight, TypeEntity typeCell)
        {
            parserHeight = _parserHeight;
            
            maxCountIterations =  wFCBuilder.size.x * wFCBuilder.size.y * wFCBuilder.size.z;

            tileObjects = prefabTiles;

            for (int y = 0; y < wFCBuilder.size.y; y++)
            {
                for (int x = 0; x < wFCBuilder.size.x; x++)
                {
                    for (int z = 0; z < wFCBuilder.size.z; z++)
                    {
                        int _height = parserHeight.heightMap[new Vector2Int(x, z)];

                        // Debug.Log($"{new Vector3Int(x, y, z)}: height={_height}");
                        var position = new Vector3Int(x, y, z);

                        if (_height > 0 && y < _height)
                        {
                            WFCCell newCell =  new WFCCell(false, tileObjects.ToArray(), position, _height);

                            newCell.SetGroup(0);

                            newCell.SetTypeCell(typeCell);

                            gridComponents.Add(newCell);

                            CreateDebugCell(newCell);
                        } else
                        {
                            WFCCell newCell =  new WFCCell(true, new Tile3D[] { wFCBuilder.wFCManager.emptyTilePrefab }, new Vector3Int(x, y, z), _height);
                            
                            newCell.disabled = true;

                            newCell.SetGroup(-1);
                        
                            gridComponents.Add(newCell);
                        }
                    }
                }
            }

            UpdateGeneration();
            // StartCoroutine(CheckEntropy());
        }

        private void CreateDebugCell(WFCCell newCell)
        {
            var obj = Instantiate(wFCBuilder.wFCManager.debugTilePrefab, newCell.position, Quaternion.identity, wFCBuilder.transform);
            obj.transform.localPosition = newCell.position - new Vector3(0, 0.5f, 0);
            newCell.MBDebug = obj;
        }

        public void StartWFC()
        {
            auto = true;

            // UpdateGeneration();
            
            StartCoroutine(CheckEntropy());
            
            // StartLoopWFC().Forget();
        }

        public void StartStepWFC()
        {
            StartCoroutine(CheckEntropy());
            
        }
        // async UniTask StartLoopWFC()
        // {
        //     List<WFCCell> tempGrid = new List<WFCCell>(gridComponents);
        //     tempGrid.RemoveAll(c => c.disabled);
        //     tempGrid.Sort((a, b) => { return a.position.y - b.position.y; });
        //     foreach(var cell in tempGrid)
        //     {
        //         CreateTile(cell.position);
        //         await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f));
        //     }
        // }


        IEnumerator CheckEntropy()
        {
            List<WFCCell> tempGrid = new List<WFCCell>(gridComponents);
            tempGrid.RemoveAll(c => c.collapsed);
            tempGrid.RemoveAll(c => c.tileOptions.Length == 0);
    //         var a = tempGrid.Count(c => c.tileOptions.Length == 0);
    //         var b = tempGrid.Count();
    // Debug.Log($"null options of {a}[{b}]");
            // tempGrid.RemoveAll(c => c.tileOptions.Length == 0);

            tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

            if (tempGrid.Count > 0)
            {
                int arrLength = tempGrid[0].tileOptions.Length;
                int stopIndex = default;

                for (int i = 1; i < tempGrid.Count; i++)
                {
                    if (tempGrid[i].tileOptions.Length > arrLength)
                    {
                        stopIndex = i;
                        break;
                    }
                }

                if (stopIndex > 0)
                {
                    tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
                }

                yield return new WaitForSeconds(0.0001f);

                CollapseCell(tempGrid);
            }  else
            {
                Debug.LogWarning($"Завершена генерация тайлов! Все ячейки обработаны!");
                
                AfterCreate();
            }
        }

        void CollapseCell(List<WFCCell> tempGrid)
        {
            int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);

            WFCCell cellToCollapse = tempGrid[randIndex];

            // cellToCollapse.collapsed = true;
            // Tile3D selectedTile = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
            // cellToCollapse.tileOptions = new Tile3D[] { selectedTile };

            // Tile3D foundTile = cellToCollapse.tileOptions[0];
            // Tile3D go = Instantiate(foundTile, cellToCollapse.position, Quaternion.identity);
            // cellToCollapse.SetMBObject(go);
            InstantiatePrefabToCell(cellToCollapse);

            UpdateGeneration();
        }
        
        void InstantiatePrefabToCell(WFCCell cellToCollapse)
        {
            cellToCollapse.collapsed = true;

            if (cellToCollapse.tileOptions.Length > 0) {
                var index = UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length);
                // Debug.Log($"InstantiatePrefabToCell (cell: {cellToCollapse.position}): index={index} из возможных={cellToCollapse.tileOptions.Length}");
                
                // Tile3D selectedTile = cellToCollapse.tileOptions[index];
                // if (auto)
                // {
                // }
                    Tile3D selectedTile = WFCHelpers.GetRandomTile(cellToCollapse.tileOptions);
                
                cellToCollapse.tileOptions = new Tile3D[] { selectedTile };

                Tile3D foundTile = cellToCollapse.tileOptions[0];
                var obj = Instantiate(foundTile, cellToCollapse.position, Quaternion.identity, WrapperTiles.transform);
                obj.transform.localPosition = cellToCollapse.position - new Vector3(0, 0.5f, 0);
                obj.transform.rotation = Quaternion.Euler(0, foundTile.tileSockets.rotation, 0);
                obj.transform.localScale = wFCBuilder.wFCManager.scaleTiles;
                obj.voxelMeshRender.DisableCollider();
                obj.voxelMeshRender.Init();

                cellToCollapse.SetMBObject(obj);
            }
        }

        void UpdateGeneration()
        {
            List<WFCCell> newGenerationCell = new List<WFCCell>(gridComponents);

            for (int y = 0; y < wFCBuilder.size.y; y++)
            {
                for (int x = 0; x < wFCBuilder.size.x; x++)
                {
                    for (int z = 0; z < wFCBuilder.size.z; z++)
                    {
                        var index = WFCHelpers.From3DTo1D(x, y, z, wFCBuilder.size);
                        
                        if (gridComponents[index].collapsed)
                        {
                            // Debug.Log("called");
                            newGenerationCell[index] = gridComponents[index];
                        }
                        else
                        {
                            List<Tile3D> options = new List<Tile3D>();
                            foreach (Tile3D t in tileObjects)
                            {
                                options.Add(t);
                            }
                            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Мы берём ТЕКУЩИЕ tileOptions этой ячейки,
                            // а не забиваем заново весь массив tileObjects!
                            //List<Tile3D> options = new List<Tile3D>(gridComponents[index].tileOptions);

                            // Запускаем оригинальную фильтрацию IsTilePossible
                            options.RemoveAll(t => !IsTilePossible(t, new Vector3Int(x, y, z), gridComponents[index]));

                            // //update above
                            // if (y > 0)
                            // {
                            //     WFCCell up = gridComponents[x + (y - 1) * dimensions];
                            //     List<Tile3D> validOptions = new List<Tile3D>();

                            //     foreach (Tile3D possibleOptions in up.tileOptions)
                            //     {
                            //         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            //         var valid = tileObjects[valOption].upNeighbours;

                            //         validOptions = validOptions.Concat(valid).ToList();
                            //     }

                            //     CheckValidity(options, validOptions);
                            // }

                            // //update right
                            // if (x < dimensions - 1)
                            // {
                            //     WFCCell right = gridComponents[x + 1 + y * dimensions];
                            //     List<Tile3D> validOptions = new List<Tile3D>();

                            //     foreach (Tile3D possibleOptions in right.tileOptions)
                            //     {
                            //         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            //         var valid = tileObjects[valOption].leftNeighbours;

                            //         validOptions = validOptions.Concat(valid).ToList();
                            //     }

                            //     CheckValidity(options, validOptions);
                            // }

                            // //look down
                            // if (y < dimensions - 1)
                            // {
                            //     WFCCell down = gridComponents[x + (y + 1) * dimensions];
                            //     List<Tile3D> validOptions = new List<Tile3D>();

                            //     foreach (Tile3D possibleOptions in down.tileOptions)
                            //     {
                            //         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            //         var valid = tileObjects[valOption].downNeighbours;

                            //         validOptions = validOptions.Concat(valid).ToList();
                            //     }

                            //     CheckValidity(options, validOptions);
                            // }

                            // //look left
                            // if (x > 0)
                            // {
                            //     WFCCell left = gridComponents[x - 1 + y * dimensions];
                            //     List<Tile3D> validOptions = new List<Tile3D>();

                            //     foreach (Tile3D possibleOptions in left.tileOptions)
                            //     {
                            //         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            //         var valid = tileObjects[valOption].rightNeighbours;

                            //         validOptions = validOptions.Concat(valid).ToList();
                            //     }

                            //     CheckValidity(options, validOptions);
                            // }

                            //Tile3D[] newTileList = new Tile3D[options.Count];

                            //for (int i = 0; i < options.Count; i++)
                            //{
                            //    newTileList[i] = options[i];
                            //}

                            newGenerationCell[index].RecreateCell(options.ToArray());
                        }
                    }
                }
            }

            gridComponents = newGenerationCell;
            iterations++;

            if (auto)
            {
                if (iterations < maxCountIterations)
                {
                    StartCoroutine(CheckEntropy());
                } else
                {
                    Debug.LogWarning($"Завершена генерация тайлов! Количество итераций ({iterations}) превысило разрешенное({maxCountIterations})!");
                    AfterCreate();
                }
            }
        }

        void AfterCreate()
        {
            auto = false;
            // if (countRemoveEmptyCells >= maxCountRemoveEmptyCells)
            // {
            //     auto = false;
            //     return;
            // }
            // countRemoveEmptyCells = countRemoveEmptyCells + 1;

            // List<WFCCell> temp = new List<WFCCell>(gridComponents);
            // temp.RemoveAll(t => t.tileOptions.Length != 0 || t.disabled);

            // Debug.Log($"Найдено {temp.Count} пустых ячеек!");
            // foreach (var cell in temp)
            // {
            //     // var obj = Instantiate(wFCManager.emptyTilePrefab, cell.position, Quaternion.identity, transform);
            //     // obj.transform.localPosition = cell.position - new Vector3(0, 0.5f, 0);
            //     // obj.transform.localScale = scaleTiles;
            //     // placeholders.Add(obj);

            //     // находим соседей и удаляем для перегенерации.
            //     RemoveNeighbours(cell);
            // }

            // if (auto)
            // {
            //     int countIterations = gridComponents.Count(t => !t.collapsed && !t.disabled);
            //     Debug.Log($"Новое количество итераций {countIterations}!");
            //     maxCountIterations = countIterations;
            //     iterations = 0;
            //     StartCoroutine(CheckEntropy());
            // }
        }

        private void RemoveNeighbours(WFCCell cell)
        {
            Vector3Int position = cell.position;
            int maxIndex = wFCBuilder.size.x * wFCBuilder.size.y * wFCBuilder.size.z;

            int index = WFCHelpers.From3DTo1D(position.x - 1, position.y, position.z, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }

            index = WFCHelpers.From3DTo1D(position.x + 1, position.y, position.z, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }

            index = WFCHelpers.From3DTo1D(position.x, position.y - 1, position.z, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }

            index = WFCHelpers.From3DTo1D(position.x, position.y + 1, position.z, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }

            index = WFCHelpers.From3DTo1D(position.x, position.y, position.z - 1, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }

            index = WFCHelpers.From3DTo1D(position.x, position.y, position.z + 1, wFCBuilder.size);
            if (index >= 0 && index < maxIndex)
            {
                RemoveTile(gridComponents[index].position);
            }
        }

        private bool IsTilePossible(Tile3D tile, Vector3Int position, WFCCell wFCCell)
        {
            // var dimensions = _gameManager.LevelConfig.gridSize.z;
            int x = position.x;
            int y = position.y;
            int z = position.z;
            int index = default;

            int maxIndex = wFCBuilder.size.x * wFCBuilder.size.y * wFCBuilder.size.z;


            if (x - 1 < 0)
            {
                if (!CanAppendTile(wFCBuilder.wFCManager.emptyTilePrefab, tile, DirectionSideTile.Left))
                {
                    return false;
                }
            } else
            {
                index = WFCHelpers.From3DTo1D(x - 1, y, z, wFCBuilder.size);
                if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
                {
                    bool isAllLeftImpossible = gridComponents[index].tileOptions
                        .All(t => !CanAppendTile(t, tile, DirectionSideTile.Left));
                    if (isAllLeftImpossible) return false;
                }
            }
            
            if (x + 1 >= wFCBuilder.size.x)
            {
                if (!CanAppendTile(wFCBuilder.wFCManager.emptyTilePrefab, tile, DirectionSideTile.Right))
                {
                    return false;
                }
            } else
            {
                index = WFCHelpers.From3DTo1D(x + 1, y, z, wFCBuilder.size);
                if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
                {
                    bool isAllRightImpossible = gridComponents[index].tileOptions // possibleTiles[position.x + 1, position.z]
                        .All(t => !CanAppendTile(t, tile, DirectionSideTile.Right));
                    if (isAllRightImpossible) return false;
                }
            }

            if (z - 1 < 0)
            {
                if (!CanAppendTile(wFCBuilder.wFCManager.emptyTilePrefab, tile, DirectionSideTile.Forward))
                {
                    return false;
                }
            } else
            {
                index = WFCHelpers.From3DTo1D(x, y, z - 1, wFCBuilder.size);
                if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
                {
                    bool isAllForwardImpossible = gridComponents[index].tileOptions // possibleTiles[position.x, position.z - 1]
                        .All(t => !CanAppendTile(t, tile, DirectionSideTile.Forward));
                    if (isAllForwardImpossible) return false;
                }
            }

            if (z + 1 >= wFCBuilder.size.z)
            {
                if (!CanAppendTile(wFCBuilder.wFCManager.emptyTilePrefab, tile, DirectionSideTile.Back))
                {
                    return false;
                }
            } else
            {
                index = WFCHelpers.From3DTo1D(x, y, z + 1, wFCBuilder.size);
                if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
                {
                    bool isAllBackImpossible = gridComponents[index].tileOptions // possibleTiles[position.x, position.z + 1]
                        .All(t => !CanAppendTile(t, tile, DirectionSideTile.Back));
                    if (isAllBackImpossible) return false;
                }
            }

            index = WFCHelpers.From3DTo1D(x, y - 1, z, wFCBuilder.size);
            // if (wFCCell.height > 2) {
            // }
            // проверка возможности установить нижний тайл на землю.
            if (y - 1 < 0 && !tile.isGround)
            {
                return false;
            }
            // чтобы исключить установку тайла для земли на верхних уровнях.
            if (y - 1 >= 0 && tile.isGround)
            {
                return false;
            }
            if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
            {
                bool isAllBottomImpossible = gridComponents[index].tileOptions // possibleTiles[position.x, position.z + 1]
                    .All(t => !CanAppendTile(t, tile, DirectionSideTile.Bottom));
                if (isAllBottomImpossible) return false;
            }
            if (y + 1 >= wFCCell.height)
            {
                if (!CanAppendTile(wFCBuilder.wFCManager.emptyTilePrefab, tile, DirectionSideTile.Top))
                {
                    return false;
                }
            } else
            {
                // if (tile.isTop)
                // {
                //     return false;
                // }
                index = WFCHelpers.From3DTo1D(x, y + 1, z, wFCBuilder.size);
                if (index >= 0 && index < maxIndex && gridComponents[index].collapsed)
                {
                    bool isAllTopImpossible = gridComponents[index].tileOptions // possibleTiles[position.x, position.z + 1]
                        .All(t => !CanAppendTile(t, tile, DirectionSideTile.Top));
                    if (isAllTopImpossible) return false;
                }
            }

            return true;
        }

    /// <summary>
    /// проверка могут ли соединится тайлы
    /// </summary>
    /// <param name="existingTile">тайл, из ячейки, которая уже сформирована (collapsed)</param>
    /// <param name="tileToAppend">тайл, который проверяется, можно ли поставить в текущую ячейку</param>
    /// <param name="direction">направление проверки</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentException"></exception>
        private bool CanAppendTile(Tile3D existingTile, Tile3D tileToAppend, DirectionSideTile direction)
        {
            //
            
            // if (existingTile == null) return true;

            if (direction == DirectionSideTile.Left)
            {
                return existingTile.tileSockets.posX == tileToAppend.tileSockets.negX;
            }
            else if (direction == DirectionSideTile.Right)
            {
                return existingTile.tileSockets.negX == tileToAppend.tileSockets.posX;
            }
            else if (direction == DirectionSideTile.Forward)
            {
                return existingTile.tileSockets.posZ == tileToAppend.tileSockets.negZ;
            }
            else if (direction == DirectionSideTile.Back)
            {
                return existingTile.tileSockets.negZ == tileToAppend.tileSockets.posZ;
            }
            else if (direction == DirectionSideTile.Top)
            {
                return existingTile.tileSockets.negY == tileToAppend.tileSockets.posY;
            }
            else if (direction == DirectionSideTile.Bottom)
            {
                return existingTile.tileSockets.posY == tileToAppend.tileSockets.negY;
            }
            else
            {
                throw new System.ArgumentException("Wrong direction value, should be Vector3.left/right/back/forward",
                    nameof(direction));
            }
        }

        // void CheckValidity(List<Tile3D> optionList, List<Tile3D> validOption)
        // {
        //     for (int x = optionList.Count - 1; x >= 0; x--)
        //     {
        //         var element = optionList[x];
        //         if (!validOption.Contains(element))
        //         {
        //             optionList.RemoveAt(x);
        //         }
        //     }
        // }

        public void LoadTile(Cell3DData data, Tile3D tile, int group = 0)
        {
            int index = WFCHelpers.From3DTo1D((int)data.position.x, (int)data.position.y, (int)data.position.z, wFCBuilder.size);

            WFCCell cellToCollapse = gridComponents[index];
            // Debug.Log($"CreateTile:  {cellToCollapse.position}: index={index}: collapsed={cellToCollapse.collapsed}");

            if (cellToCollapse.collapsed)
            {
                RemoveTile(data.position);
                Debug.LogWarning($"Ячейка для загрузки уже закрыта! Производим удаление!");
                return;
            }

            cellToCollapse.RecreateCell(new Tile3D[] {tile});
            cellToCollapse.SetGroup(group);
            cellToCollapse.SetTypeCell(data.typeCell);

            if (cellToCollapse.tileOptions.Length > 0)
            {
                InstantiatePrefabToCell(cellToCollapse);

                if (cellToCollapse.MBDebug)
                {
                    Destroy(cellToCollapse.MBDebug);
                    cellToCollapse.MBDebug = null;
                }
            } else
            {
                Debug.LogWarning($"Не найдено подходящих тайлов!");
            }
        }

        public void CreateTile(Vector3 position)
        {
            int index = WFCHelpers.From3DTo1D((int)position.x, (int)position.y, (int)position.z, wFCBuilder.size);

            WFCCell cellToCollapse = gridComponents[index];
            // Debug.Log($"CreateTile:  {cellToCollapse.position}: index={index}: collapsed={cellToCollapse.collapsed}");

            if (cellToCollapse.collapsed)
            {
                Debug.LogWarning($"Ячейка уже закрыта!");
                return;
            }

            if (cellToCollapse.tileOptions.Length > 0)
            {
                InstantiatePrefabToCell(cellToCollapse);

                UpdateGeneration();

                if (cellToCollapse.MBDebug)
                {
                    Destroy(cellToCollapse.MBDebug);
                    cellToCollapse.MBDebug = null;
                }
            } else
            {
                Debug.LogWarning($"Не найдено подходящих тайлов!");
            }
        }

        public void RemoveTile(Vector3 position)
        {
            int index = WFCHelpers.From3DTo1D((int)position.x, (int)position.y, (int)position.z, wFCBuilder.size);

            if (index > 0)
            {
                WFCCell cellToRemove = gridComponents[index];

                if (!cellToRemove.disabled)
                {
                    cellToRemove.collapsed = false;
                    cellToRemove.RecreateCell(tileObjects);

                    Debug.Log($"Remove cell {position}: {index}");

                    if (cellToRemove.MBObject)
                    {
                        Destroy(cellToRemove.MBObject.gameObject);
                        cellToRemove.MBObject = null;
                    }

                    // if (cellToRemove.MBDebug)
                    // {
                    //     Destroy(cellToRemove.MBDebug);
                    //     cellToRemove.MBDebug = null;
                    // } else
                    // {
                    //     CreateDebugCell(cellToRemove);
                    // }
                    
                    UpdateGeneration();
                } else 
                {
                    Debug.LogWarning($"Ячейка заблокирована!");
                    return;
                }
            
            }
        }

        public void ResetTiles()
        {
            List<WFCCell> tempGrid = new List<WFCCell>(gridComponents);
            tempGrid.RemoveAll(i => i.disabled);

            foreach (WFCCell cell in tempGrid)
            {
                // if (cell.disabled)
                // {
                //     cell.RecreateCell(new Tile3D[] { wFCManager.emptyTilePrefab });
                //     cell.disabled = true;
                //     cell.collapsed = cell.disabled;
                // } else
                // {
                // }
                    cell.RecreateCell(tileObjects);
                    cell.collapsed = false;

                if (cell.MBObject)
                {
                    Destroy(cell.MBObject.gameObject);
                }
            }

            UpdateGeneration();
        }

        public virtual List<DataGroupTiles> OnSaveTiled()
        {
            List<DataGroupTiles> levelDataGroups = new List<DataGroupTiles>();

            var tempGrid = new List<WFCCell>(gridComponents);

            tempGrid.RemoveAll(x => x.disabled);

            var groupedCells = tempGrid.GroupBy(t => t.groupNumber);


            foreach (var group in groupedCells)
            {
                List<Cell3DData> tilesGroup = new List<Cell3DData>();

                // не сохраняем пустые ячейки из группы с индексом 0.
                if (group.Key == -1)
                {
                    continue;
                }

                foreach (var cell in group)
                {
                if (!cell.disabled) {
                        Tile3D uidPrefabTile = cell.tileOptions.Length > 0 ? cell.tileOptions[0] : null;
                        if (cell.tileOptions.Length == 0 && cell.typeCell != TypeEntity.Tree)
                        {
                            Debug.LogWarning($"Не удалось найти подходящий префаб для позиции: {cell.position}!");
                            
                        }

                        // if (!gridComponents[i].collapsed)
                        // {
                        //      Tile3D foundTile = TilePlaceholderNone;
                        //     Instantiate(foundTile, gridComponents[i].position, foundTile.transform.rotation, transform);
                        // }

                        // GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(gridComponents[i].position.x, gridComponents[i].position.y, gridComponents[i].position.z);   
                        if ((uidPrefabTile && cell.MBObject)) //  || cell.typeCell == TypeEntity.Tree
                        {
                            tilesGroup.Add(new Cell3DData()
                            {
                                position = cell.position,
                                uid = uidPrefabTile != null ? uidPrefabTile.name : "",
                                // stateNode = (int)node.StateNode,
                                RotationY = uidPrefabTile != null ? cell.MBObject.transform.eulerAngles.y : 0,
                                typeCell = wFCBuilder.typeCell
                            });
                        }
                    } 
                };

                levelDataGroups.Add(new DataGroupTiles()
                {
                    group = group.Key,
                    team = 0,
                    tiles = tilesGroup,
                });
            };

            // for (int i = 0; i < gridComponents.Count; i++)
            // {
            //     if (!gridComponents[i].disabled) {
            //         Tile3D uidPrefabTile = gridComponents[i].tileOptions.Length > 0 ? gridComponents[i].tileOptions[0] : null;
            //         if (gridComponents[i].tileOptions.Length == 0)
            //         {
            //             Debug.LogWarning($"Не удалось найти подходящий префаб для позиции: {gridComponents[i].position}! Индекс в массиве -  {i}");
                        
            //         }

            //         // if (!gridComponents[i].collapsed)
            //         // {
            //         //      Tile3D foundTile = TilePlaceholderNone;
            //         //     Instantiate(foundTile, gridComponents[i].position, foundTile.transform.rotation, transform);
            //         // }

            //         // GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(gridComponents[i].position.x, gridComponents[i].position.y, gridComponents[i].position.z);   
            //         if (uidPrefabTile && gridComponents[i].MBObject)
            //         {
            //             cellsData.Add(new Cell3DData()
            //             {
                            
            //                 position = gridComponents[i].position,
            //                 uid = uidPrefabTile != null ? uidPrefabTile.name : "",
            //                 // stateNode = (int)node.StateNode,
            //                 RotationY = uidPrefabTile != null ? gridComponents[i].MBObject.transform.eulerAngles.y : 0
            //             });
            //         }
            //     }
            // }

            // _gameManager.LevelConfig.saveTiled = new SaveTiled()
            // {
            //     nameMap = _gameManager.LevelConfig.tileSettings.nameMap,
            //     gridComponents = cellsData
            // };
            return levelDataGroups;
        }

        public virtual void Load()
        {
            
        }

        public void LoadTiles(List<DataGroupTiles> groups)
        {
            foreach (var group in groups)
            {
                foreach (Cell3DData data in group.tiles)
                {
                    if (data.uid != "")
                    {
                        var tile = wFCBuilder.tilePrefabs.Find(x => x.name == data.uid);
                        if (tile)
                        {
                            LoadTile(data, tile, group.group);
                        }
                    }
                }
            }
        }
    }
}
#endif