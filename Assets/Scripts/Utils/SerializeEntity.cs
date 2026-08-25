using Mikalai2006.VoxelBase;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public class SerializeEntity : Dictionary<Vector3Int, TypeEntity>, ISerializationCallbackReceiver
{
  [SerializeField] private List<string> values = new List<string>();
  [System.NonSerialized] private List<TypeEntity> keys = new List<TypeEntity>();

  public void OnBeforeSerialize()
  {
    keys.Clear();
    values.Clear();
    foreach (KeyValuePair<Vector3Int, TypeEntity> pair in this)
    {
      values.Add($"{pair.Key.x}:{pair.Key.y}:{pair.Key.z}:{(int)pair.Value}");
      keys.Add(pair.Value);
    }
  }

  public void OnAfterDeserialize()
  {
    this.Clear();

    // if (keys.Count != values.Count)
    // {
    //   Debug.LogWarning($"Tried to deserialize s Dictionary, but the amount of keys " +
    //       $"{keys.Count} does not match the number of values {values.Count} [{this.ToString()}]");
    // }

    for (int i = 0; i < values.Count; i++)
    {
      // this.Add(keys[i], values[i]);
      string[] arStr = values[i].Split(":");
      this.Add(new Vector3Int(int.Parse(arStr[0]), int.Parse(arStr[1]), int.Parse(arStr[2])), (TypeEntity)int.Parse(arStr[3]));
    }
  }

}