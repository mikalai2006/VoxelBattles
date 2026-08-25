#if UNITY_EDITOR
using System.Collections.Generic;
using Mikalai2006.VoxelBase;
using UnityEditor;

namespace Mikalai2006.VoxelMap {
    public class WFCCreatorZabor : WFCCreator
    {
        public override void SetConfig(ref ParserHeight parserHeight)
        {
            parserHeight._settings = wFCBuilder.wFCManager.settingMapZabor;

            base.SetConfig(ref parserHeight);
        }

        public override void InitializeGrid(Tile3D[] prefabTiles, ParserHeight _parserHeight, TypeEntity typeCell)
        {
            // _parserHeight._settings = wFCBuilder.wFCManager.settingMapZabor;

            base.InitializeGrid(prefabTiles, _parserHeight, typeCell);
        }

        public override List<DataGroupTiles> OnSaveTiled()
        {
            List<DataGroupTiles> levelDataGroups = base.OnSaveTiled();

            // сохраняем в файл уровня.
            // записываем сохраненные данные для тайлов.
            var levelData = wFCBuilder.wFCManager.gameLevel.levelData;
            
            levelData.zabor = levelDataGroups;

            wFCBuilder.wFCManager.gameLevel.levelData = levelData;
            
            EditorUtility.SetDirty(wFCBuilder.wFCManager.gameLevel);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return levelDataGroups;
        }

        public override void Load()
        {
            LoadTiles(wFCBuilder.wFCManager.gameLevel.levelData.zabor);
        }
    }
}
#endif