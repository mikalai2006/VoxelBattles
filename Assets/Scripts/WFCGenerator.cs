using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mikalai2006.VoxelMap;
using UnityEngine;

public class WFCGenerator : MonoBehaviour {
    public static event System.Action<string> OnSetNotify;
    public static event System.Action<float> OnAddProgress;
    public static event System.Action OnCompleteTiled;
    GameManager _gameManager => GameManager.Instance;
    public MapManager mapManager;
    public List<Tile3D> TilePrefabsEmpty;
    public List<Tile3D> TilePrefabsInner;
    public List<Tile3D> TilePrefabsInnerTop;
    System.Threading.CancellationTokenSource cts;

    void Awake()
    {
        cts = new System.Threading.CancellationTokenSource();
    }

    void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    // public void OnUpdateColors()
    // {
    //     for (int i = 0; i < _gameManager.LevelConfig.TilePrefabs.Count; i++)
    //     {
    //         var vmRenderer = _gameManager.LevelConfig.TilePrefabs[i].GetComponentInChildren<VoxelMeshRender>();

    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 0)
    //         {
    //             var a = vmRenderer.Config.sOVoxelData.groups[0];
    //             a.color = _gameManager.LevelConfig.colorWall;
    //             vmRenderer.Config.sOVoxelData.groups[0] = a;
    //         }
    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 1)
    //         {
    //             var b = vmRenderer.Config.sOVoxelData.groups[1];
    //             b.color = _gameManager.LevelConfig.colorNature;
    //             vmRenderer.Config.sOVoxelData.groups[1] = b;
    //         }
    //     }

    //     for (int i = 0; i < _gameManager.LevelConfig.TilePrefabsInner.Count; i++)
    //     {
    //         var vmRenderer = _gameManager.LevelConfig.TilePrefabsInner[i].GetComponentInChildren<VoxelMeshRender>();

    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 0)
    //         {
    //             var a = vmRenderer.Config.sOVoxelData.groups[0];
    //             a.color = _gameManager.LevelConfig.colorWall;
    //             vmRenderer.Config.sOVoxelData.groups[0] = a;
    //         }
    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 1)
    //         {
    //             var b = vmRenderer.Config.sOVoxelData.groups[1];
    //             b.color = _gameManager.LevelConfig.colorNature;
    //             vmRenderer.Config.sOVoxelData.groups[1] = b;
    //         }
    //     }

        
    //     for (int i = 0; i < _gameManager.LevelConfig.TilePrefabsInnerTop.Count; i++)
    //     {
    //         var vmRenderer = _gameManager.LevelConfig.TilePrefabsInnerTop[i].GetComponentInChildren<VoxelMeshRender>();

    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 0)
    //         {
    //             var a = vmRenderer.Config.sOVoxelData.groups[0];
    //             a.color = _gameManager.LevelConfig.colorWall;
    //             vmRenderer.Config.sOVoxelData.groups[0] = a;
    //         }
    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 1)
    //         {
    //             var b = vmRenderer.Config.sOVoxelData.groups[1];
    //             b.color = _gameManager.LevelConfig.colorNature;
    //             vmRenderer.Config.sOVoxelData.groups[1] = b;
    //         }
    //     }
    //     for (int i = 0; i < _gameManager.LevelConfig.TilePrefabsEmpty.Count; i++)
    //     {
    //         var vmRenderer = _gameManager.LevelConfig.TilePrefabsEmpty[i].GetComponentInChildren<VoxelMeshRender>();

    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 0)
    //         {
    //             var a = vmRenderer.Config.sOVoxelData.groups[0];
    //             a.color = _gameManager.LevelConfig.colorWall;
    //             vmRenderer.Config.sOVoxelData.groups[0] = a;
    //         }
    //         if (vmRenderer.Config.sOVoxelData.groups.Count > 1)
    //         {
    //             var b = vmRenderer.Config.sOVoxelData.groups[1];
    //             b.color = _gameManager.LevelConfig.colorNature;
    //             vmRenderer.Config.sOVoxelData.groups[1] = b;
    //         }
    //     }

    //     VoxelMeshRender vmGround = _gameManager.LevelConfig.planePrefab;

    //     if (vmGround.Config.sOVoxelData.groups.Count > 0)
    //     {
    //         var a = vmGround.Config.sOVoxelData.groups[0];
    //         a.color = _gameManager.LevelConfig.colorWall;
    //         vmGround.Config.sOVoxelData.groups[0] = a;
    //     }
    //     if (vmGround.Config.sOVoxelData.groups.Count > 1)
    //     {
    //         var b = vmGround.Config.sOVoxelData.groups[1];
    //         b.color = _gameManager.LevelConfig.colorNature;
    //         vmGround.Config.sOVoxelData.groups[1] = b;
    //     }
    // }

    // public void OnCreateVariantsPrefabs()
    // {
    //     OnSetNotify?.Invoke("createVariantsTiles");
        
    //     // // создаем префабы.
    //     // foreach (var obj in _gameManager.LevelConfig.TilePrefabs)
    //     // {
    //     //     var clone = Instantiate(obj, new Vector3(0,100,0), Quaternion.identity, transform);
    //     //     clone.name = $"{obj.UID}";
    //     //     clone.OnStart();
    //     //     TilePrefabs.Add(clone);
    //     // }
        
    //     // // создаем префабы inner.
    //     // foreach (var obj in _gameManager.LevelConfig.TilePrefabsInner)
    //     // {
    //     //     var clone = Instantiate(obj, new Vector3(0,100,0), Quaternion.identity, transform);
    //     //     clone.name = $"{obj.UID}";
    //     //     clone.OnStart();
    //     //     TilePrefabsInner.Add(clone);
    //     // }
    //     // foreach (var obj in _gameManager.LevelConfig.TilePrefabsInnerTop)
    //     // {
    //     //     var clone = Instantiate(obj, new Vector3(0,100,0), Quaternion.identity, transform);
    //     //     clone.name = $"{obj.UID}";
    //     //     clone.OnStart();
    //     //     TilePrefabsInnerTop.Add(clone);
    //     // }
        
    //     int countBeforeAdding = _gameManager.LevelConfig.TilePrefabs.Count;

    //     for (int i = 0; i < countBeforeAdding; i++)
    //     {

    //         Vector3 pos = new Vector3(0, -100, 0);
    //         Tile3D tile = _gameManager.LevelConfig.TilePrefabs[i];

    //         // var a = tile.meshConfig.sOVoxelData.groups[0];
    //         // a.color = _gameManager.LevelConfig.colorGround;
    //         // tile.meshConfig.sOVoxelData.groups[0] = a;
    //         // if (tile.meshConfig.sOVoxelData.groups.Count > 1)
    //         // {
    //         //     var b = tile.meshConfig.sOVoxelData.groups[1];
    //         //     a.color = _gameManager.LevelConfig.colorNature;
    //         //     tile.meshConfig.sOVoxelData.groups[1] = b;
    //         // }
    //         // tile.OnRefreshData();

    //         switch (tile.meshConfig.sOVoxelData.Rotation)
    //         {
    //             case RotationType.OnlyRotation:
    //                 break;

    //             case RotationType.TwoRotations:
    //                 // tile.Weight /= 2;
    //                 // if (tile.Weight <= 0) tile.Weight = 1;

    //                 var clone = Instantiate(tile, pos,// + Vector3.right,
    //                     Quaternion.identity, transform);
    //                 clone.name = $"{tile.UID}_90";
    //                 clone.OnStart();
    //                 clone.Rotate90();
    //                 TilePrefabs.Add(clone);
    //                 break;

    //             case RotationType.FourRotations:
    //                 // tile.Weight /= 4;
    //                 // if (tile.Weight <= 0) tile.Weight = 1;

    //                 var clone2 = Instantiate(tile, pos,// + Vector3.right,
    //                     Quaternion.identity, transform);
    //                 clone2.name = $"{tile.UID}_90";
    //                 clone2.OnStart();
    //                 clone2.Rotate90();
    //                 TilePrefabs.Add(clone2);

    //                 var clone3 = Instantiate(tile, pos,// + Vector3.right * 2,
    //                     Quaternion.identity, transform);
    //                 clone3.name = $"{tile.UID}_180";
    //                 clone3.OnStart();
    //                 clone3.Rotate90();
    //                 clone3.Rotate90();
    //                 TilePrefabs.Add(clone3);

    //                 var clone4 = Instantiate(tile, pos,// + Vector3.right * 3,
    //                     Quaternion.identity, transform);
    //                 clone4.name = $"{tile.UID}_270";
    //                 clone4.OnStart();
    //                 clone4.Rotate90();
    //                 clone4.Rotate90();
    //                 clone4.Rotate90();
    //                 TilePrefabs.Add(clone4);
    //                 break;
    //             default:
    //                 throw new ArgumentOutOfRangeException();
    //         }
    //     }
    // }

    public async UniTask OnGenerateTiles(System.Threading.CancellationTokenSource cancelToken)
    {
 
            if (!cancelToken.IsCancellationRequested)
            {
                Vector3Int sizeGridCells = new Vector3Int(_gameManager.LevelConfig.levelData.size.x,  _gameManager.LevelConfig.levelData.maxHeight,  _gameManager.LevelConfig.levelData.size.z);

                // gridComponents = new Cell3D[_gameManager.LevelConfig.levelData.size.x * _gameManager.LevelConfig.levelData.maxHeight * _gameManager.LevelConfig.levelData.size.z];

                // TilePrefabs.AddRange(_gameManager.LevelConfig.TilePrefabs);

                // if (_gameManager.LevelConfig.saveTiled.nameMap == _gameManager.LevelConfig.tileSettings.nameMap)
                // {
                OnSetNotify?.Invoke("loadMap");
                // если в сохраненных тайлах находится карта с текущим именем,
                // значит карта уже сгенерирована и осталось только создать игровые объекты.
                // List<Cell3DData> savedTiles = new List<Cell3DData>();
                List<Cell3DItemForCreate> stackCellNeedCreate = new List<Cell3DItemForCreate>();
                foreach (var item in _gameManager.LevelConfig.levelData.trees)
                {
                    //// savedTiles.AddRange(item.tiles);
                    //foreach (var cell3DData in item.tiles)
                    //{
                    //    VoxelMeshRender tree = _gameManager.LevelConfig.TreesPrefabs[Random.Range(0, _gameManager.LevelConfig.TreesPrefabs.Count)];
                    //    if (tree)
                    //    {
                    //        var obj = Instantiate(tree, cell3DData.position, Quaternion.identity, transform);
                    //        obj.name = $"{cell3DData.position.x}_{cell3DData.position.y}x{cell3DData.position.z}_{cell3DData.typeCell}__{tree.name}";
                    //        obj.transform.localRotation = Quaternion.Euler(0, cell3DData.RotationY, 0);
                    //    }
                    //}

                    // savedTiles.AddRange(item.tiles);
                    List<Cell3DItemForCreate> caveCells = GenerateTile(item, "tree");
                    stackCellNeedCreate.AddRange(caveCells);

                }
                foreach (var item in _gameManager.LevelConfig.levelData.caves)
                {
                    // savedTiles.AddRange(item.tiles);
                    List<Cell3DItemForCreate> caveCells = GenerateTile(item, "cave");
                    stackCellNeedCreate.AddRange(caveCells);
                }
                foreach (var item in _gameManager.LevelConfig.levelData.houses)
                {
                    // savedTiles.AddRange(item.tiles);
                    List<Cell3DItemForCreate> caveCells = GenerateTile(item, "house");
                    stackCellNeedCreate.AddRange(caveCells);
                }
                foreach (var item in _gameManager.LevelConfig.levelData.zabor)
                {
                    // savedTiles.AddRange(item.tiles);
                    List<Cell3DItemForCreate> caveCells = GenerateTile(item, "zabor");
                    stackCellNeedCreate.AddRange(caveCells);
                }

                // List<Cell3DStackItemForCreate> stackCellNeedCreate = new List<Cell3DStackItemForCreate>();
                // for (int i = 0; i < savedTiles.Count; i++)
                // {
                //     Cell3DData cell3DData = savedTiles[i];

                //     if (cell3DData.typeCell == TypeCell.Tree)
                //         // создание деревьев.
                //     {
                //         VoxelMeshRender tree = _gameManager.LevelConfig.TreesPrefabs[Random.Range(0, _gameManager.LevelConfig.TreesPrefabs.Count)];
                //         if (tree)
                //         {
                //             var obj = Instantiate(tree, cell3DData.position, Quaternion.identity, transform);
                //             obj.name = $"{cell3DData.position.x}_{cell3DData.position.y}x{cell3DData.position.z}_{cell3DData.typeCell}__{tree.name}";
                //             obj.transform.localRotation = Quaternion.Euler(0, cell3DData.RotationY, 0);
                //         }

                //     }
                //         // создание тайлов.
                //     else
                //     {
                //         List<Tile3D> tileForCell = _gameManager.LevelConfig.TilePrefabs.FindAll(x => x.name == cell3DData.uid);

                //         if (tileForCell.Count == 0)
                //         {
                //             Debug.LogWarning($"Не найден префаб для загрузки {cell3DData.uid}!");
                //         }

                //         Vector3Int position = new Vector3Int(cell3DData.position.x, cell3DData.position.y, cell3DData.position.z);

                //         GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(cell3DData.position.x, cell3DData.position.y, cell3DData.position.z);

                //         Cell3D loadCell = new Cell3D();

                //         loadCell.CreateCell(false, tileForCell, position, new Vector3(0, cell3DData.RotationY, 0));
                //         if (!EqualityComparer<GridTileNode>.Default.Equals(node, default(GridTileNode)))
                //         {
                //             node.isCollapsed = true;
                //         }
                //         // var index = Helpers.From3DTo1D(position.x, position.y, position.z, sizeGridCells);
                //         // gridComponents[index] = loadCell;
                //         stackCellNeedCreate.Add(loadCell);
                //     }
                // }

                await CreateTiles(cts, stackCellNeedCreate);

                OnCompleteTiled?.Invoke();
                // }
                // else
                // {
                //     OnSetNotify?.Invoke("generateMap");
                //     // генерируем карту и создаем игровые объекты.
                //     await InitializeGrid(cts);
                // }
            }

    }

    private List<Cell3DItemForCreate> GenerateTile(DataGroupTiles group, string prefix)
    {
        List<Cell3DItemForCreate> output = new List<Cell3DItemForCreate>();

        GameObject wrapper = new GameObject($"{prefix}__{group.group}");
        var tile3DGroup = wrapper.AddComponent<Tile3DGroup>();
        wrapper.transform.parent = transform;

        for (int i = 0; i < group.tiles.Count; i++)
        {
            Cell3DData cell3DData = group.tiles[i];

            // if (cell3DData.typeCell == TypeCell.Tree)
            //     // создание деревьев.
            // {
            //     VoxelMeshRender tree = _gameManager.LevelConfig.TreesPrefabs[Random.Range(0, _gameManager.LevelConfig.TreesPrefabs.Count)];
            //     if (tree)
            //     {
            //         var obj = Instantiate(tree, cell3DData.position, Quaternion.identity, transform);
            //         obj.name = $"{cell3DData.position.x}_{cell3DData.position.y}x{cell3DData.position.z}_{cell3DData.typeCell}__{tree.name}";
            //         obj.transform.localRotation = Quaternion.Euler(0, cell3DData.RotationY, 0);
            //     }

            // }
            //     // создание тайлов.
            // else
            // {
                List<Tile3D> tileForCell = _gameManager.LevelConfig.TilePrefabs.FindAll(x => x.name == cell3DData.uid);

                if (tileForCell.Count == 0)
                {
                    Debug.LogWarning($"Не найден префаб для загрузки {cell3DData.uid}!");
                }

                Vector3Int position = new Vector3Int(cell3DData.position.x, cell3DData.position.y, cell3DData.position.z);

                GridTileNode node = mapManager.gridTileHelper.GetNode(cell3DData.position.x, cell3DData.position.y, cell3DData.position.z);

                Cell3D loadCell = new Cell3D();

                loadCell.CreateCell(false, tileForCell, position, new Vector3(0, cell3DData.RotationY, 0));
                if (!EqualityComparer<GridTileNode>.Default.Equals(node, default(GridTileNode)))
                {
                    node.isCollapsed = true;
                }
                
                output.Add(new Cell3DItemForCreate
                {
                    cell3D = loadCell,
                    wrapper = wrapper,
                    tile3DGroup = tile3DGroup,
                });
            // }
        }

        return output;
    }

    private async UniTask CreateTiles(System.Threading.CancellationTokenSource cancelToken, List<Cell3DItemForCreate> stackCellNeedCreate)
    {
        int count = 25;
        float stepProgress = 0.6f / (stackCellNeedCreate.Count / count);

        OnSetNotify?.Invoke("createGameObjects");
        while (stackCellNeedCreate.Count > 0)
        {
            if (cancelToken.IsCancellationRequested)
            {
                break;
            } else
            {
                var cell = stackCellNeedCreate.First();
                // GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(cell.position.x, cell.position.y, cell.position.z);

                // if (!node.StateNode.HasFlag(StateNode.Empty))
                // {
                //     // Tile3D selectedTile = GetRandomTile(cell.tileOptions);
                //     // // cell.tileOptions = new Tile3D[] { selectedTile };
                //     // var obj = Instantiate(cell.tileOptions[0], cell.position, cell.tileOptions[0].transform.rotation, transform);
                //     // obj.name = $"{cell.position.x}x{cell.position.z}__{selectedTile.name}";
                // }
                InstantiateTile(cell);

                stackCellNeedCreate.Remove(cell);

                count--;

                if (count < 0)
                {
                    count = 25;
                    await UniTask.Delay(System.TimeSpan.FromSeconds(0.00001f));
                    OnAddProgress?.Invoke(stepProgress);
                }
            }
        }
    }

    private void InstantiateTile(Cell3DItemForCreate cellToCollapse)
    {
        if (cellToCollapse.cell3D.tileOptions.Length > 0)
        {
            // Tile3D[] availableTiles = cellToCollapse.tileOptions.Where(t => t.isTop == cellToCollapse.isTop).ToArray();
            
            // if (cellToCollapse.isTop)
            // {
            //     availableTiles = cellToCollapse.tileOptions.Where(t => t.isTop).ToArray();
            // }

            Tile3D selectedTile = WFCHelpers.GetRandomTile(cellToCollapse.cell3D.tileOptions);
            
            if (selectedTile)
            {
                // GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(cellToCollapse.position.x, cellToCollapse.position.y, cellToCollapse.position.z);
                // node.isCollapsed = true;
                //cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
                // cellToCollapse.tileOptions = new Tile3D[] { selectedTile };
                // cellToCollapse.rotation = selectedTile.transform.eulerAngles;

                Tile3D foundTile = cellToCollapse.cell3D.tileOptions[0];
                if (foundTile)
                {
                    Tile3D obj = Instantiate(foundTile, cellToCollapse.cell3D.position, foundTile.transform.rotation, transform);
                    obj.name = $"{cellToCollapse.cell3D.position.x}x{cellToCollapse.cell3D.position.y}x{cellToCollapse.cell3D.position.z}__{selectedTile.name}";
                    obj.transform.localRotation = Quaternion.Euler(cellToCollapse.cell3D.rotation);
                    obj.transform.parent = cellToCollapse.wrapper.transform;
                    obj.voxelMeshRender.SetColorsModify(_gameManager.LevelConfig.colorsModify);
                    obj.voxelMeshRender.Init();
                    // Container[] containers = obj.gameObject.GetComponentsInChildren<Container>();
                    // foreach (var item in containers)
                    // {
                    //     item.gameObject.layer = LayerMask.NameToLayer("Wall");
                    // }
                    cellToCollapse.tile3DGroup.AddTile(obj);
                }
            } else
            {
                Debug.LogWarning("Не найден префаб для создания тайла!");
            }
        }
    }

    // private void OnSaveTiled()
    // {
    //     Cell3DData[] cellsData = new Cell3DData[gridComponents.Length];

    //     for (int i = 0; i < gridComponents.Length; i++)
    //     {
    //         Tile3D uidPrefabTile = gridComponents[i].tileOptions.Length > 0 ? gridComponents[i].tileOptions[0] : null;
    //         if (gridComponents[i].tileOptions.Length == 0)
    //         {
    //             Debug.LogWarning($"Не удалось найти подходящий префаб для позиции: {gridComponents[i].position}! Индекс в массиве -  {i}");
                
    //         }

    //         if (!gridComponents[i].collapsed)
    //         {
    //              Tile3D foundTile = TilePlaceholderNone;
    //             Instantiate(foundTile, gridComponents[i].position, foundTile.transform.rotation, transform);
    //         }

    //         GridTileNode node = levelManager.mapManager.gridTileHelper.GetNode(gridComponents[i].position.x, gridComponents[i].position.y, gridComponents[i].position.z);   
    //         cellsData[i] = new Cell3DData()
    //         {
    //             position = gridComponents[i].position,
    //             uid = uidPrefabTile != null ? uidPrefabTile.UID : "",
    //             stateNode = (int)node.StateNode,
    //             RotationY = uidPrefabTile != null ? uidPrefabTile.transform.eulerAngles.y : 0
    //         };
    //     }

    //     _gameManager.LevelConfig.saveTiled = new SaveTiled()
    //     {
    //         nameMap = _gameManager.LevelConfig.tileSettings.nameMap,
    //         gridComponents = cellsData
    //     };
    // }

}