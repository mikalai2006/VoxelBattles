#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mikalai2006.VoxelBase;
using Mikalai2006.VoxelMap;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using VoxReader.Interfaces;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxParser : MonoBehaviour
{
    [SerializeField] private string[] files;
    // [SerializeField] private TextAsset asset;
    // [SerializeField] private string folderPrefix = "panzers";
    [Tooltip("Если активировать, то не будут сжиматься модели. Будут записаны координаты как есть и ограничивающая рамка будет как задана в редакторе VoxelMagic")]
    [SerializeField] private bool isGlobalPosition;
    [SerializeField] private bool isCreateMesh;
    [SerializeField] private bool isTiles;
    [SerializeField] private TypeEntity typeEntity;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public string OutputPath = "Assets/Prefabs/1Vox";
    public string customDir;


    void Start()
    {
        // CreateData();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void OnSetFiles(string[] paths)
    {
        files = paths;
    }

    public void OnCreateData()
    {
        foreach (string path in files)
        {
            CreateData(path);
        }
    }

    void CreateData(string pathFile)
    {
        // if (!AssetDatabase.IsValidFolder($"{OutputPath}/SO/{folderPrefix}"))
        // {
        //     AssetDatabase.CreateFolder($"{OutputPath}/SO", folderPrefix);
        // }

        IVoxFile voxFile = VoxReader.VoxReader.Read(pathFile);


        // Access models of .vox file
        IModel[] models = voxFile.Models;

        // Access voxels of first model in the file
        for (int m = 0; m < models.Length; m++)
        {
            var model = models[m];

            // формируем данные для сохранения файла.
            string[] namePathArray = pathFile.Split(new char[] { '/' });
            string nameFolderModel = Path.GetFileNameWithoutExtension(namePathArray[namePathArray.Length - 1]);
            string nameDirForAllModel = nameFolderModel;
            if (customDir != "")
            {
                nameDirForAllModel = Path.GetFileNameWithoutExtension(customDir);
            }

            string modelName = models[m].Name;

            if (!AssetDatabase.IsValidFolder($"{OutputPath}/{nameDirForAllModel}"))
            {
                AssetDatabase.CreateFolder($"{OutputPath}", nameDirForAllModel);
            }
            if (!AssetDatabase.IsValidFolder($"{OutputPath}/{nameDirForAllModel}/data"))
            {
                AssetDatabase.CreateFolder($"{OutputPath}/{nameDirForAllModel}", "data");
            }

            string nameFile = $"{nameFolderModel}_{modelName}";
            if (isTiles)
            {
                nameFile = $"{modelName}_{nameFolderModel}";
            }
            string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputPath}/{nameDirForAllModel}/data/{nameFile}.asset");
            // Debug.Log($"path={path}, OutputPath={OutputPath}, nameDirForAllModel={nameDirForAllModel}");
            string pathMesh = AssetDatabase.GenerateUniqueAssetPath($"{OutputPath}/{nameDirForAllModel}/data/{nameFile}_mesh.asset");


            // генерируем данные для модели.
            VoxReader.Voxel[] voxels = model.Voxels;

            Debug.Log($"Voxels: name={model.Name} count={voxels.Length}, global size = {model.GlobalSize}, isCopy={model.IsCopy}");

            // // Access properties of a voxel
            // VoxReader.Vector3 position = voxels[0].GlobalPosition;
            // VoxReader.Color color = voxels[0].Color;

            // List<UnityEngine.Vector3> points = new();
            // List<UnityEngine.Color> colors = new();
            Dictionary<UnityEngine.Vector3Int, UnityEngine.Color> pointsColors = new();

            UnityEngine.Vector3 pivot = UnityEngine.Vector3.zero;
            UnityEngine.Vector3Int bounds = new UnityEngine.Vector3Int(model.GlobalSize.X, model.GlobalSize.Z, model.GlobalSize.Y);

            UnityEngine.Vector3Int min = new UnityEngine.Vector3Int(model.GlobalSize.X, model.GlobalSize.Z, model.GlobalSize.Y);
            UnityEngine.Vector3Int max = UnityEngine.Vector3Int.zero;


            // Проходим по всем точкам и находим смещения по всем осям.
            for (int x = 0; x < voxels.Count(); x++) // Используем ref для изменения оригинальных значений
            {
                VoxReader.Voxel voxel = voxels[x];
                if (isGlobalPosition == false)
                {
                    min = new Vector3Int(
                        Mathf.Min(min.x, voxel.LocalPosition.X),
                        Mathf.Min(min.y, voxel.LocalPosition.Z),
                        Mathf.Min(min.z, voxel.LocalPosition.Y)
                    );

                    max.x = Mathf.Max(max.x, voxel.LocalPosition.X);
                    max.y = Mathf.Max(max.y, voxel.LocalPosition.Z);
                    max.z = Mathf.Max(max.z, voxel.LocalPosition.Y);
                }
            }
            // Debug.Log($"min:{min}, max:{max}");


            foreach (VoxReader.Voxel voxel in voxels) // Используем ref для изменения оригинальных значений
            {
                pointsColors.Add(
                    isGlobalPosition
                        ? new UnityEngine.Vector3Int(voxel.LocalPosition.X, voxel.LocalPosition.Z, voxel.LocalPosition.Y)
                        : new UnityEngine.Vector3Int(voxel.LocalPosition.X - min.x, voxel.LocalPosition.Z - min.y, voxel.LocalPosition.Y - min.z),
                    new UnityEngine.Color(voxel.Color.R / 255f, voxel.Color.G / 255f, voxel.Color.B / 255f, voxel.Color.A / 255f)
                );

                // points.Add(new UnityEngine.Vector3(voxel.GlobalPosition.X, voxel.GlobalPosition.Z, voxel.GlobalPosition.Y));
                // // Debug.Log($"voxel.Color=>{voxel.Color}|{voxel.Color.R}, {voxel.Color.G}, {voxel.Color.B}, {voxel.Color.A}");
                // colors.Add(new UnityEngine.Color(voxel.Color.R / 255f, voxel.Color.G / 255f, voxel.Color.B / 255f, voxel.Color.A / 255f));
            }

            if (isGlobalPosition == false)
            {
                bounds.x = max.x - min.x + 1;
                bounds.y = max.y - min.y + 1;
                bounds.z = max.z - min.z + 1;

            }
            
            pivot.x = bounds.x / 2f;
            pivot.y = bounds.y / 2f;
            pivot.z = bounds.z / 2f;

            var groupSubMeshes = pointsColors.GroupBy(obj => obj.Value)
                .AsParallel()
                // .Select(group => group)
                .ToList();
            Debug.Log($"uniqueColors: {groupSubMeshes.Count()}");

            // foreach (var group in groupSubMeshes)
            // {
            //     Debug.Log($"key: {group.Key}");
            //     foreach (var voxel in group)
            //     {
            //         Debug.Log($"  - {voxel.Key} ({voxel.Value})");
            //     }
            // }
            List<SubmeshesData> submeshesDatas = new();
            for (int n = 0; n < groupSubMeshes.Count; n++)
            {
                SubmeshesData submeshesData = new SubmeshesData()
                {
                    color = groupSubMeshes[n].Key,
                    voxels = groupSubMeshes[n].AsParallel().ToDictionary(t => Vector3Int.FloorToInt(t.Key), t => t.Value).Keys.ToList(),
                };
                submeshesDatas.Add(submeshesData);
            }

            // Create ScriptableObject with data voxels and colors.
            SOVoxelData asset = ScriptableObject.CreateInstance<SOVoxelData>();
            asset.groups = submeshesDatas;
            // asset.voxels = pointsColors.Keys.ToList(); // .AsParallel()
            asset.countVoxels = pointsColors.Count;
            asset.Pivot = pivot;
            asset.Bounds = bounds;
            // asset.colors = pointsColors.Values.AsParallel().ToList();
            asset.GlobalSize = new UnityEngine.Vector3Int(model.GlobalSize.X, model.GlobalSize.Y, model.GlobalSize.Z);
            asset.GlobalPosition = new UnityEngine.Vector3(model.GlobalPosition.X, model.GlobalPosition.Y, model.GlobalPosition.Z);

            Matrix4x4 globalRotation = Matrix4x4.identity;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    globalRotation[i, j] = model.GlobalRotation[i, j];
                }
            }
            asset.GlobalRotation = globalRotation.rotation;
            float y = asset.GlobalRotation.y;
            asset.GlobalRotation.y = asset.GlobalRotation.z;
            asset.GlobalRotation.z = y;

            Matrix4x4 localRotation = Matrix4x4.identity;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    localRotation[i, j] = model.LocalRotation[i, j];
                }
            }
            asset.LocalRotation = localRotation.rotation;
            float y2 = asset.LocalRotation.y;
            asset.LocalRotation.y = asset.LocalRotation.z;
            asset.LocalRotation.z = y2;

            asset.LocalSize = new UnityEngine.Vector3Int(model.LocalPosition.X, model.LocalPosition.Y, model.LocalPosition.Z);
            asset.LocalPosition = new UnityEngine.Vector3(model.LocalPosition.X, model.LocalPosition.Y, model.LocalPosition.Z);
            // asset.GlobalRotation = new UnityEngine.Vector3(model.GlobalRotation[0,0],);
            asset.sizeVoxel = 1f; // / Mathf.Min(CubeVoxels.size.x, CubeVoxels.size.y, CubeVoxels.size.z);
            
            asset.typeEntity = typeEntity;

            // Create mesh.
            if (isCreateMesh)
            {
                Mesh mesh = CreateMesh(nameFolderModel, asset, pathMesh);
                asset.startMesh = mesh;
            }

            // Create arrays colors for tileGenerator.
            if (isTiles)
            {
                asset = CreateArraysColors(asset);
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

        }

    }

    private SOVoxelData CreateArraysColors(SOVoxelData sOVoxelData) {
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

        return sOVoxelData;
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

        Color color = Color.clear;
        // var index = sOVoxelData.voxels.FindIndex(x => x == position);

        // if (index > -1)
        // {
        //     color = sOVoxelData.colors[index];
        // }
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

        Voxel vox = new Voxel()
        {
            color = color,
            position = position,

        };

        return vox;
    }

    private Mesh CreateMesh(string meshName, SOVoxelData sOVoxelData, string path)
    {
        var TileSideVoxels = Mathf.Max(sOVoxelData.Bounds.x, sOVoxelData.Bounds.y, sOVoxelData.Bounds.z);
        Mesh mesh = new Mesh();  //meshFilter.sharedMesh;
        mesh.name = meshName;

        NativeArray<Mikalai2006.VoxelBase.Voxel> arrayVoxels = new NativeArray<Mikalai2006.VoxelBase.Voxel>(sOVoxelData.Bounds.x * sOVoxelData.Bounds.y * sOVoxelData.Bounds.z, Allocator.Persistent);
        NativeArray<VoxelColors> arrayVoxelColors = new NativeArray<VoxelColors>(sOVoxelData.groups.Count + 1, Allocator.Persistent);

        // parse list voxels and create data. 
        for (int j = 0; j < sOVoxelData.groups.Count; j++)
        {
            Color color = sOVoxelData.groups[j].color;
            color.a = 1;
            arrayVoxelColors[j + 1] = new VoxelColors()
            {
                color = color,
                type = (VoxelType)(j + 1)
            };


            for (int i = 0; i < sOVoxelData.groups[j].voxels.Count; i++)
            {

                Vector3Int pos = Vector3Int.FloorToInt(sOVoxelData.groups[j].voxels[i]);
                var vox = new Voxel() // * scale
                {
                    ID = 1,
                    color = color, //meshConfig.sOVoxelData.colors.ElementAt(i),
                    type = (VoxelType)(j + 1),
                    position = pos,
                    //IndexSubMesh = j,
                };

                arrayVoxels[HelperVoxel.To1D(pos.x, pos.y, pos.z, isGlobalPosition ? TileSideVoxels :  sOVoxelData.Bounds.x, isGlobalPosition ? TileSideVoxels : sOVoxelData.Bounds.y)] = vox;
            }
        }


        var meshArray = Mesh.AllocateWritableMeshData(mesh);
        var _job = new MeshGreedyJob();
        _job.mesh = meshArray[0];
        _job.chunkSize = new int3(sOVoxelData.Bounds.x, sOVoxelData.Bounds.y, sOVoxelData.Bounds.z);
        _job.blockSize = 1;
        _job.voxelColors = arrayVoxelColors;
        _job.voxels = arrayVoxels;
        _job.Schedule().Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshArray, mesh);

        // FIXME: For some reason setting bounds directly doesn't work so this is needed as a workaround, investigate
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        // Get the mesh from the MeshFilter
        Mesh meshToSave = meshFilter.sharedMesh;

        // Define the save path and filename
        // string path = EditorUtility.SaveFilePanelInProject("Save Procedural Mesh", "NewProceduralMesh", "mesh", "Save the generated mesh asset.");

        if (string.IsNullOrEmpty(path))
        {
            return null; // User cancelled the save operation
        }

        // Create and save the mesh asset
        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        arrayVoxels.Dispose();
        arrayVoxelColors.Dispose();

        return meshToSave;
    }

}
#endif