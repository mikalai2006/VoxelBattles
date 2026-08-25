using System;
using System.Collections.Generic;
using System.Linq;
using Mikalai2006.VoxelBase;
using UnityEngine;

public class StateManager
{
  public static event Action<StatePlayer> OnChangeState;

  private GameManager _gameManager => GameManager.Instance;
  private GameSetting _gameSetting => GameManager.Instance.Settings;
  public StatePlayer statePlayer;
  public StateLevel stateLevel;

 public StatePlayer Init(StatePlayer _stateGame, bool reset = false)
  {
    if (_stateGame == null && !reset)
    {

      // Debug.Log("Get stateGame from playPref");
      _stateGame = JsonUtility.FromJson<StatePlayer>(PlayerPrefs.GetString(_gameManager.Settings.nameSaveData));
    }
    if (_stateGame == null)
    {
      // Debug.Log("Create new stateGame");
      _stateGame = new()
      {
        gerbId = _gameSetting.gerbs.ElementAt(UnityEngine.Random.Range(0, _gameSetting.gerbs.Count)).name,
        machines = new(),
      };
      _gameManager.DataManager.Save(true);
    }

    statePlayer = _stateGame;



    return statePlayer;
  }

  public void AddMachine(string name)
  {
      statePlayer.machines.Add(new StateMachinePlayer
      {
          name = name
      });

    OnChangeState?.Invoke(statePlayer);
  }

  public void InitDataAloneLevel()
  {
    //stateLevel = new();

    //// генерация случайных имен.
    //var listRandomNames = _gameSetting.names.OrderBy(t => UnityEngine.Random.value);

    //for (int i = 0; i < _gameManager.LevelConfig.countPlayers; i++)
    //{
    //  var allMachines = _gameManager.ResourceSystem.GetAllMachines();
    //  var randomMachine = allMachines[UnityEngine.Random.Range(0, allMachines.Count)];

    //  MachineLevelData machine = new()
    //  {
    //    id = i == 0 ? statePlayer.machines[statePlayer.indexActiveMachine].name : randomMachine.name,
    //    gerbId = i == 0 ? statePlayer.gerbId : _gameSetting.gerbs[UnityEngine.Random.Range(0, _gameSetting.gerbs.Count - 1)].name,
    //    isBot = i != 0,
    //    name = i == 0 ? _gameManager.AppInfo.UserInfo.name : listRandomNames.ElementAt(i),
    //    rank = i == 0 ? _gameManager.StateManager.statePlayer.rank : UnityEngine.Random.Range(0, _gameSetting.ranks.Count - 1),
    //    data = i == 0 ? statePlayer.machines[statePlayer.indexActiveMachine].data : Helpers.GetStateMachinePlayerData(randomMachine),
    //    // colorsModify = i == 0 ? statePlayer.machines[statePlayer.indexActiveMachine].colorsModifies : new List<ColorsModify>(),
    //    // destroyedVoxels = i == 0 ? statePlayer.machines[statePlayer.indexActiveMachine].destroyVoxels : new List<Vector3Int>(),
    //  };

    //  stateLevel.machines.Add(machine);
    //}
  }

  public void InitDataCommandLevel()
  {
    stateLevel = new();

    for (int i = 0; i < _gameManager.LevelConfig.countTeam; i++)
    {
      TeamData team = new TeamData()
      {
        index = i,
        logo = i == 0 ? statePlayer.gerbId : _gameSetting.gerbs[UnityEngine.Random.Range(0, _gameSetting.gerbs.Count)].name,
      };

      // TODO

      stateLevel.teams.Add(team);
    }
  }

  public void RefreshData(bool saveDb)
  {

    OnChangeState?.Invoke(statePlayer);
  }

  public StatePlayer GetData()
  {
    return statePlayer;
  }

  public void Reset()
  {
    _gameManager.StateManager.statePlayer = new StatePlayer();
    _gameManager.StateManager.Init(null);

    OnChangeState?.Invoke(statePlayer);
  }

  public void BuyTower(TowerPartAsset configTower)
  {
    
  }

  public void BuyMachine(VehiclePresetAsset configMachine)
  {
    statePlayer.coin -= 1000;

    //statePlayer.machines.Add(new StateMachinePlayer()
    //{
    //  data = Helpers.GetStateMachinePlayerData(configMachine),
    //  name = configMachine.name
    //});

    OnChangeState?.Invoke(statePlayer);
    _gameManager.DataManager.Save(true);
  }

  public void SellMachine(StateMachinePlayer configMachine)
  {
    statePlayer.coin += 1000;

    StateMachinePlayer stateMachinePlayer = statePlayer.machines.Find(x => x.name == configMachine.name);

    if (stateMachinePlayer != null)
    {
      statePlayer.machines.Remove(stateMachinePlayer);

      statePlayer.indexActiveMachine = 0;

      OnChangeState?.Invoke(statePlayer);

      _gameManager.DataManager.Save(true);
    }
  }

  public void RepairMachine(int index, int cost)
  {
      statePlayer.coin -= cost;

      var configMachine = statePlayer.machines[index];
      
      foreach (var item in statePlayer.machines[index].data.dataDetails)
      {
        item.destroyVoxels.Clear();
      }

      OnChangeState?.Invoke(statePlayer);

      _gameManager.DataManager.Save(true);
  }

  /// <summary>
  /// Устанавливает индекс активной машины в гараже.
  /// </summary>
  /// <param name="index"></param>
  public int SetActiveMachine(int index)
  {
    index = Mathf.Clamp(index, 0, statePlayer.machines.Count - 1);

    return statePlayer.indexActiveMachine = index;
  }

  /// <summary>
  /// Красит выделенный цвет на машине в выбранный цвет.
  /// </summary>
  /// <param name="input"></param>
  public List<ColorsModify> OnFillMachine(FillData input)
  {

    StateMachinePlayer activeMachineData = statePlayer.machines[statePlayer.indexActiveMachine];

    // int indexForChange = -1;

    List<ColorsModify> newColorsModify = activeMachineData.data != null ?
      new List<ColorsModify>(activeMachineData.data.colorsModifies) :
      new List<ColorsModify>();

    Color32 color32 = input.voxelGroupData.color;

    newColorsModify.RemoveAll( x => HelperVoxel.AreColorsApproximatelyEqual(x.input, color32));

    newColorsModify.Add(new ColorsModify
    {
      input = color32,
      output = input.outputColor,
      typeEntity = TypeEntity.Vehicle
    });

    // bool isExist = false;
    // for (int i = 0; i < activeMachineData.colorsModifies.Count; i++)
    // {
    //   Color32 color32 = input.voxelGroupData.color;
    //   if (HelperVoxel.AreColorsApproximatelyEqual(activeMachineData.colorsModifies[i].input, color32))
    //   {
    //     isExist = true;
    //   } else
    //   {
    //     newColorsModify.Add(activeMachineData.colorsModifies[i]);
    //   }
    //   Debug.LogWarning($"Colors: {i} - {color32}/{activeMachineData.colorsModifies[i].input}");
    // }

    // if (isExist)
    // {
    //     Color32 color32 = input.voxelGroupData.color;
    //     newColorsModify.Add(new ColorsModify
    //     {
    //       input = color32,
    //       output = input.outputColor,
    //       typeEntity = TypeEntity.Machine
    //     });
    // }

    statePlayer.machines[statePlayer.indexActiveMachine].data.colorsModifies = newColorsModify;
    // if (indexForChange > -1)
    // {
    //   allColorsModify[indexForChange] = new ColorsModify()
    //   {
    //     input = input.voxelGroupData.color,
    //     output = input.inputColor,
    //     typeEntity = TypeEntity.Machine
    //   };
    // } else
    // {
    //   statePlayer.machines[statePlayer.indexActiveMachine].colorsModifies.Add(new ColorsModify()
    //   {
    //     input = input.voxelGroupData.color,
    //     output = input.inputColor,
    //     typeEntity = TypeEntity.Machine
    //   });
    // }

      Debug.LogWarning($"OnFillMachine: complete!");

      OnChangeState?.Invoke(statePlayer);

      _gameManager.DataManager.Save(true);

      return activeMachineData.data.colorsModifies;
  }

  public void SaveDestroyVoxelsMachine(List<RemoveVoxel> removeVoxels, DataDetail dataDetail)
  {
    StateMachinePlayer activeMachineData = statePlayer.machines[statePlayer.indexActiveMachine];

    if (activeMachineData != null)
    {
      int indexChangeItem = statePlayer.machines[statePlayer.indexActiveMachine].data.dataDetails.FindIndex(x => x.nameConfig == dataDetail.nameConfig && x.number == dataDetail.number);

      if (indexChangeItem > -1)
      {
        // if (statePlayer.machines[statePlayer.indexActiveMachine].data.dataDetails == null)
        // {
        //   statePlayer.machines[statePlayer.indexActiveMachine].data.destroyVoxels = new List<Vector3Int>();
        // }

        List<Vector3Int> vector3s = removeVoxels.Select(x => Vector3Int.RoundToInt(x.position)).ToList();

        var existDestroyVoxels = statePlayer.machines[statePlayer.indexActiveMachine].data.dataDetails[indexChangeItem].destroyVoxels;
        
        foreach (var item in vector3s)
        {
            if (!existDestroyVoxels.ContainsKey(item))
            {
              existDestroyVoxels.Add(item, TypeEntity.Vehicle);
            }
            else
            {
              // Handle the duplicate key case:
              // Option A: Log a warning (as shown here)
              Debug.LogWarning($"Skipping duplicate key: {item}");
              // Option B: Overwrite the existing value
              // targetDictionary[item.Key] = item.Value;
              // Option C: Throw an exception
              // throw new System.ArgumentException($"Duplicate key found: {item.Key}");
            }
        }
      }
    }

      OnChangeState?.Invoke(statePlayer);

      _gameManager.DataManager.Save(true);
  }
}