#if UNITY_EDITOR
using System.Collections.Generic;
using Mikalai2006.VoxelBase;
using UnityEngine;
using UnityEngine.UI;

namespace Mikalai2006.VoxelMap {
    public class WFCBuilder : MonoBehaviour
    {
        public WFCCreator wFCCreator;
        public WFCManager wFCManager;
        public Vector3Int size;
        public Button buttonCreateGrid;
        public Button buttonAutoGen;
        public Button buttonStepGen;
        public Button buttonSave;
        public Button buttonLoad;
        public List<Tile3D> tilePrefabs;
        public ParserHeight ParserHeight;
        public TypeEntity typeCell;

        public List<WFCToolsCell> toolsCells;

        void Awake()
        {
            buttonCreateGrid.onClick.AddListener(() =>
            {
                CreateTools();
            });
            buttonAutoGen.onClick.AddListener(() =>
            {
                wFCCreator.StartWFC();
            });
            buttonStepGen.onClick.AddListener(() =>
            {
                wFCCreator.StartStepWFC();
            });
            buttonSave.onClick.AddListener(() =>
            {
                wFCCreator.OnSaveTiled();
            });
            buttonLoad.onClick.AddListener(() =>
            {
                wFCCreator.Load();
            });
        }

        void Start()
        {
            Init();
        }

        void Init()
        {
            wFCCreator.SetConfig(ref ParserHeight);

            // генерируем карту высот из картинки.
            ParserHeight.Init();

            // устанавливаем размеры карты.
            size = new Vector3Int(ParserHeight.gridSize.x, ParserHeight._settings.heightSize, ParserHeight.gridSize.y);

            // Запускаем генератор.
            wFCCreator.InitializeGrid(tilePrefabs.ToArray(), ParserHeight, typeCell);
        }

        void CreateTools()
        {
            ResetCellsTools();

            for (int y = 0; y < 1; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        int _height = ParserHeight.heightMap[new Vector2Int(x, z)];

                        if (_height > 0)
                        {
                            Vector3 position = new Vector3(x,y,z);
                            GameObject cellGameObject = Instantiate(wFCManager.prefabCellPlaceholder, position, Quaternion.identity, transform);
                            
                            cellGameObject.transform.localPosition = position;

                            WFCToolsCell cell = cellGameObject.GetComponent<WFCToolsCell>();
                            cell.Init(this);
                            cell.height = _height;

                            toolsCells.Add(cell);
                        } else
                        {
                            
                        }
                    }  
                }  
            }    
        }

        private void ResetCellsTools()
        {
            foreach (var item in toolsCells)
            {
                Destroy(item.gameObject);
            }

            toolsCells.Clear();

            wFCCreator.ResetTiles();
        }

        public void CreateCellTools(Vector3 position)
        {
            // var bottomCellTools = toolsCells.Find(x => x.position.Equals(new Vector3(position.x, 0, position.z)));
            // if (!bottomCellTools)
            // {
            //     Debug.LogWarning($"Попытка создать тайл в недопустимом месте!");
            //     return;
            // }

            // if (position.y >= bottomCellTools.height)
            // {
            //     Debug.LogWarning($"Попытка создать тайл на недопустимой высоте!");
            //     return;
            // }

            if (
                position.x < 0 ||
                position.x >= size.x ||
                position.y < 0 ||
                position.y >= size.y ||
                position.z < 0 ||
                position.z >= size.z
            )
            {
                Debug.LogWarning($"Попытка создать тайл за пределами заданной сетки!");
                return;
            }

            GameObject cellGameObject = Instantiate(wFCManager.prefabCellPlaceholder, position, Quaternion.identity, transform);
            cellGameObject.transform.localPosition = position;

            WFCToolsCell cell = cellGameObject.GetComponent<WFCToolsCell>();
            cell.Init(this);
            toolsCells.Add(cell);
            
            // вставка префаба.
            wFCCreator.CreateTile(position);
            // Tile3D tile3D = Instantiate(tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)], position, Quaternion.identity, transform);
            // tile3D.transform.localPosition = position - new Vector3(0, 0.5f, 0);
        }

        public void RemoveCellTools(WFCToolsCell cellTools)
        {
            WFCToolsCell cellForRemove = toolsCells.Find(x => x == cellTools);

            if (cellForRemove)
            {
                Destroy(cellForRemove.gameObject);
                toolsCells.Remove(cellForRemove);

                wFCCreator.RemoveTile(cellTools.position);
            }
        }
    }
}
#endif