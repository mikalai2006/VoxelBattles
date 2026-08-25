using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

public class ResourceSystem : StaticInstance<ResourceSystem>
{
  public Dictionary<string, List<Object>> ResourceAssets = new Dictionary<string, List<Object>>();

  #region Asset load and destroy
  public async UniTask<List<T>> LoadCollectionsAsset<T>(string assetNameOrLabel)
     where T : Object
  {
    List<T> createdObjs = new List<T>();
    var locations = await Addressables.LoadResourceLocationsAsync(assetNameOrLabel).Task;

    await CreateAssetsThenUpdateCollection<T>(locations, createdObjs);

    List<Object> list = new List<Object>();
    foreach (var asset in createdObjs)
    {
      // Debug.Log($"list name = {asset.GetType()}");
      list.Add(asset);
    }
    if (!ResourceAssets.ContainsKey(assetNameOrLabel))
    {
      ResourceAssets.Add(assetNameOrLabel, list);
      //Debug.Log($"Load {assetNameOrLabel}::: {list.Count}");

    }
    else
    {
      Debug.LogWarning($" {assetNameOrLabel} is exists");

    }
    return createdObjs;
  }


  private async UniTask CreateAssetsThenUpdateCollection<T>(IList<IResourceLocation> locations, List<T> createdObjs)
      where T : Object
  {
    foreach (var location in locations)
    {
      if (location.ResourceType.IsSubclassOf(typeof(T)) || location.ResourceType == typeof(T))
      {
        var output = await Addressables.LoadAssetAsync<T>(location).Task as T;
        // Debug.Log($"ResourceType={location.ResourceType}, typeof(T)({typeof(T)})[{location.ResourceType is T}]");
        // Debug.Log($"IsSubclassOf = {location.ResourceType.IsSubclassOf(typeof(T))}");
        // Debug.Log($"output is T[{output is T}]");
        createdObjs.Add(output);
      }
    }
  }

  public void DestroyAssets()
  {
    foreach (var asset in ResourceAssets)
    {
      if (asset.Value.Count > 0)
      {
        foreach (var item in asset.Value)
        {
          if (item != null) Addressables.Release(item);
        }
      }
    }
  }

  public void DestroyAssetsByLabel(string label)
  {
    ResourceAssets.TryGetValue(label, out List<Object> list);
    foreach (var asset in list)
    {
      if (asset != null) Addressables.Release(asset);
    }
  }
  public void DestroyAsset(Object asset)
  {
    if (asset != null) Addressables.Release(asset);
  }
  #endregion

  public List<T> GetAllAssetsByLabel<T>(string label)
     where T : Object
  {
    var output = new List<T>();
    ResourceAssets.TryGetValue(label, out List<Object> list);
    for (int i = 0; i < list.Count; i++)
    {
      var item = list[i] as T;
      output.Add(item);
    }

    //Debug.Log($"GetAllAssetsByLabel::: label={label}, values={ttt.Count}");
    return output;
  }

//   #region managers entity
//   public List<GameEntity> GetAllEntity()
//   {
//     return GetAllAssetsByLabel<GameEntity>(ConstantsApp.Labels.LABEL_ENTITY);
//   }
//   #endregion

//   #region managers bonus
//   public List<GameBonus> GetAllBonus()
//   {
//     return GetAllAssetsByLabel<GameBonus>(ConstantsApp.Labels.LABEL_BONUS).Where(t => !t.disable).ToList();
//   }
//   #endregion

  #region managers
  public List<GameTheme> GetAllTheme()
  {
    return GetAllAssetsByLabel<GameTheme>(ConstantsApp.Labels.LABEL_THEME);
  }
  //public List<GameTowerShop> GetAllShopTower()
  //{
  //  return GetAllAssetsByLabel<GameTowerShop>(ConstantsApp.Labels.SHOP_TOWER);
  //}
  //public List<GameTower> GetAllTower()
  //{
  //  return GetAllAssetsByLabel<GameTower>(ConstantsApp.Labels.TOWER);
  //}
  //public List<GameMuzzle> GetAllMuzzles()
  //{
  //  return GetAllAssetsByLabel<GameMuzzle>(ConstantsApp.Labels.MUZZLE);
  //}
  //public List<GameCaterpillar> GetAllCaterpillar()
  //{
  //  return GetAllAssetsByLabel<GameCaterpillar>(ConstantsApp.Labels.CATERPILLAR);
  //}
  //public List<GameWheel> GetAllWheel()
  //{
  //  return GetAllAssetsByLabel<GameWheel>(ConstantsApp.Labels.WHEEL);
  //}
  //public List<GameBody> GetAllBody()
  //{
  //  return GetAllAssetsByLabel<GameBody>(ConstantsApp.Labels.BODY);
  //}
  //public List<GameMachine> GetAllMachines()
  //{
  //  return GetAllAssetsByLabel<GameMachine>(ConstantsApp.Labels.MACHINE);
  //}
  #endregion
}