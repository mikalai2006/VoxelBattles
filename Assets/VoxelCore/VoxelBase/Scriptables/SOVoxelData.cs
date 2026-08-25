using Mikalai2006.VoxelBase;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOVoxelData", menuName = "SO/VoxelData")]
public class SOVoxelData : ScriptableObject
{
    [Tooltip("Уникальный ID модели. Должен быть уникальным для каждого ассета.")]
    public int runtimeModelId;

    public List<SubmeshesData> groups;
    public int countVoxels;
    // public List<Vector3Int> voxels;
    // public List<Color> colors;
    public float sizeVoxel;
    public Vector3 Pivot;
    public Vector3Int Bounds;
    public Vector3Int GlobalSize;
    public Vector3 GlobalPosition;
    public Quaternion GlobalRotation;
    public Vector3Int LocalSize;
    public Vector3 LocalPosition;

    public Quaternion LocalRotation;

    [Tooltip("Меш для начальной загрузки (позволяет использовать все возможности GPU Instancing до начала разрушения)")]
    public Mesh startMesh;

    public TypeEntity typeEntity;

    public RotationType Rotation;

    [Space(15)]
    [Header("Tiledata")]
    [HideInInspector] public Voxel[] ColorsRight;
    [HideInInspector] public Voxel[] ColorsForward;
    [HideInInspector] public Voxel[] ColorsLeft;
    [HideInInspector] public Voxel[] ColorsBack;
    [HideInInspector] public Voxel[] ColorsTop;
    [HideInInspector] public Voxel[] ColorsBottom;

    [Tooltip("Розетки")]
    public TileSockets tileSockets;
    // [Tooltip("Возможные соседи")]
    // public TileNeghboursList TileNeghboursList;


    public SubmeshesData GetVoxelGroup(Vector3 position)
    {
        SubmeshesData output = default;
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].voxels.Contains(VoxelHelpers.RoundVector3(position)))
            {
                output = groups[i];
                break;
            }
            ;
        }

        return output;
    }

    // Автоматическая генерация ID на основе имени, если забыли указать вручную
    private void OnValidate()
    {
        if (runtimeModelId == 0)
        {
            runtimeModelId = name.GetHashCode();
        }
    }
}


[System.Serializable]
public struct SubmeshesData
{
    public Color color;
    public List<Vector3Int> voxels;
}


[System.Serializable]
public struct TileSockets
{
    public string name;
    public int rotation;
    public string posX;
    public string negX;
    public string posY;
    public string negY;
    public string posZ;
    public string negZ;
    public int weight;
}

[System.Serializable]
public struct TileNeghboursList
{
    public string[] pX;
    public string[] nX;
    public string[] pY;
    public string[] nY;
    public string[] pZ;
    public string[] nZ;
}

[System.Serializable]
public enum RotationType
{
    OnlyRotation,
    TwoRotations,
    FourRotations
}