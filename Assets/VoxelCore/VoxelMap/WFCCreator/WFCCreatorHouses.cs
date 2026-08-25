#if UNITY_EDITOR
using System.Collections.Generic;
using Mikalai2006.VoxelBase;
using UnityEditor;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public class WFCCreatorHouses : WFCCreator
    {
        int nextNumberGroup;

        public override void Awake()
        {
            base.Awake();

            nextNumberGroup = 0;
        }

        public override void SetConfig(ref ParserHeight parserHeight)
        {
            parserHeight._settings = wFCBuilder.wFCManager.settingMapHouses;

            base.SetConfig(ref parserHeight);
        }
        
        public override void InitializeGrid(Tile3D[] prefabTiles, ParserHeight _parserHeight, TypeEntity typeCell)
        {
            // _parserHeight._settings = wFCBuilder.wFCManager.settingMapHouses;

            base.InitializeGrid(prefabTiles, _parserHeight, typeCell);

            AnalyseGroup();
        }


        public void AnalyseGroup()
        {
            
            for (int x = 0; x < wFCBuilder.size.x; x++)
            {
                for (int z = 0; z < wFCBuilder.size.z; z++)
                {
                    int _height = parserHeight.heightMap[new Vector2Int(x, z)];
                    
                    for (int y = 0; y < _height; y++)
                    {
                        var position = new Vector3Int(x, y, z);

                        WFCCell wFCCell = gridComponents.Find(x => x.position.Equals(position));

                        if (EqualityComparer<WFCCell>.Default.Equals(wFCCell, default(WFCCell)) || wFCCell.disabled)
                        {
                            continue;
                        }

                        CheckGroup(wFCCell);
                    }
                }
            }
        }

        /// <summary>
        /// Определяет группу ячейки.
        /// </summary>
        /// <param name="cell"></param>
        void CheckGroup(WFCCell cell)
        {
            List<WFCCell> cellsNeighbours = GetNeighboursByPosition(cell);
            
            int groupNumber = GetNumberGroup(cellsNeighbours);

            cell.SetGroup(groupNumber);
        }

        /// <summary>
        /// Определяет номер группы, к которой принадлежит ячейка.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        int GetNumberGroup(List<WFCCell> input)
        {
            int numberGroup = 0;

            foreach (var cell in input)
            {
                if (cell.groupNumber > 0)
                {
                    numberGroup = cell.groupNumber;
                    // return numberGroup;
                    break;
                }
            }

            if (numberGroup == 0)
            {
                nextNumberGroup = nextNumberGroup + 1;

                numberGroup = nextNumberGroup;
            }

            return numberGroup;
        }

        /// <summary>
        /// Находит 6 соседей для ячейки.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        List<WFCCell> GetNeighboursByPosition(WFCCell cell)
        {
            List<WFCCell> output = new List<WFCCell>();

            List<Vector3Int> posNeighbours = new List<Vector3Int>()
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
            };

            for (int i = 0; i < 6; i++)
            {
                // Формируем позицию соседа.
                Vector3Int positionNeighbour =  Vector3Int.FloorToInt(cell.position + posNeighbours[i]);
                    
                WFCCell wFCCell = gridComponents.Find(x => x.position.Equals(positionNeighbour));

                if (EqualityComparer<WFCCell>.Default.Equals(wFCCell, default(WFCCell)) || wFCCell.disabled)
                {
                    continue;
                }

                output.Add(wFCCell);
            }

            return output;
        }

        public override List<DataGroupTiles> OnSaveTiled()
        {
            List<DataGroupTiles> levelDataGroups = base.OnSaveTiled();

            // сохраняем в файл уровня.
            // записываем сохраненные данные для тайлов.
            var levelData = wFCBuilder.wFCManager.gameLevel.levelData;
            
            levelData.houses = levelDataGroups;

            wFCBuilder.wFCManager.gameLevel.levelData = levelData;
            
            EditorUtility.SetDirty(wFCBuilder.wFCManager.gameLevel);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return levelDataGroups;
        }

        public override void Load()
        {
            LoadTiles(wFCBuilder.wFCManager.gameLevel.levelData.houses);
        }
    }
}
#endif