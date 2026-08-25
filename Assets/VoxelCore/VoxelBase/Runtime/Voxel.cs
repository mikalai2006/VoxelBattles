using System;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [Serializable]
    public struct Voxel
    {
        public byte ID;
        public Color32 color;
        public Vector3Int position;
        public VoxelType type;
        //public int IndexSubMesh;
        //public bool IsActive;

        public bool isSolid
        {
            get
            {
                return ID != 0;
            }
        }

        public void SetType(VoxelType _type)
        {
            type = _type;
        }
    }

    [Serializable]
    public struct VoxelColors
    {
        public VoxelType type;
        public Color color;
    }
}

public enum VoxelType : byte
{
    Air, // Or None
    Grass,
    Dirt,
    Stone,
    OakLog,
    Red,
    Blue,
    Green,
    Gray,
    Silver,
    Olive,
    Yellow,
    Magenta,
    Black,
    White,
    Destroyed
}


[Serializable]
public struct RemoveVoxel
{
    public Vector3 position;
    public Color color;
}