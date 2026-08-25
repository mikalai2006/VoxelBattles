using Mikalai2006.VoxelBase;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatePlayer
{
  public int playDay;
  public string lastDay;
  public string gerbId;
  public int coutBattle;
  public int countWin;
  public int rank;
  public int score;
  public int indexActiveMachine;
  public List<StateMachinePlayer> machines;
  public int coin;

  public StatePlayer()
  {
    machines = new();
  }
}

[Serializable]
public class StateMachinePlayer
{
  public string name;
  public StateMachinePlayerData data;

  public StateMachinePlayer()
  {
    data = new();
  }
}

[Serializable]
public class StateMachinePlayerData
{
  public List<ColorsModify> colorsModifies;
  public List<DataDetail> dataDetails;
  public StateMachinePlayerData()
  {
    colorsModifies = new();
    dataDetails = new();
  }
}

//[Serializable]
//public class DataDetail
//{
//    public string ido;
//    public string parentId;
//    public int number;
//    public string nameConfig;
//    public VehicleDetailType type;
//    // [NonSerialized] public Vector3 Bounds;
//    public Vector3 offset;
//    public SerializeVector3 destroyVoxels;
//    public DataDetail()
//    {
//        destroyVoxels = new();
//    }
//}

[Serializable]
public class StateLevel
{
  public List<TeamData> teams;
  public List<MachineLevelData> machines;

  public StateLevel()
  {
    teams = new();
    machines = new();
  }
}

[Serializable]
public class MachineLevelData
{
  public string id;
  public string gerbId;
  public string name;
  public int rank;
  public bool isBot;
  public StateMachinePlayerData data;
  // public List<ColorsModify> colorsModify;
  // public List<Vector3Int> destroyedVoxels;

  public MachineLevelData()
  {
    data = new();
  }
}


[Serializable]
public class TeamData
{
    public int index;
    public string logo;
}
