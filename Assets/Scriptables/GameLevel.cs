using System;
using System.Collections.Generic;
using Mikalai2006.VoxelBase;
using Mikalai2006.VoxelMap;
using UnityEngine;

[CreateAssetMenu]
public class GameLevel : ScriptableObject
{
    [Space(5)]
    [Header("Настройки карты")]
    public LevelData levelData;
    public List<Tile3D> TilePrefabs;
    public List<VoxelMeshRender> TreesPrefabs;
    // public List<Tile3D> TilePrefabsEmpty;
    // public List<Tile3D> TilePrefabsInner;
    // public List<Tile3D> TilePrefabsInnerTop;
    // public VoxelMeshRender planePrefab;
    // public List<VoxelMeshRender> TreePrefabs;

    [Space(5)]
    [Header("Map")]
    // public TypeGround typeGround;
    // public RuleTile tileRuleCave;
    // public RuleTile tileLandscape;
    // public RuleTile tileSecondLandscape;
    // public RuleTile tileBorder;
    // public List<RuleTile> tileObstcles;
    // [Range(0.1f, 1f)] public float noiseScaleObstacleKoof = 0.2f;
    // [Range(0.1f, 1f)] public float noiseObstacleMaxKoof = 0.4f;
    // [Range(0.1f, 1f)] public float noiseScaleKoof = 0.2f;
    // [Range(0.1f, 1f)] public float noiseMaxKoof = 0.4f;
    [Range(0f, 1f)] public float light;

    [Header("Цвета карты")]
    [Tooltip("Цвета, в которые будут окрашены объекты на карте: 1 - земля, 2 - стены, 3 - природа")]
    public List<ColorsModify> colorsModify;
    public Color colorGround;
    public Color colorWall;
    public Color colorNature;


    // [Space(5)]
    // [Header("Prefabs GameObjects")]
    // public VoxelMeshRender[] testObjects;
    // public Cell3D prefabPlaceholder;

    [Space(5)]
    [Header("Player")]
    public TypeLevel typeLevel;
    public int countTeam;
    public int countPlayers;

    // public List<ItemProbabiliti<GameBonus>> bonuses;


}

// [Serializable]
// public struct SaveTiled
// {
//     public string nameMap;
//     public Cell3DData[] gridComponents;
// }

[Serializable]
public struct LevelData
{
    [Tooltip("Максимальная высота карты")]
    public int maxHeight;
    [Tooltip("x - rows, y - depth, z - cols")]
    public Vector3Int size;
    public List<DataGroupTiles> caves;
    public List<DataGroupTiles> houses;
    public List<DataGroupTiles> zabor;
    public List<DataGroupTiles> trees;
}

//[Serializable]
//public struct LevelDataGroup
//{
//    public int group;
//    public int team;
//    public List<Cell3DData> tiles;
//}

//[Serializable]
//public struct ColorsModify
//{
//    public TypeEntity typeEntity;
//    public Color32 input;
//    public Color32 output;
//}

[Serializable]
public struct FillData
{
    public SubmeshesData voxelGroupData;
    public Color32 outputColor;

}

[Serializable]
public enum TypeLevel
{
    Command = 1,
    Alone = 2,
}