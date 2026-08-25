using System;
using System.Collections.Generic;
using System.Linq;
using Mikalai2006.VoxelBase;
using UnityEngine;


namespace Mikalai2006.VoxelMap {
    // [RequireComponent(typeof(VoxelMeshRender))]
    public class Tile3D : MonoBehaviour
    {
        GameManager gameManager => GameManager.Instance;
        public string UID;
        // public float VoxelSize = 0.1f;
        public int TileSideVoxels = 5;

        [Range(1, 100)]
        public int Weight = 50;

        public RotationType Rotation;
        [HideInInspector] public VoxelMeshRender voxelMeshRender;

        public bool isEmpty;
        public bool isActive = true;


        [HideInInspector] public Voxel[] ColorsRight;
        [HideInInspector] public Voxel[] ColorsForward;
        [HideInInspector] public Voxel[] ColorsLeft;
        [HideInInspector] public Voxel[] ColorsBack;
        [HideInInspector] public Voxel[] ColorsTop;
        [HideInInspector] public Voxel[] ColorsBottom;

        [Tooltip("Розетки")]
        public TileSockets tileSockets;
        [Tooltip("Возможные соседи")]
        public TileNeghboursList TileNeghboursList;
        
        [HideInInspector] public MeshConfig meshConfig;

        public Tile3D[] tileOptions;
        public bool isCollapsed;
        public bool isTop;
        public bool isGround;

        private void Awake()
        {
            voxelMeshRender = transform.GetComponentInChildren<VoxelMeshRender>();
        }

        public void CreateNode(bool collapseState, List<Tile3D> tiles)
        {
            isCollapsed = collapseState;
            tileOptions = tiles.ToArray();
        }

        public void RecreateNode(Tile3D[] tiles)
        {
            tileOptions = tiles;
        }

        public void Start()
        {

            if (voxelMeshRender == null)
            {
                Debug.Log($"<color=red>Ошибка тайла {gameObject.name}: Не найден компонент VoxelMeshRender</color>");
                return;
            }

            //if (gameManager != null)
            //{
            //    voxelMeshRender.SetColorsModify(gameManager.LevelConfig.colorsModify);
            //}
            // else
            // {
            //     Debug.Log("Start Tile3D");
            // }

            //voxelMeshRender.OnSetData += OnStart;
        }

        void OnDestroy()
        {
            //voxelMeshRender.OnSetData -= OnStart;
        }

        public void OnStart()
        {
            meshConfig = voxelMeshRender.Config;

            if (!voxelMeshRender.Config.isOneMesh)
            {
                Debug.Log($"<color=red>Tile3D требует одного меша: В компоненте {gameObject.name} не установлена опция - isOneMesh</color>");
                return;
            }

            TileSideVoxels = meshConfig.sOVoxelData.Bounds.x;

            if (!(meshConfig.sOVoxelData.Bounds.x == meshConfig.sOVoxelData.Bounds.y
                && meshConfig.sOVoxelData.Bounds.y == meshConfig.sOVoxelData.Bounds.z))
            {
                // Debug.Log($"<color=yellow>Предупреждение: стороны тайла {gameObject.name} должны быть равными по трем измерениям! Текущее значение: {meshConfig.sOVoxelData.Bounds}</color>");
                TileSideVoxels = Mathf.Max(meshConfig.sOVoxelData.Bounds.x, meshConfig.sOVoxelData.Bounds.y, meshConfig.sOVoxelData.Bounds.z);
            }

            // ColorsRight = new Voxel[TileSideVoxels * TileSideVoxels];
            // ColorsForward = new Voxel[TileSideVoxels * TileSideVoxels];
            // ColorsLeft = new Voxel[TileSideVoxels * TileSideVoxels];
            // ColorsBack = new Voxel[TileSideVoxels * TileSideVoxels];
            // ColorsTop = new Voxel[TileSideVoxels * TileSideVoxels];
            // ColorsBottom = new Voxel[TileSideVoxels * TileSideVoxels];
            
            if (isEmpty)
            {
                // ColorsRight = new Voxel[0];
                // ColorsForward = new Voxel[0];
                // ColorsLeft = new Voxel[0];
                // ColorsBack = new Voxel[0];
                // ColorsTop = new Voxel[0];
                // ColorsBottom = new Voxel[0];

                ColorsRight = Enumerable.Repeat(new Voxel() { color = Color.clear }, TileSideVoxels * TileSideVoxels).ToArray();
                ColorsForward = Enumerable.Repeat(new Voxel() { color = Color.clear }, TileSideVoxels * TileSideVoxels).ToArray();
                ColorsLeft = Enumerable.Repeat(new Voxel() { color = Color.clear }, TileSideVoxels * TileSideVoxels).ToArray();
                ColorsBack = Enumerable.Repeat(new Voxel(){color=Color.clear}, TileSideVoxels*TileSideVoxels).ToArray();
                ColorsTop = Enumerable.Repeat(new Voxel(){color=Color.clear}, TileSideVoxels*TileSideVoxels).ToArray();
                ColorsBottom = Enumerable.Repeat(new Voxel(){color=Color.clear}, TileSideVoxels*TileSideVoxels).ToArray();
            } else
            {
                ColorsRight = voxelMeshRender.Config.sOVoxelData.ColorsRight.ToArray();
                ColorsForward = voxelMeshRender.Config.sOVoxelData.ColorsForward.ToArray();
                ColorsLeft = voxelMeshRender.Config.sOVoxelData.ColorsLeft.ToArray();
                ColorsBack = voxelMeshRender.Config.sOVoxelData.ColorsBack.ToArray();
                // Debug.Log($"{name} ColorTop: {voxelMeshRender.Config.sOVoxelData.ColorsTop.Length}");
                ColorsTop = voxelMeshRender.Config.sOVoxelData.ColorsTop != null ? voxelMeshRender.Config.sOVoxelData.ColorsTop.ToArray() : new Voxel[TileSideVoxels * TileSideVoxels];
                ColorsBottom = voxelMeshRender.Config.sOVoxelData.ColorsBottom != null ? voxelMeshRender.Config.sOVoxelData.ColorsBottom.ToArray() : new Voxel[TileSideVoxels * TileSideVoxels];
            }

            // CalculateSidesColors();
        }

        // public void CalculateSidesColors()
        // {
        //     for (int i = 0; i < TileSideVoxels; i++)
        //     {
        //         for (int y = 0; y < TileSideVoxels; y++)
        //         {
        //             ColorsForward[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Forward);
        //             ColorsRight[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Right);
        //             ColorsLeft[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Left);
        //             ColorsBack[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Back);
        //             // ColorsTop[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Top);
        //             // ColorsBottom[i * TileSideVoxels + y] = GetVoxelColor(y, i, DirectionSideTile.Bottom);
        //         }
        //     }
        // }

        public void Rotate90()
        {
            // var TileSideVoxels = meshConfig.sOVoxelData.Bounds.x;
            transform.Rotate(0, 90, 0);

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
                    // colorsRightNew[row * TileSideVoxels + column] = ColorsForward[row * TileSideVoxels + TileSideVoxels - column - 1];
                    // colorsForwardNew[row * TileSideVoxels + column] = ColorsLeft[row * TileSideVoxels + column];
                    // colorsLeftNew[row * TileSideVoxels + column] = ColorsBack[row * TileSideVoxels + TileSideVoxels - column - 1];
                    // colorsBackNew[row * TileSideVoxels + column] = ColorsRight[row * TileSideVoxels + column];
                    // // TODO
                    colorsForwardNew[row * TileSideVoxels + column] = ColorsRight[row * TileSideVoxels + column];
                    colorsRightNew[row * TileSideVoxels + column] = ColorsBack[row * TileSideVoxels + TileSideVoxels - column - 1];
                    colorsBackNew[row * TileSideVoxels + column] = ColorsLeft[row * TileSideVoxels + column];
                    colorsLeftNew[row * TileSideVoxels + column] = ColorsForward[row * TileSideVoxels + TileSideVoxels - column - 1];
                }
            }
            Voxel[] colorsTopNew = Rotate90TopBottom(ColorsTop);
            Voxel[] colorsBottomNew = Rotate90TopBottom(ColorsBottom);

            // colorsRightNew = ColorsForward;
            // colorsForwardNew = ColorsLeft;
            // colorsLeftNew = ColorsBack;
            // colorsBackNew = ColorsRight;

            ColorsRight = colorsRightNew;
            ColorsForward = colorsForwardNew;
            ColorsLeft = colorsLeftNew;
            ColorsBack = colorsBackNew;
            
            ColorsTop = colorsTopNew;
            ColorsBottom = colorsBottomNew;
        }

        public Voxel[] Rotate90TopBottom(Voxel[] arrayVoxels)
        {
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

        // public Voxel GetVoxelColor(int verticalLayer, int horizontalOffset, DirectionSideTile direction)
        // {
        //     Voxel vox = default;

        //     if (direction == DirectionSideTile.Forward)
        //     {
        //         vox = voxelMeshRender.GetVoxel(0, new Vector3Int(horizontalOffset, verticalLayer, 0));
        //     }
        //     else if (direction == DirectionSideTile.Right)
        //     {
        //         vox = voxelMeshRender.GetVoxel(0, new Vector3Int(0, verticalLayer,  horizontalOffset));
        //     }
        //     else if (direction == DirectionSideTile.Back)
        //     {
        //         vox = voxelMeshRender.GetVoxel(0, new Vector3Int(horizontalOffset, verticalLayer, TileSideVoxels - 1)); // TileSideVoxels - horizontalOffset - 1
        //     }
        //     else if (direction == DirectionSideTile.Left)
        //     {
        //         vox = voxelMeshRender.GetVoxel(0, new Vector3Int(TileSideVoxels - 1, verticalLayer, horizontalOffset)); // TileSideVoxels - horizontalOffset - 1
        //     }
        //     // Color32 voxColor = (Color32)vox.color;
        //     // var meshCollider = GetComponentInChildren<MeshCollider>();

        //     // float vox = VoxelSize;
        //     // float half = VoxelSize / 2;

        //     // Vector3 rayStart;
        //     // Vector3 rayDir;
        //     // if (direction == Direction.Right)
        //     // {
        //     //     rayStart = meshCollider.bounds.min +
        //     //                new Vector3(-half, 0, half + horizontalOffset * vox);
        //     //     rayDir = Vector3.right;
        //     // }
        //     // else if (direction == Direction.Forward)
        //     // {
        //     //     rayStart = meshCollider.bounds.min +
        //     //                new Vector3(half + horizontalOffset * vox, 0, -half);
        //     //     rayDir = Vector3.forward;
        //     // }
        //     // else if (direction == Direction.Left)
        //     // {
        //     //     rayStart = meshCollider.bounds.max +
        //     //                new Vector3(half, 0, -half - (TileSideVoxels - horizontalOffset - 1) * vox);
        //     //     rayDir = Vector3.left;
        //     // }
        //     // else if (direction == Direction.Back)
        //     // {
        //     //     rayStart = meshCollider.bounds.max +
        //     //                new Vector3(-half - (TileSideVoxels - horizontalOffset - 1) * vox, 0, half);
        //     //     rayDir = Vector3.back;
        //     // }
        //     // else
        //     // {
        //     //     throw new ArgumentException("Wrong direction value, should be Direction.left/right/back/forward",
        //     //         nameof(direction));
        //     // }

        //     // rayStart.y = meshCollider.bounds.min.y + half + verticalLayer * vox;

        //     // //Debug.DrawRay(rayStart, direction * .1f, Color.blue, 2);

        //     // if (Physics.Raycast(new Ray(rayStart, rayDir), out RaycastHit hit, vox))
        //     // {
        //     //     byte colorIndex = (byte)(hit.textureCoord.x * 256);

        //     //     if (colorIndex == 0) Debug.LogWarning("Found color 0 in mesh palette, this can cause conflicts");

        //     //     return colorIndex;
        //     // }

        //     return vox;
        // }

        public void OnRefreshData()
        {
            Start();

            CreateArraysColors(ref voxelMeshRender.Config.sOVoxelData);
        }

        
        private void CreateArraysColors(ref SOVoxelData sOVoxelData) {
            var TileSideVoxels = Mathf.Max(sOVoxelData.Bounds.x, sOVoxelData.Bounds.y, sOVoxelData.Bounds.z);

            sOVoxelData.ColorsRight = new Voxel[TileSideVoxels * TileSideVoxels];
            sOVoxelData.ColorsForward = new Voxel[TileSideVoxels * TileSideVoxels];
            sOVoxelData.ColorsLeft = new Voxel[TileSideVoxels * TileSideVoxels];
            sOVoxelData.ColorsBack = new Voxel[TileSideVoxels * TileSideVoxels];
            sOVoxelData.ColorsTop = new Voxel[TileSideVoxels * TileSideVoxels];
            sOVoxelData.ColorsBottom = new Voxel[TileSideVoxels * TileSideVoxels];

            for (int row = 0; row < TileSideVoxels; row++)
            {
                for (int column = 0; column < TileSideVoxels; column++)
                {
                    sOVoxelData.ColorsForward[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Forward, sOVoxelData);
                    sOVoxelData.ColorsRight[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Right, sOVoxelData);
                    sOVoxelData.ColorsLeft[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Left, sOVoxelData);
                    sOVoxelData.ColorsBack[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Back, sOVoxelData);
                    sOVoxelData.ColorsTop[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Top, sOVoxelData);
                    sOVoxelData.ColorsBottom[row * TileSideVoxels + column] = GetVoxelColor(row, column, DirectionSideTile.Bottom, sOVoxelData);
                }
            }
        }
        
        public Voxel GetVoxelColor(int y, int column, DirectionSideTile direction, SOVoxelData sOVoxelData)
        {
            var TileSideVoxels = Mathf.Max(sOVoxelData.Bounds.x, sOVoxelData.Bounds.y, sOVoxelData.Bounds.z);

            Vector3Int position = Vector3Int.zero;

            if (direction == DirectionSideTile.Forward)
            {
                position = new Vector3Int(column, y, 0);
            }
            else if (direction == DirectionSideTile.Right)
            {
                position = new Vector3Int(TileSideVoxels - 1, y, column);
            }
            else if (direction == DirectionSideTile.Back)
            {
                position = new Vector3Int(column, y, TileSideVoxels - 1);
            }
            else if (direction == DirectionSideTile.Left)
            {
                position = new Vector3Int(0, y, column);
            }
            else if (direction == DirectionSideTile.Top)
            {
                position = new Vector3Int(column, TileSideVoxels - 1, y);
            }
            else if (direction == DirectionSideTile.Bottom)
            {
                position = new Vector3Int(column, 0, y);
            }

            // var index = sOVoxelData.voxels.FindIndex(x => x == position);
            Color color = Color.clear;


            // if (index > -1)
            // {
                var positionVoxel = position;

                for (int g = 0; g < sOVoxelData.groups.Count; g++)
                {
                    for (int el = 0; el < sOVoxelData.groups[g].voxels.Count; el++)
                    {
                        if (sOVoxelData.groups[g].voxels[el].x == positionVoxel.x
                            && sOVoxelData.groups[g].voxels[el].y == positionVoxel.y
                            && sOVoxelData.groups[g].voxels[el].z == positionVoxel.z)
                        {
                            color = sOVoxelData.groups[g].color;
                            break;
                        }
                    }
                    if (color != Color.clear)
                    {
                        break;
                    }
                }

            // color = sOVoxelData.colors[index];
            // }

            Voxel vox = new Voxel()
            {
                color = color,
                position = position,

            };

            return vox;
        }

        public void SetActive(bool v)
        {
            isActive = v;
        }
    }

    public enum DirectionSideTile
    {
        Left,
        Right,
        Back,
        Forward,
        Top,
        Bottom
    }
}