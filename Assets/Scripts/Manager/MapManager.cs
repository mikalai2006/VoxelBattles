using Mikalai2006.VoxelBase;
using Mikalai2006.VoxelMap;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public static event Action<string> OnSetNotify;
    public static event Action OnCompleteBakeMap;
    private GameManager _gameManager => GameManager.Instance;
    private LevelManager _levelManager;
    private GameSetting _gameSetting => GameManager.Instance.Settings;
    // [SerializeField] private ParserHeight ParserHeight;
    public GridTileHelper gridTileHelper;
    [SerializeField] Tilemap map;
    // public Tilemap Map => map;
    public GameObject plane;
    // [SerializeField] Tilemap mapBorder;
    // [SerializeField] Tilemap mapObjects;
    // [SerializeField] Tilemap mapDamages;
    public NavMeshSurface NavMeshSurface;

    public void OnInit(LevelManager levelManager)
    {
        _levelManager = levelManager;
    }

    void Awake()
    {
        WFCGenerator.OnCompleteTiled += OnBakeMap;
    }

    void OnDestroy()
    {

        WFCGenerator.OnCompleteTiled -= OnBakeMap;
    }
    
    private void OnBakeMap()
    {
        NavMeshSurface.BuildNavMesh();

        OnCompleteBakeMap?.Invoke();
    }

    public void CreateMap()
    {
        OnSetNotify?.Invoke("createMap");
        // ParserHeight.SetConfig(_gameManager.LevelConfig.tileSettings);
        // ParserHeight.Init();
        // _gameManager.LevelConfig.gridSize = new Vector3Int(ParserHeight.gridSize.x, _gameManager.LevelConfig.tileSettings.heightSize, ParserHeight.gridSize.y);

        gridTileHelper = new GridTileHelper(_gameManager.LevelConfig.levelData.size.x, _gameManager.LevelConfig.levelData.size.y, _gameManager.LevelConfig.levelData.size.z);

        // var scaleX = _gameManager.LevelConfig.gridSize.x - 10f;
        // var scaleZ = _gameManager.LevelConfig.gridSize.z - 10f;
        // plane.transform.localScale = new Vector3(scaleX, 1, scaleZ);
        // plane.transform.position = new Vector3(0.5f * _gameManager.LevelConfig.gridSize.x - 0.5f, 0, 0.5f * _gameManager.LevelConfig.gridSize.z - 0.5f);
        // var obj = Instantiate(_gameManager.LevelConfig.planePrefab, Vector3.zero, Quaternion.identity, plane.transform);
        // obj.transform.localPosition = Vector3.zero;

        
        var scaleX = _gameManager.LevelConfig.levelData.size.x / 10f;
        var scaleZ = _gameManager.LevelConfig.levelData.size.z / 10f;
        plane.transform.localScale = new Vector3(scaleX, 1, scaleZ);
        plane.transform.position = new Vector3(0.5f * _gameManager.LevelConfig.levelData.size.x - 0.5f, 0, 0.5f * _gameManager.LevelConfig.levelData.size.z - 0.5f);
        GPUInstanceEnabler gPUInstanceEnabler = plane.transform.GetComponent<GPUInstanceEnabler>();
        if (gPUInstanceEnabler)
        {
            gPUInstanceEnabler.SetColor(_gameManager.LevelConfig.colorGround);
        }

        // // Random value for noise.
        // var xOffSet = Random.Range(-10000f, 10000f);
        // var zOffSet = Random.Range(-10000f, 10000f);

        // for (int x = 0; x < _gameManager.LevelConfig.gridSize.x; x++)
        // {
        //     for (int y = 0; y < _gameManager.LevelConfig.gridSize.y; y++)
        //     {
        //         // TileBase tile = map.GetTile(new Vector3Int(x, y));
        //         var position = new Vector3Int(x, y);

        //         GridTileNode node = gridTileHelper.GetNode(new Vector3Int(x, y));

        //         float noiseValue = Mathf.PerlinNoise(
        //             x * _gameManager.LevelConfig.noiseScaleKoof + xOffSet,
        //             y * _gameManager.LevelConfig.noiseScaleKoof + zOffSet
        //         );

        //         bool isFirstLand = noiseValue < _gameManager.LevelConfig.noiseMaxKoof;
        //         if (isFirstLand)
        //         {
        //             map.SetTile(position, _gameManager.LevelConfig.tileLandscape);
        //         }
        //         else
        //         {
        //             map.SetTile(position, _gameManager.LevelConfig.tileSecondLandscape);

        //         }

        //         float noiseForBorder = Mathf.PerlinNoise(
        //             x * _gameManager.LevelConfig.noiseScaleObstacleKoof + xOffSet,
        //             y * _gameManager.LevelConfig.noiseScaleObstacleKoof + zOffSet
        //         );

        //         bool isBorderCenter = noiseForBorder > _gameManager.LevelConfig.noiseObstacleMaxKoof;

        //         bool isBorder = x == 0 || y == 0 || x == _gameManager.LevelConfig.gridSize.x - 1 || y == _gameManager.LevelConfig.gridSize.y - 1;
        //         if (isBorder || isBorderCenter)
        //         {
        //             mapBorder.SetTile(position, _gameManager.LevelConfig.tileBorder);
        //             node.SetDisableNode();
        //             // mapBorder.SetColor(position, Color.black);
        //         }


        //         bool isObstacle = Random.Range(0f, 1f) < 0.01f;
        //         if (isObstacle && (x > 2 || y > 2 || x < _gameManager.LevelConfig.gridSize.x - 2 || y < _gameManager.LevelConfig.gridSize.y - 2))
        //         {
        //             mapBorder.SetTile(position, _gameManager.LevelConfig.tileObstcles[Random.Range(0, _gameManager.LevelConfig.tileObstcles.Count - 1)]);
        //             node.SetDisableNode();
        //             // mapBorder.SetColor(position, Color.black);
        //         }
        //     }
        // }

        // OnCreateNoise();
        // if (!_gameManager.Settings.DebugSettings.disableCreateTiles)
        // {
        //     OnCreateTiles();
        //     OnCreateFixedTiles();
        // }
    }

    // public void OnCreateTiles()
    // {
    //     OnSetNotify?.Invoke("createTiles");

    //     var tilesHeights = ParserHeight.GenerateHeightMap();
        
    //         for (int row = 0; row < _gameManager.LevelConfig.levelData.size.x; row++)
    //         {
    //             for (int col = 0; col < _gameManager.LevelConfig.levelData.size.z; col++)
    //             {

    //                 int _height = tilesHeights[new Vector2Int(row, col)];

    //                 if (_height > 0)
    //                 {
    //                     for (int depth = 0; depth < _height; depth++)
    //                     {
            
    //                     GridTileNode node = gridTileHelper.GetNode(row, depth, col);

    //                     node.StateNode = StateNode.Tiled;
    //                     node.isTop = depth == _height - 1;

    //                     // map.SetTileFlags(node.position, TileFlags.None);
    //                     // map.SetTile(node.position, _gameManager.LevelConfig.tileRuleCave);
    //                     // map.SetTileFlags(node.position, TileFlags.LockAll);
    //                     }

                        
    //                     // for (int depth = 0; depth < _height; depth++)
    //                     // {
    //                     //     GridTileNode node = gridTileHelper.GetNode(row, depth, col);

    //                     //     var allNeighbours = gridTileHelper.GetNeighbourListWithTiled(node, true);

    //                     //     if (allNeighbours.Count >= 8 ||
    //                     //         row == 0 ||
    //                     //         row == _gameManager.LevelConfig.gridSize.x - 1 ||
    //                     //         col == 0 ||
    //                     //         col == _gameManager.LevelConfig.gridSize.z - 1
    //                     //     )
    //                     //     {
    //                     //         if (depth < _height - 1)
    //                     //         {
    //                     //             node.StateNode = StateNode.TiledInner;
    //                     //         } else
    //                     //         {
    //                     //             node.StateNode = StateNode.TiledInnerTop;
    //                     //         }
    //                     //     } else
    //                     //     {
    //                     //         if (depth < _height - 1)
    //                     //         {
    //                     //             node.StateNode = StateNode.Tiled;
    //                     //         } else
    //                     //         {
    //                     //             node.StateNode = StateNode.TiledTop;
    //                     //         }
    //                     //     }
    //                     // }

                    
    //                 // int redColor = Mathf.RoundToInt(tilesHeights[new Vector2Int(row, col)].r * _gameManager.LevelConfig.tileSettings.heightSize);

    //                 // if (redColor > 0)
    //                 // {
    //                 //     node.StateNode = StateNode.Tiled;
    //                 //     map.SetTileFlags(node.position, TileFlags.None);
    //                 //     map.SetTile(node.position, _gameManager.LevelConfig.tileRuleCave);
    //                 //     map.SetTileFlags(node.position, TileFlags.LockAll);
    //                 // }

    //                 // int greenColor = Mathf.RoundToInt(tilesHeights[new Vector2Int(row, col)].g);
    //                 // if (redColor == 0 && greenColor > 0)
    //                 // {
    //                 //     node.StateNode = StateNode.Tree;
    //                 //     var treePrefab = _gameManager.LevelConfig.TreePrefabs[UnityEngine.Random.Range(0, _gameManager.LevelConfig.TreePrefabs.Count)];
    //                 //     var obj = Instantiate(treePrefab, transform.position, Quaternion.identity, transform);
    //                 //     obj.transform.localPosition = node.positionXZ();
    //                 // }

    //                 // int blueColor = Mathf.RoundToInt(tilesHeights[new Vector2Int(row, col)].b);
    //                 // if (redColor == 0 && greenColor == 0 && blueColor > 0)
    //                 // {
    //                 //     node.StateNode = StateNode.Tree;
    //                 //     var housePrefab = _gameManager.LevelConfig.HousePrefabs[UnityEngine.Random.Range(0, _gameManager.LevelConfig.HousePrefabs.Count)];
    //                 //     var obj = Instantiate(housePrefab, transform.position, Quaternion.identity, transform);
    //                 //     obj.transform.localPosition = node.positionXZ();
    //                 // }
    //                 // Debug.Log($"greenColor={greenColor}[{blueColor}]<{redColor}>");
    //             }
    //         }
    //     }
    // }

    // public void OnCreateNoise()
    // {
    //     // Random value for noise.
    //     var xOffSet = UnityEngine.Random.Range(-10000f, 10000f);
    //     var zOffSet = UnityEngine.Random.Range(-10000f, 10000f);
        
    //     for (int x = 0; x < _gameManager.LevelConfig.gridSize.x; x++)
    //     {
    //         for (int z = 0; z < _gameManager.LevelConfig.gridSize.z; z++)
    //         {
    //             GridTileNode node = gridTileHelper.GetNode(new Vector3Int(x, z));

    //             float noiseValue = Mathf.PerlinNoise(
    //                 x * _gameManager.LevelConfig.noiseScaleKoof + xOffSet,
    //                 z * _gameManager.LevelConfig.noiseScaleKoof + zOffSet
    //             );

    //             bool isNeedCreate = noiseValue < _gameManager.LevelConfig.noiseMaxKoof;
    //             if (isNeedCreate)
    //             {
    //                 node.StateNode = StateNode.Tiled;
    //                 map.SetTileFlags(node.position, TileFlags.None);
    //                 map.SetTile(node.position, _gameManager.LevelConfig.tileRuleCave);
    //                 map.SetTileFlags(node.position, TileFlags.LockAll);
    //             }
    //         }
    //     }
    // }

    // public void OnCreateFixedTiles()
    // {
    //     OnSetNotify?.Invoke("createFixedTiles");
    //     for (int depth = 0; depth < _gameManager.LevelConfig.gridSize.y; depth++)
    //     {
    //         for (int row = 0; row < _gameManager.LevelConfig.gridSize.x; row++)
    //         {
    //             for (int col = 0; col < _gameManager.LevelConfig.gridSize.z; col++)
    //             {
    //                 GridTileNode node = gridTileHelper.GetNode(row, depth, col);
                    
    //                 if (node.StateNode.HasFlag(StateNode.Tiled)) {
    //                     var allNeighbours = gridTileHelper.GetNeighbourListWithTiled(node, true);
    //                     // Debug.Log($"allNeighbours {node.position}/{node.positionXZ()} {node.StateNode}: {allNeighbours.Count}");

    //                     if (allNeighbours.Count >= 8 ||
    //                         row == 0 ||
    //                         row == _gameManager.LevelConfig.gridSize.x - 1 ||
    //                         col == 0 ||
    //                         col == _gameManager.LevelConfig.gridSize.z - 1
    //                     )
    //                     {
    //                         node.StateNode = StateNode.TiledInner;
    //                         // map.SetTileFlags(node.position, TileFlags.None);
    //                         // map.SetTile(node.position, _gameManager.LevelConfig.tileLandscape);
    //                         // map.SetTileFlags(node.position, TileFlags.LockAll);
    //                     }

    //                 }
    //             }
    //         }
    //     }
    // }
    // public void OnCreateTestObjects()
    // {
    //     // Random value for noise.
    //     var xOffSet = UnityEngine.Random.Range(-10000f, 10000f);
    //     var zOffSet = UnityEngine.Random.Range(-10000f, 10000f);
        
    //     for (int x = 0; x < _gameManager.LevelConfig.gridSize.x; x++)
    //     {
    //         for (int z = 0; z < _gameManager.LevelConfig.gridSize.z; z++)
    //         {
    //             var position = new Vector3Int(x, 0, z);

    //             GridTileNode node = gridTileHelper.GetNode(new Vector3Int(x, z));

    //             float noiseValue = Mathf.PerlinNoise(
    //                 x * _gameManager.LevelConfig.noiseScaleKoof + xOffSet,
    //                 z * _gameManager.LevelConfig.noiseScaleKoof + zOffSet
    //             );

    //             bool isNeedCreate = noiseValue < _gameManager.LevelConfig.noiseMaxKoof;
    //             if (isNeedCreate)
    //             {
    //                 // Instantiate(_gameManager.LevelConfig.testObjects[UnityEngine.Random.Range(0, _gameManager.LevelConfig.testObjects.Length)], position, Quaternion.identity, _levelManager.objectSpawnEffect.transform);
    //                 map.SetTileFlags(node.position, TileFlags.None);
    //                 map.SetTile(node.position, _gameManager.LevelConfig.tileRuleCave);
    //                 map.SetTileFlags(node.position, TileFlags.LockAll);
    //                 // Debug.Log($"Draw tile to position {position}-{node.position}");
    //             }
    //         }
    //     }
    // }

    // public void OnSetColor(GridTileNode node, Color color)
    // {
    //     Vector3Int posTile = map.WorldToCell(node.position);

    //     map.SetColor(posTile, color);
    // }

    
    public Vector3 GetRandomNavmeshLocation(float sampleRadius)
    {
        // 1. Generate a random origin point near the NavMeshSurface's center
        Vector3 randomPoint = new Vector3(
            UnityEngine.Random.Range(2f, _gameManager.LevelConfig.levelData.size.x - 2),
            1,
            UnityEngine.Random.Range(2f, _gameManager.LevelConfig.levelData.size.x - 2)
        ); //UnityEngine.Random.insideUnitSphere * sampleRadius;
        // randomDirection.y = 1f;
        // Vector3 origin = transform.position + randomDirection; // Use the spawner's position or a known center

        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        // 2. Sample the position on the NavMesh
        if (NavMesh.SamplePosition(randomPoint, out hit, sampleRadius, NavMesh.AllAreas))
        {
            // 3. Return the valid position
            finalPosition = hit.position;
        }
        // finalPosition.y = 0.01f;

        // Debug.LogWarning($"GetRandomNavmeshLocation: randomPoint={randomPoint}, finalPosition={finalPosition}");
        return finalPosition;
    }
}
