
#if UNITY_EDITOR
using Mikalai2006.VoxelBase;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mikalai2006.VoxelMap {
    public class WFCManager : MonoBehaviour
    {
        [SerializeField] private VoxelTextureGenerator voxelTextureGenerator;

        [Header("Общее")]
        public Tile3D emptyTilePrefab;
        public Tile3DDebugCell debugTilePrefab;
        public GameObject prefabCellPlaceholder;
        public Vector3 scaleTiles;
        public Button buttonSave;
        public Button buttonSaveAll;
        public Button buttonLoad;

        [SerializeField] public GameLevel gameLevel;
        // public List<Cell3DData> saveZabor;
        // public List<Cell3DData> saveCaves;
        // public List<Cell3DData> saveHouses;

        [Header("Скалы")]
        public TextureHeightMapperSettings settingMapCaves;
        [SerializeField] private WFCBuilder wFCBuilderCaves;
        [Header("Постройки")]
        public TextureHeightMapperSettings settingMapHouses;
        [SerializeField] private WFCBuilder wFCBuilderHouses;
        [Header("Забор")]
        public TextureHeightMapperSettings settingMapZabor;
        [SerializeField] private WFCBuilder wFCBuilderZabor;
        [Header("Деревья")]
        public TextureHeightMapperSettings settingMapTrees;
        [SerializeField] private WFCBuilder wFCBuilderTrees;


        void Awake()
        {
            buttonSave.onClick.AddListener(() =>
            {
                OnSaveSettings();
            });
            buttonSaveAll.onClick.AddListener(() =>
            {
                OnSaveAll();
            });
            buttonLoad.onClick.AddListener(() =>
            {
                Load();
            });

        }

        /// <summary>
        /// Функция подготавливает и пишет в файл ScriptableObject данные для создания уровня.
        /// </summary>
        void OnSaveAll()
        {
            wFCBuilderZabor.wFCCreator.OnSaveTiled();
            wFCBuilderCaves.wFCCreator.OnSaveTiled();
            wFCBuilderHouses.wFCCreator.OnSaveTiled();
            wFCBuilderTrees.wFCCreator.OnSaveTiled();
            
            OnSaveSettings();
        }

        /// <summary>
        /// Функция подготавливает и пишет в файл ScriptableObject префабы и размеры карты.
        /// </summary>
        private void OnSaveSettings()
        {
            // определяем максимальную высоту.
            int maxHeight = Mathf.Max(wFCBuilderCaves.size.y, wFCBuilderHouses.size.y, wFCBuilderZabor.size.y, wFCBuilderTrees.size.y);

            // записываем сохраненные данные для тайлов.
            gameLevel.levelData.maxHeight = maxHeight;
            gameLevel.levelData.size = wFCBuilderCaves.size;

            // записываем все использованные префабы.
            var tilePrefabs = new System.Collections.Generic.List<Tile3D>();
            foreach (var item in wFCBuilderZabor.tilePrefabs)
            {
                tilePrefabs.Add(item);
            }
            foreach (var item in wFCBuilderCaves.tilePrefabs)
            {
                tilePrefabs.Add(item);
            }
            foreach (var item in wFCBuilderHouses.tilePrefabs)
            {
                tilePrefabs.Add(item);
            }
            foreach (var item in wFCBuilderTrees.tilePrefabs)
            {
                tilePrefabs.Add(item);
            }
            gameLevel.TilePrefabs = tilePrefabs;
            
            EditorUtility.SetDirty(gameLevel);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Load()
        {
            wFCBuilderZabor.wFCCreator.Load();
            wFCBuilderCaves.wFCCreator.Load();
            wFCBuilderHouses.wFCCreator.Load();
            wFCBuilderTrees.wFCCreator.Load();
        }
    }

}
#endif