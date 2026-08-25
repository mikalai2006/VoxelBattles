using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;

using Random = UnityEngine.Random;


/// <summary>
/// A static class for general helpful methods
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Находит колесо с самым большим Bounds.y (высотой) среди всех настроенных слотов шасси.
    /// </summary>
    public static Vector3 GetMaxWheelBounds(VehiclePresetWheelAsset wheelsPreset)
    {
        Vector3 maxBounds = Vector3.zero;

        if (wheelsPreset == null)
            return maxBounds;

        int slotCount = wheelsPreset.wheelSlots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            WheelPartAsset wheel = wheelsPreset.wheelSlots[i].wheelPartAsset;

            // ИСПРАВЛЕНО: Убрана ошибочная проверка структуры meshConfig != null
            // Мы сразу безопасно проверяем ссылку на Scriptable Object вокселей внутри конфигурации
            if (wheel != null && wheel.meshConfig.sOVoxelData != null)
            {
                Vector3 currentBounds = wheel.meshConfig.sOVoxelData.Bounds;

                // Если текущее колесо выше, чем ранее найденное — запоминаем его
                if (currentBounds.y > maxBounds.y)
                {
                    maxBounds.y = currentBounds.y;
                }

                //if (currentBounds.y > maxBounds.y)
                //{
                //    maxBounds.y = currentBounds.y;
                //}
            }
        }

        return maxBounds;
    }



    /// <summary>
    /// Определяет лежит ли число в диапазоне
    /// <typeparam name="max">Максимальное значение диапазона</typeparam>
    /// <code>
    /// if (IsBetween(0, 1, 0.5)) {
    /// return true;
    /// }
    /// </code>
    /// </summary>
    public static bool IsBetween(float min, float max, float value)
  {
    return value >= min && value <= max;
  }

  /// <summary>
  /// Destroy all child objects of this transform (Unintentionally evil sounding).
  /// Use it like so:
  /// <code>
  /// transform.DestroyChildren();
  /// </code>
  /// </summary>
  public static void DestroyChildren(this Transform t)
  {
    foreach (Transform child in t) UnityEngine.Object.Destroy(child.gameObject);
  }

  // public static int GenerateChance(int start = 0, int end = 100)
  // {
  //   System.Random random = new System.Random();
  //   return random.Next(0, 100);
  // }

  public static Dictionary<Transform, dynamic> GetChildrenHierarchy(this GameObject gameobject)
  {
    Dictionary<Transform, dynamic> children = new Dictionary<Transform, dynamic>();

    foreach (Transform child in gameobject.transform)
    {
      children.Add(child, GetChildrenHierarchy(child.gameObject));
    }

    return children;
  }

  /// <summary>
  /// Clone Dictionary
  /// </summary>
  /// <param name="original"></param>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  /// <returns></returns>
  public static Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue>
 (Dictionary<TKey, TValue> original) where TValue : ICloneable
  {
    Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                            original.Comparer);
    foreach (KeyValuePair<TKey, TValue> entry in original)
    {
      ret.Add(entry.Key, (TValue)entry.Value.Clone());
    }
    return ret;
  }


  public static void LineConnection(LineRenderer lr, GameObject first, GameObject second, float linewidth)
  {
    // var lr = first.GetComponent<LineRenderer>();
    lr.SetPosition(0, first.gameObject.transform.position);
    lr.SetPosition(1, second.gameObject.transform.position);
    lr.startWidth = linewidth;
    lr.endWidth = linewidth;
  }
  public static string GetColorString(string str)
  {
    return " <color=#FFFFAB>" + str + "</color>";
  }


  public async static UniTask<string> GetLocaledString(LocalizedString localizedString)
  {
    var t = localizedString.GetLocalizedStringAsync();
    await t.Task;
    return t.Result;
  }


  public async static UniTask<string> GetLocaledString(string key)
  {
    var t = new LocalizedString(ConstantsApp.LanguageTable.LANG_TABLE_LOCALIZE, key).GetLocalizedStringAsync();
    await t.Task;
    return t.Result;
  }


  public static IEnumerable<T> IntersectWithRepetitons<T>(this IEnumerable<T> first,
    IEnumerable<T> second)
  {
    var lookup = second.GroupBy(x => x).ToDictionary(group => group.Key, group => group.Count());
    foreach (var item in first)
      if (lookup.ContainsKey(item) && lookup[item] > 0)
      {
        yield return item;
        lookup[item]--;
      }
  }

  public async static UniTask<string> GetLocalizedPluralString<T>(string key, Dictionary<string, T> data)
  {
    var localizedString = new LocalizedString(ConstantsApp.LanguageTable.LANG_TABLE_LOCALIZE, key);
    var args = new[] { data };
    localizedString.Arguments = args;
    var t = localizedString.GetLocalizedStringAsync(data);
    await t.Task;
    return t.Result;
  }


  public async static UniTask<string> GetLocalizedPluralString<T>(
        LocalizedString localizedString,
        Dictionary<string, T>[] args,
        Dictionary<string, T> dictionary
        )
  {
    if (localizedString.IsEmpty) return "NO_LANG";

    localizedString.Arguments = args;
    var t = localizedString.GetLocalizedStringAsync(dictionary);
    await t.Task;
    return t.Result;
  }

  public static bool HasValueDouble(this double value)
  {
    return !Double.IsNaN(value) && !Double.IsInfinity(value);
  }

  // public async static UniTask<string> GetPlayPrefKey()
  // {
  //   await LocalizationSettings.InitializationOperation.Task;

  //   return string.Format("{0}_{1}",
  //     GameManager.Instance.namePlayPref,
  //     LocalizationSettings.SelectedLocale.Identifier.Code
  //   );
  // }


  public static string StripHTML(string input)
  {
    return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", String.Empty);
  }


  public static async UniTask<Texture2D> LoadTexture(string path)
  {
    Texture2D result = null;

    if (string.IsNullOrEmpty(path)) return result;

    if (Application.internetReachability == NetworkReachability.NotReachable) return result;

    UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(path);

    try
    {
      await webRequest.SendWebRequest();

      Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
      result = texture;
    }
    catch (System.Exception error)
    {
      Debug.Log(error);
      return result;
    }


    // if (webRequest.result == UnityWebRequest.Result.ConnectionError
    //   || webRequest.result == UnityWebRequest.Result.DataProcessingError
    //   || webRequest.result == UnityWebRequest.Result.ProtocolError)
    // {
    //   Debug.Log(webRequest.error);
    //   return result;
    // }
    // else
    // {
    //   Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
    //   result = texture;
    // }

    return result;
  }

  public static async UniTask<string> GetName()
  {
    var _gameManager = GameManager.Instance;

    string userName = string.IsNullOrEmpty(_gameManager.AppInfo.UserInfo.name)
  ? await Helpers.GetLocaledString(_gameManager.Settings.noName.title)
  : _gameManager.AppInfo.UserInfo.name;
    return userName;
  }
  // public static Dictionary<string, List<string>> GetDictionaryCompleteLevel(List<string> list)
  // {
  //   Dictionary<string, List<string>> result = new();

  //   foreach (var item in list)
  //   {
  //     var splitString = item.Split(":");
  //     if (!result.ContainsKey(splitString[0]))
  //     {
  //       result[splitString[0]] = new List<string>() {
  //         splitString[1]
  //       };
  //     }
  //     else
  //     {
  //       result[splitString[0]].Add(splitString[1]);
  //     }
  //   }

  //   return result;
  // }
  /// <summary>
  ///  Get the probability of getting an item
  /// </summary>
  /// <param name="item">Items for search</param>
  /// <returns>result item or first item<T></returns>
  public static ResultProbabiliti<T> GetProbabilityItem<T>(List<ItemProbabiliti<T>> items)
  {
    double p = new System.Random().NextDouble();
    double accumulator = 0.0;
    var result = new ResultProbabiliti<T>()
    {
      Item = items[0].Item,
      index = 0
    };
    for (int i = 0; i < items.Count; i++)
    {
      ItemProbabiliti<T> item = items[i];
      accumulator += item.probability;
      if (p <= accumulator)
      {
        result.Item = item.Item;
        result.index = i;
        break;
      }
    }
    return result;
  }

  /// <summary>
  /// Возвращает вероятность события, как булево значение, принимая долю происхождения
  /// </summary>
  /// <returns></returns>
  public static bool GetChance(float chance)
    {
        float randomValue = Random.Range(0f, 1f); // Сгенерировать случайное число от 0 до 1

        if (randomValue <= chance)
        {
            return true;
        }
        return false;
    }
  
  /// <summary>
  /// Преобразование 3D-координат (x, y, z) в 1D индекс
  /// </summary>
  /// <param name="row">Значение строки в 3-х-мерном массиве</param>
  /// <param name="depth">Значение глубины в 3-х-мерном массива</param>
  /// <param name="col">Значение столбца в 3-х-мерном массиве</param>
  /// <param name="rowsCount">Количество строк в 3-х-мерном массиве</param>
  /// <param name="depthMax">Глубина 3-х-мерного массива</param>
  /// <param name="colsCount">Количество столбцов в 3-х-мерном массиве</param>
  /// <returns>1D index calulation</returns>
  public static int From3DTo1D(int row, int depth, int col, Vector3Int size)
  {
    //return (z * xMax * yMax) + (y * xMax) + x;

    // Пример преобразования 3D-координат (d, r, c) в 1D индекс
    // int d = 0; // Индекс глубины
    // int r = 1; // Индекс строки
    // int c = 2; // Индекс столбца

    return (depth * size.x * size.z) + (row * size.z) + col;
  }

  /// <summary>
  /// Преобразование 1D индекс в 3D-координаты (x, y, z)
  /// </summary>
  /// <param name="row">Значение строки в 3-х-мерном массиве</param>
  /// <param name="depth">Значение глубины в 3-х-мерном массива</param>
  /// <param name="col">Значение столбца в 3-х-мерном массиве</param>
  /// <param name="rowsCount">Количество строк в 3-х-мерном массиве</param>
  /// <param name="depthMax">Глубина 3-х-мерного массива</param>
  /// <param name="colsCount">Количество столбцов в 3-х-мерном массиве</param>
  /// <returns>1D index calulation</returns>
  public static Vector3Int From1DTo3D(int index, int rowsCount, int depthCount, int colsCount)
  {
    // Рассчитайте координаты: Используйте формулу для преобразования одномерного индекса (например, oneDIndex) 
    // в 3D индексы x, y, z:
    // x = oneDIndex / (dimY * dimZ)
    // y = (oneDIndex % (dimY * dimZ)) / dimZ
    // z = oneDIndex % dimZ

    return new Vector3Int(
      index / (depthCount * colsCount),
      (index % (depthCount * colsCount)) / colsCount,
      index % colsCount
    );
  }

  /// <summary>
  /// Takes 3D indexes and returns a 1D index based on them
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <param name="z"></param>
  /// <param name="xMax"></param>
  /// <param name="yMax"></param>
  /// <returns>1D index calulation</returns>
  public static int To1D(int x, int y, int z, int xMax, int yMax)
  {
    //return (z * xMax * yMax) + (y * xMax) + x;
    return x + xMax * (y + yMax * z);
  }

  /// <summary>
  /// Takes 2D indexes and returns a 1D index based on them
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <returns>1D index calulation</returns>
  public static int To1D(int x, int y, int width)
  {
    return y * width + x;
  }

  /// <summary>
  /// Переводит индекс элемента массива из одномерного в двумерный
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <returns>1D index calulation</returns>
  public static Vector2Int From1DTo2D(int index, int width)
  {
    var x = (int) Math.Floor((decimal)index / width);
    var y = index % width;
    return new Vector2Int(x, y);
  }

  /// <summary>
  /// Takes 1D index and returns 3D indexes based on it
  /// </summary>
  /// <param name="index"></param>
  /// <param name="xMax"></param>
  /// <param name="yMax"></param>
  /// <returns></returns>
  public static Vector3Int To3D(int index, int xMax, int yMax)
  {
    int z = index / (xMax * yMax);
    int idx = index - (z * xMax * yMax);
    int y = idx / xMax;
    int x = idx % xMax;
    return new Vector3Int(x, y, z);
  }

/// <summary>
/// округление координат вектора
/// </summary>
/// <param name="vector"></param>
/// <returns></returns>
  public static Vector3Int RoundVector3(Vector3 vector)
  {
    return new Vector3Int(
        Mathf.RoundToInt(vector.x),
        Mathf.RoundToInt(vector.y),
        Mathf.RoundToInt(vector.z)
    );
  }
  
  // Helper function to check if a point is inside a sphere
  public static bool IsInsideSphere(Vector3 point, Vector3 sphereCenter, float sphereRadius)
  {
      return (point - sphereCenter).sqrMagnitude <= (sphereRadius * sphereRadius); //Vector3.Distance(point, sphereCenter) <= sphereRadius;
  }
  // Helper function to check if a point is inside a sphere and on the border
  public static bool IsInsideSphereBorder(Vector3 point, Vector3 sphereCenter, float sphereRadius, float innerRadius)
  {
      return Vector3.Distance(point, sphereCenter) <= sphereRadius && Vector3.Distance(point, sphereCenter) > innerRadius;
  }

  // /// <summary>
  // /// Определяет находится ли точка внутри меш коллайдера.
  // /// </summary>
  // /// <param name="point"></param>
  // /// <param name="meshCollider"></param>
  // /// <returns></returns>
  // static public bool IsPointInMeshColliderClosestPoint(Vector3 point, MeshCollider meshCollider)
  // {
  //     // Get the closest point on the collider to the given point.
  //     Vector3 closestPoint = meshCollider.ClosestPoint(point);

  //     // Check if the test point is inside the collider by comparing its distance to the closest point.
  //     // If the distance is very small, the point is inside.
  //     return (Vector3.Distance(point, closestPoint) < 0.001f);
  // }

  public static List<Vector2> GenerateArchimedeanSpiral(double radiusIncrement, int numPoints)
    {
        var points = new List<Vector2>();
        double currentRadius = 0;
        double angle = 0;
        double angleIncrement = 0.1; // Шаг по углу (чем меньше, тем глаже спираль)

        for (int i = 0; i < numPoints; i++)
        {
            // Расчет радиуса для текущего шага
            currentRadius = radiusIncrement * angle;

            // Перевод в декартовы координаты
            double x = currentRadius * Math.Cos(angle);
            double y = currentRadius * Math.Sin(angle);

            points.Add(new Vector2((float)x, (float)y));

            // Увеличение угла для следующей точки
            angle += angleIncrement;
        }
        return points;
    }

/// <summary>
/// Возвращает кол-во вокселей из конфига машины.
/// </summary>
/// <param name="gameMachine"></param>
/// <returns></returns>
    public static int GetCountVoxels(VehicleAssembler gameMachine)
    {
        int countVoxels = 0; // gameMachine.body.Config.MeshConfig.sOVoxelData.countVoxels;

      //foreach (var towerOption in gameMachine.towers)
      //{
      //  countVoxels += towerOption.Config.MeshConfig.sOVoxelData.countVoxels;
        
      //  foreach (var muzzleOption in towerOption.muzzles)
      //  {
      //    countVoxels += muzzleOption.Config.MeshConfig.sOVoxelData.countVoxels;
      //  } 
      //}
      
      //foreach (var caterpillarOption in gameMachine.catterpillars)
      //{
      //  countVoxels += caterpillarOption.Config.MeshConfig.sOVoxelData.countVoxels;
      //} 

      //foreach (var wheelOption in gameMachine.wheels)
      //{
      //  countVoxels += wheelOption.Config.MeshConfig.sOVoxelData.countVoxels;
      //} 

      return countVoxels;
    }

  ///// <summary>
  ///// Используя конфигурацию машины, возвращает данные для спавна.
  ///// </summary>
  ///// <param name="configMachine"></param>
  //public static StateMachinePlayerData GetStateMachinePlayerData(VehiclePartAsset configMachine)
  //{
  //   var data = new StateMachinePlayerData();

  //  // init data body.
  //  data.dataDetails.Add(new DataDetail()
  //  {
  //    nameConfig = configMachine.body.Config.name,
  //    offset = configMachine.body.offsetBody,
  //    type = VehicleDetailType.Body,
  //    number = 0
  //  });

  //  // init data caterpillars.
  //  for (int i = 0; i < configMachine.catterpillars.Count; i++)
  //  {
  //    var item = configMachine.catterpillars[i];
  //    var newData = new DataDetail()
  //    {
  //      nameConfig = item.Config.name,
  //      offset = item.offsetCat,
  //      type = VehicleDetailType.Caterpillar,
  //      number = i
  //    };
  //    data.dataDetails.Add(newData);
  //  }

  //  // init data wheels.
  //  for (int i = 0; i < configMachine.wheels.Count; i++)
  //  {
  //    var item = configMachine.wheels[i];
  //    data.dataDetails.Add(new DataDetail()
  //    {
  //      nameConfig = item.Config.name,
  //      offset = item.offsetWheel,
  //      type = VehicleDetailType.Wheel,
  //      number = i
  //    });
  //  }

  //  // init data towers.
  //  for (int i = 0; i < configMachine.towers.Count; i++)
  //  {
  //    var item = configMachine.towers.ElementAt(i);
  //    var uid = System.Guid.NewGuid().ToString();
  //    data.dataDetails.Add(new DataDetail()
  //    {
  //      nameConfig = item.Config.name,
  //      number = i,
  //      offset = item.offsetTower,
  //      type = VehicleDetailType.Tower,
  //      ido = string.IsNullOrEmpty(item.ido) ? uid : item.ido,
  //      parentId = item.parentId,
  //    });

  //    // init data muzzles.
  //    for (int j = 0; j < item.muzzles.Count; j++)
  //    {
  //      var itemMuzzle = item.muzzles.ElementAt(j);
  //      data.dataDetails.Add(new DataDetail()
  //      {
  //        nameConfig = itemMuzzle.Config.name,
  //        number = j,
  //        offset = itemMuzzle.offsetMuzzle,
  //        type = VehicleDetailType.Muzzle,
  //        parentId = string.IsNullOrEmpty(item.ido) ? uid : item.ido
  //      });
  //    }
  //  }

  //  return data;
  //}


  // /// <summary>
  // /// Создает игровой объект машины и возвращает его.
  // /// </summary>
  // /// <param name="configMachine"></param>
  // /// <param name="stateMachinePlayer"></param>
  // /// <returns></returns>
  // public static BaseMachine CreateMachine(GameMachine configMachine, StateMachinePlayerData stateMachinePlayerData, Transform parent)
  // {
    

  // }

    // /// <summary>
    // /// Добавление списка элементов в словарь аналогичных элементов.
    // /// </summary>
    // /// <typeparam name="TKey"></typeparam>
    // /// <typeparam name="TValue"></typeparam>
    // /// <param name="targetDictionary"></param>
    // /// <param name="sourceCollection"></param>
    // public static void AddRange<TKey, TValue>(
    // Dictionary<TKey, TValue> targetDictionary, 
    // IEnumerable<KeyValuePair<TKey, TValue>> sourceCollection)
    // {
    //   foreach (var item in sourceCollection)
    //   {
    //       if (!targetDictionary.ContainsKey(item.Key))
    //       {
    //         targetDictionary.Add(item.Key, item.Value);
    //       }
    //       else
    //       {
    //         // Handle the duplicate key case:
    //         // Option A: Log a warning (as shown here)
    //         Debug.LogWarning($"Skipping duplicate key: {item.Key}");
    //         // Option B: Overwrite the existing value
    //         // targetDictionary[item.Key] = item.Value;
    //         // Option C: Throw an exception
    //         // throw new System.ArgumentException($"Duplicate key found: {item.Key}");
    //       }
    //   }
    // }
    public static byte[] StructToBytes<T>(T structure) where T : struct {
      int size = Marshal.SizeOf(structure);
      byte[] arr = new byte[size];
      IntPtr ptr = Marshal.AllocHGlobal(size);

      Marshal.StructureToPtr(structure, ptr, true);
      Marshal.Copy(ptr, arr, 0, size);
      Marshal.FreeHGlobal(ptr);

      return arr;
  }

}




[System.Serializable]
public struct ItemProbabiliti<T>
{
  public T Item;
  [Range(0, 1)] public double probability;
}

[System.Serializable]
public struct ResultProbabiliti<T>
{
  public T Item;
  public int index;
}
