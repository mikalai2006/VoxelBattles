#if UNITY_EDITOR
using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public class WFCCreatorTiles : MonoBehaviour
    {
        public List<SOVoxelData> tilesConfigs;
        public GameObject wrapper;
        public int nextName;
        public string nameDirPrefabs = "Test";
        public string layerName;
        public string tagName;
        public Dictionary<string, Voxel[]> dictionarySocketsNames;
        [Tooltip("Очищать ли массивы цветов границ в конфигах (больше не будет возможности использовать этот функционал)")]
        public bool isClearColors;
        [Tooltip("Добавлять ли массивы цветов границ в каждый тайл (будет много весить)")]
        public bool isAddColorsToTile;
        [Tooltip("Очищать ли файлы конфигураций")]
        public bool isClearTilesConfigs;

        [Header("Разрушаемые ли тайлы")]
        [Tooltip("Если установить эту галочку, то будет использоваться только значение из этих настроек, игнорируя из имени файла")]
        public bool forceIsDestructible;
        public bool isDestructible;

        [Header("Фантом тайла")]
        [Tooltip("Если установить эту галочку, то модель создастся как тайл, игнорируя отсутствие входных данных в имени файла")]
        public bool isFantomTile;

        [Space(5)]
        [Header("Опции коллайдера и меша")]
        public bool isCollider;
        public TypeCollider typeCollider;
        public Material material;
        public Material materialAlpha;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        [Tooltip("Физический материал")]
        public PhysicsMaterial physicsMaterial;
        public bool isConvex;

        [Space(5)]
        [Header("Опции твердого тела")]
        public bool isRigidbody;
        public bool isKinematic;
        public bool isGravity;
        public int mass;
        // public bool freezeRotation;
        public bool FreezePositionX;
        public bool FreezePositionY;
        public bool FreezePositionZ;
        public bool FreezeRotationX;
        public bool FreezeRotationY;
        public bool FreezeRotationZ;
        // public RigidbodyConstraints constraints;
        public RigidbodyInterpolation rigidbodyInterpolation;
        public CollisionDetectionMode collisionDetection;

        public void AnalyseSockets()
        {
            nextName = 0;

            dictionarySocketsNames = new Dictionary<string, Voxel[]>();

            foreach (SOVoxelData configTile in tilesConfigs)
            {
                // создаем розетки (идентификаторы для разрешения стыкования граней тайлов).
                CheckSocket(configTile.ColorsRight, ref configTile.tileSockets.posX);
                CheckSocket(configTile.ColorsLeft, ref configTile.tileSockets.negX);
                CheckSocket(configTile.ColorsForward, ref configTile.tileSockets.negZ);
                CheckSocket(configTile.ColorsBack, ref configTile.tileSockets.posZ);
                CheckSocket(configTile.ColorsTop, ref configTile.tileSockets.posY);
                CheckSocket(configTile.ColorsBottom, ref configTile.tileSockets.negY);
            
                var colorsDefault = new ColorsBorders
                {
                    ColorsRight = configTile.ColorsRight,
                    ColorsLeft = configTile.ColorsLeft,
                    ColorsForward = configTile.ColorsForward,
                    ColorsBack = configTile.ColorsBack,
                    ColorsTop = configTile.ColorsTop,
                    ColorsBottom = configTile.ColorsBottom,
                };

                Tile3D pref = CreatePrefab(configTile, 0, configTile.tileSockets, colorsDefault);

                if (configTile.Rotation == RotationType.FourRotations)
                {
                    // 90 градусов.
                    ColorsBorders colors90 = Rotate90Colors(colorsDefault, configTile);
                    TileSockets tileSockets90 = new TileSockets
                    {
                        rotation = 90,
                    };
                    CheckSocket(colors90.ColorsRight, ref tileSockets90.posX);
                    CheckSocket(colors90.ColorsLeft, ref tileSockets90.negX);
                    CheckSocket(colors90.ColorsForward, ref tileSockets90.negZ);
                    CheckSocket(colors90.ColorsBack, ref tileSockets90.posZ);
                    CheckSocket(colors90.ColorsTop, ref tileSockets90.posY);
                    CheckSocket(colors90.ColorsBottom, ref tileSockets90.negY);
                    // tileSockets90.posY = GetSocketByPrefixRotate(configTile.tileSockets.posY, 90);
                    // tileSockets90.negY = GetSocketByPrefixRotate(configTile.tileSockets.negY, 90);
                    Tile3D pref90 = CreatePrefab(configTile, 90, tileSockets90, colors90);

                    // 180 градусов.
                    ColorsBorders colors180 = Rotate90Colors(colors90, configTile);
                    TileSockets tileSockets180 = new TileSockets
                    {
                        rotation = 180,
                    };
                    CheckSocket(colors180.ColorsRight, ref tileSockets180.posX);
                    CheckSocket(colors180.ColorsLeft, ref tileSockets180.negX);
                    CheckSocket(colors180.ColorsForward, ref tileSockets180.negZ);
                    CheckSocket(colors180.ColorsBack, ref tileSockets180.posZ);
                    CheckSocket(colors180.ColorsTop, ref tileSockets180.posY);
                    CheckSocket(colors180.ColorsBottom, ref tileSockets180.negY);
                    // tileSockets180.posY = GetSocketByPrefixRotate(configTile.tileSockets.posY, 180);
                    // tileSockets180.negY = GetSocketByPrefixRotate(configTile.tileSockets.negY, 180);
                    Tile3D pref180 = CreatePrefab(configTile, 180, tileSockets180, colors180);

                    // 270 градусов.
                    ColorsBorders colors270 = Rotate90Colors(colors180, configTile);
                    TileSockets tileSockets270 = new TileSockets
                    {
                        rotation = 270
                    };
                    CheckSocket(colors270.ColorsRight, ref tileSockets270.posX);
                    CheckSocket(colors270.ColorsLeft, ref tileSockets270.negX);
                    CheckSocket(colors270.ColorsForward, ref tileSockets270.negZ);
                    CheckSocket(colors270.ColorsBack, ref tileSockets270.posZ);
                    CheckSocket(colors270.ColorsTop, ref tileSockets270.posY);
                    CheckSocket(colors270.ColorsBottom, ref tileSockets270.negY);
                    // tileSockets270.posY = GetSocketByPrefixRotate(configTile.tileSockets.posY, 270);
                    // tileSockets270.negY = GetSocketByPrefixRotate(configTile.tileSockets.negY, 270);
                    Tile3D pref270 = CreatePrefab(configTile, 270, tileSockets270, colors270);
                }
            }

            // если нужно очищаем массивы цветов границ.
            if (isClearColors)
            {
                for (int i = 0; i < tilesConfigs.Count; i++)
                {
                    var cacheConfigTile = tilesConfigs[i];
                    cacheConfigTile.ColorsRight = new Voxel[0];
                    cacheConfigTile.ColorsLeft = new Voxel[0];
                    cacheConfigTile.ColorsForward = new Voxel[0];
                    cacheConfigTile.ColorsBack = new Voxel[0];
                    cacheConfigTile.ColorsTop = new Voxel[0];
                    cacheConfigTile.ColorsBottom = new Voxel[0];
                    tilesConfigs[i] = cacheConfigTile;
                    EditorUtility.SetDirty(tilesConfigs[i]);
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (isClearTilesConfigs)
            {
                tilesConfigs.Clear();
            }
        }

        /// <summary>
        /// поворот массивов цветов на 90 по часовой стрелке.
        /// </summary>
        /// <returns>ColorsBorders</returns>
        public ColorsBorders Rotate90Colors(ColorsBorders inputColors, SOVoxelData configTile)
        {
            var TileSideVoxels = configTile.Bounds.x;

            Voxel[] colorsRightNew = new Voxel[TileSideVoxels * TileSideVoxels];
            Voxel[] colorsForwardNew = new Voxel[TileSideVoxels * TileSideVoxels];
            Voxel[] colorsLeftNew = new Voxel[TileSideVoxels * TileSideVoxels];
            Voxel[] colorsBackNew = new Voxel[TileSideVoxels * TileSideVoxels];
            // Voxel[] colorsTopNew = new Voxel[TileSideVoxels * TileSideVoxels];
            // Voxel[] colorsBottomNew = new Voxel[TileSideVoxels * TileSideVoxels];

            for (int row = 0; row < TileSideVoxels; row++)
            {
                for (int column = 0; column < TileSideVoxels; column++)
                {
                    // colorsRightNew[row * TileSideVoxels + column] = inputColors.ColorsForward[row * TileSideVoxels + TileSideVoxels - column - 1];
                    // colorsForwardNew[row * TileSideVoxels + column] = inputColors.ColorsLeft[row * TileSideVoxels + column];
                    // colorsLeftNew[row * TileSideVoxels + column] = inputColors.ColorsBack[row * TileSideVoxels + TileSideVoxels - column - 1];
                    // colorsBackNew[row * TileSideVoxels + column] = inputColors.ColorsRight[row * TileSideVoxels + column];
                    // // TODO
                    colorsForwardNew[row * TileSideVoxels + column] = inputColors.ColorsRight[row * TileSideVoxels + column];
                    colorsRightNew[row * TileSideVoxels + column] = inputColors.ColorsBack[row * TileSideVoxels + TileSideVoxels - column - 1];
                    colorsBackNew[row * TileSideVoxels + column] = inputColors.ColorsLeft[row * TileSideVoxels + column];
                    colorsLeftNew[row * TileSideVoxels + column] = inputColors.ColorsForward[row * TileSideVoxels + TileSideVoxels - column - 1];
                    
                    // colorsForwardNew[row * TileSideVoxels + column] = inputColors.ColorsRight[row * TileSideVoxels + column];
                    // colorsRightNew[row * TileSideVoxels + column] = inputColors.ColorsBack[row * TileSideVoxels + column];
                    // colorsBackNew[row * TileSideVoxels + column] = inputColors.ColorsLeft[row * TileSideVoxels + column];
                    // colorsLeftNew[row * TileSideVoxels + column] = inputColors.ColorsForward[row * TileSideVoxels + column];
                }
            }
            Voxel[] colorsTopNew = Rotate90TopBottom(inputColors.ColorsTop, configTile);
            Voxel[] colorsBottomNew = Rotate90TopBottom(inputColors.ColorsBottom, configTile);


            return new ColorsBorders
            {
                ColorsRight = colorsRightNew,
                ColorsLeft = colorsLeftNew,
                ColorsForward = colorsForwardNew,
                ColorsBack = colorsBackNew,
                ColorsTop = colorsTopNew,
                ColorsBottom = colorsBottomNew
            };
        }

        public Voxel[] Rotate90TopBottom(Voxel[] arrayVoxels, SOVoxelData configTile)
        {
            var TileSideVoxels = configTile.Bounds.x;
        // приводим массив одномерный к двумерному.
            Voxel[,] temp = new Voxel[TileSideVoxels, TileSideVoxels];
            for (int i = 0; i < arrayVoxels.Length; i++)
            {
                Vector2Int el = WFCHelpers.From1DTo2D(i, TileSideVoxels);
                temp[el[0], el[1]] = arrayVoxels[i];
            }
            
            // reverse.
            Voxel[,] tempReverseArray = new Voxel[temp.GetLength(0), temp.GetLength(1)];
            for (int i = 0; i < temp.GetLength(0); i++)
            {
                for (int j = 0; j < temp.GetLength(1); j++)
                {
                    tempReverseArray[i, j] = temp[temp.GetLength(0) - 1 - i, j];
                }
            }

            // поворот на 90 град.
            Voxel[,] transposedArray = new Voxel[TileSideVoxels, TileSideVoxels];
            Voxel[] output = new Voxel[TileSideVoxels * TileSideVoxels];
            for (int i = 0; i < TileSideVoxels; i++)
            {
                for (int j = 0; j < TileSideVoxels; j++)
                {
                    // Меняем местами индексы при копировании
                    transposedArray[j, i] = tempReverseArray[i, j];
                    output[i * TileSideVoxels + j] = tempReverseArray[j, i];
                    // Console.WriteLine("ПРОФЕСИОНАЛНА ГИМНАЗИЯ {0}/{1}", transposedArray[j, i], temp[i, j]);
                }
            }

            return output;
        }

        string GetSocketByPrefixRotate(string nameSocket, int angle)
        {
            string[] posYSlice = nameSocket.Split("_");
            string output = posYSlice[0] != "-1" ? $"{posYSlice[0]}_{angle}" : "-1";
            return output;
        }

        // /// <summary>
        // /// поворот розеток на 90 градусов.
        // /// </summary>
        // /// <param name="tileSockets"></param>
        // /// <returns></returns>
        // TileSockets Rotate90Sockets(TileSockets tileSockets)
        // {
        //     string newNegZ = tileSockets.posX;
        //     string newPosX = tileSockets.posZ;
        //     string newPosZ = tileSockets.negX;
        //     string newNegX = tileSockets.negZ;
            

            
        //     int newAngle = tileSockets.rotation + 90;
        //     string[] posYSlice = tileSockets.posY.Split("_");
        //     string newPosY = posYSlice[0] != "-1" ? $"{posYSlice[0]}_{newAngle}" : "-1";
        //     string[] negYSlice = tileSockets.negY.Split("_");
        //     string newNegY = negYSlice[0] != "-1" ? $"{negYSlice[0]}_{newAngle}" : "-1";



        //     return new TileSockets()
        //     {
        //         name = tileSockets.name,
        //         posX = newPosX,
        //         negX = newNegX,
        //         posZ = newPosZ,
        //         negZ = newNegZ,
        //         posY = newPosY,
        //         negY = newNegY,
        //         rotation = newAngle,
        //         weight = tileSockets.weight
        //     };
        // }

        Tile3D CreatePrefab(SOVoxelData inputConfig, int angle = 0, TileSockets tileSockets = default, ColorsBorders colorsBorders = default)
        {
            string newObjectName = $"{inputConfig.name}__{angle}";
            string OutputPath = "Assets/1_Prefabs";

            GameObject newObject = new GameObject(newObjectName);
            newObject.transform.parent = wrapper.transform;
            if (!string.IsNullOrEmpty(layerName))
            {
                newObject.layer = LayerMask.NameToLayer(layerName);
            }
            if (!string.IsNullOrEmpty(tagName) && GameObject.FindWithTag(tagName) != null)
            {
                newObject.tag = tagName;
            }
            Tile3D newTile3D = newObject.AddComponent<Tile3D>();
            if (!EqualityComparer<TileSockets>.Default.Equals(tileSockets, default(TileSockets)))
            {
                newTile3D.tileSockets = tileSockets;
            } else
            {
                newTile3D.tileSockets = inputConfig.tileSockets;
            }

            // определяем тайл для земли (0 уровень).
            string[] namePathArray = inputConfig.name.Split("_");
            if (namePathArray.Contains("ground") || isFantomTile)
            {
                newTile3D.isGround = true;
            } else if (namePathArray.Contains("top"))
            {
                newTile3D.isTop = true;
            }

            // определяем количество поворотов для тайла.
            if (!isFantomTile)
            {
                if (namePathArray[namePathArray.Length - 2] == "r4")
                {
                    inputConfig.Rotation = RotationType.FourRotations;
                    newTile3D.Rotation = RotationType.FourRotations;
                } else if (namePathArray[namePathArray.Length - 2] == "r2")
                {
                    inputConfig.Rotation = RotationType.TwoRotations;
                    newTile3D.Rotation = RotationType.TwoRotations;
                }
            }

            // определяем вес для тайла.
            if (!isFantomTile)
            {
                if (namePathArray[namePathArray.Length - 3] != "")
                {
                    string weightString = namePathArray[namePathArray.Length - 3].Replace("w", "");
                    newTile3D.Weight = int.Parse(weightString);
                }
            }

            GameObject wrapperPrefab = new GameObject("Wrapper");
            wrapperPrefab.transform.parent = newObject.transform;
            wrapperPrefab.layer = LayerMask.NameToLayer(layerName);
            wrapperPrefab.tag = tagName;

            VoxelMeshRender newVMR = wrapperPrefab.AddComponent<VoxelMeshRender>();
            newVMR.Wrapper = wrapperPrefab;

            newVMR.Config.sOVoxelData = inputConfig;
            newVMR.Config._material = material;
            newVMR.Config._materialAlpha = materialAlpha;
            newVMR.Config.isGreedy = true;
            newVMR.Config.isOneMesh = true;
            newVMR.Config.isTile = true;
            newVMR.Config.existCollider = isCollider;
            newVMR.Config.typeCollider = typeCollider;// TypeCollider.MeshCollider;
            newVMR.Config.collisionDetection = CollisionDetectionMode.Continuous;
            newVMR.Config.useGlobalScale = true;

            if (forceIsDestructible)
            {
                newVMR.Config.isDestructible = isDestructible;
            }
            else
            {
                newVMR.Config.isDestructible = true;
                // определяем разрушаемый ли тайл.
                newVMR.Config.isDestructible = namePathArray[namePathArray.Length - 1] == "d";
            }

            newVMR.Config.isRigidbody = isRigidbody;
            newVMR.Config.physMaterial = physicsMaterial;
            newVMR.Config.isKinematic = isKinematic;
            newVMR.Config.isGravity = isGravity;
            newVMR.Config.shadowCastingMode = shadowCastingMode;
            newVMR.Config.mass = mass;
            newVMR.Config.rigidbodyInterpolation = rigidbodyInterpolation;
            newVMR.Config.collisionDetection = collisionDetection;
            newVMR.Config.isConvex = isConvex;

            // newVMR.Config.freezeRotation = freezeRotation;
            // RigidbodyConstraints _constraints = RigidbodyConstraints.None;
            // if (FreezePositionX) _constraints |= RigidbodyConstraints.FreezePositionX;
            // if (FreezePositionY) _constraints |= RigidbodyConstraints.FreezePositionY;
            // if (FreezePositionZ) _constraints |= RigidbodyConstraints.FreezePositionZ;
            // if (FreezeRotationX) _constraints |= RigidbodyConstraints.FreezeRotationX;
            // if (FreezeRotationY) _constraints |= RigidbodyConstraints.FreezeRotationY;
            // if (FreezeRotationZ) _constraints |= RigidbodyConstraints.FreezeRotationZ;
            // newVMR.Config.constraints = _constraints;
            newVMR.Config.FreezePositionX = FreezePositionX;
            newVMR.Config.FreezePositionY = FreezePositionY;
            newVMR.Config.FreezePositionZ = FreezePositionZ;
            newVMR.Config.FreezeRotationX = FreezeRotationX;
            newVMR.Config.FreezeRotationY = FreezeRotationY;
            newVMR.Config.FreezeRotationZ = FreezeRotationZ;

            if (!EqualityComparer<ColorsBorders>.Default.Equals(colorsBorders, default(ColorsBorders)) && isAddColorsToTile)
            {
                newTile3D.ColorsBack = colorsBorders.ColorsBack;
                newTile3D.ColorsBottom = colorsBorders.ColorsBottom;
                newTile3D.ColorsForward = colorsBorders.ColorsForward;
                newTile3D.ColorsTop = colorsBorders.ColorsTop;
                newTile3D.ColorsLeft = colorsBorders.ColorsLeft;
                newTile3D.ColorsRight = colorsBorders.ColorsRight;
            }

            NavMeshModifier newNMM = wrapperPrefab.AddComponent<NavMeshModifier>();
            newNMM.overrideArea = true;
            newNMM.area = 1;


            // GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExistingPrefab.prefab");
            // var savePath = "Assets/Prefabs/ExistingPrefab.prefab";
            if (!AssetDatabase.IsValidFolder($"{OutputPath}/WFC/{nameDirPrefabs}"))
            {
                AssetDatabase.CreateFolder($"{OutputPath}/WFC", nameDirPrefabs);
            }

            string savePath = AssetDatabase.GenerateUniqueAssetPath($"{OutputPath}/WFC/{nameDirPrefabs}/{newObjectName}.prefab");

            GameObject prefabAsset1 = PrefabUtility.SaveAsPrefabAsset(newObject, savePath);
            Tile3D prefTile3D = prefabAsset1.GetComponent<Tile3D>();
            // GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            // instance.transform.position = Vector3.zero;

            return prefTile3D;
        }   

        void CheckSocket(Voxel[] listColors, ref string configOption)
        {
            // если это фантом тайла (не реальный тайл. Пример: дерево)
            if (isFantomTile) {
                configOption = "-1";
            }
            else if (HelperVoxel.AreExistColors(listColors))
            {
                // Debug.Log($"name={vmRenderer.Config.sOVoxelData.name}");
                bool isExist = false;

                // проходим по уже существующим розеткам.
                foreach (KeyValuePair<string, Voxel[]> item in dictionarySocketsNames)
                {
                    // если есть совпадения, присваиваем уже существующее имя розетки.
                    if (HelperVoxel.AreColorEqual(item.Value, listColors))
                    {
                        configOption = item.Key;
                        isExist = true;
                        break;
                    }
                }

                // если нет совпадений в существующих розетках.
                // добавляем новую розетку.
                if (!isExist)
                {
                    nextName = nextName + 1;
                    string newName = $"{nextName}";
                    dictionarySocketsNames.Add(newName, listColors);
                    configOption = newName;
                }
            } else
            // если цветов нет в массиве, присваиваем розетке имя по умолчанию как -1;
            {
                configOption = "-1";
            }
        }
    }


    [System.Serializable]
    public struct ColorsBorders
    {
        public Voxel[] ColorsRight;
        public Voxel[] ColorsForward;
        public Voxel[] ColorsLeft;
        public Voxel[] ColorsBack;
        public Voxel[] ColorsTop;
        public Voxel[] ColorsBottom;
    }
}
#endif