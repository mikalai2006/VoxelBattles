using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FileDataHandler
{
  private readonly string _dataDirPath;
  //   private readonly string _dataFileName;
  private readonly string _nameFile;
  private readonly string _nameFile2;
  private readonly bool _useEncryption = false;
  private readonly string _encryptionCodeWord = "word";

  public FileDataHandler(string dataDirPath, string _fileName, bool useEncryption)
  {
    this._dataDirPath = dataDirPath;
    this._useEncryption = useEncryption;
    this._nameFile = _fileName;
    this._nameFile2 = _fileName + "_2";
  }

  public async UniTask<StatePlayer> LoadData()
  {
    string fullPath = Path.Combine(_dataDirPath, _nameFile);
    string fullPath2 = Path.Combine(_dataDirPath, _nameFile);

    StatePlayer loadedData = null;

    if (File.Exists(fullPath))
    {
      try
      {
        string dataToLoad = "";

        using (FileStream stream = new FileStream(fullPath, FileMode.Open))
        {
          using (StreamReader reader = new StreamReader(stream))
          {
            dataToLoad = await reader.ReadToEndAsync();
          }
        }

        if (true) {
          var data = File.ReadAllBytes(fullPath2);

          dataToLoad = DecompressString(data);

          // Debug.Log("Decompress string data: " + dataToLoad);
        }
          
        loadedData = JsonUtility.FromJson<StatePlayer>(dataToLoad);

        if (_useEncryption)
        {
          dataToLoad = EncryptDecrypt(dataToLoad);
        }

      }
      catch (Exception e)
      {
        Debug.LogError("Error Load file::: " + fullPath + "\n" + e);
      }
    }

    return loadedData;
  }

  public void SaveData(StatePlayer data)
  {
    string fullPath = Path.Combine(_dataDirPath, _nameFile);
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

      string dataToStore = JsonUtility.ToJson(data);

      if (_useEncryption)
      {
        dataToStore = EncryptDecrypt(dataToStore);
      }

      using (FileStream stream = new FileStream(fullPath, FileMode.Create))
      {
        using (StreamWriter writer = new StreamWriter(stream))
        {
          writer.Write(dataToStore);
        }
      }
    }
    catch (Exception e)
    {
      Debug.LogError("Error Save file::: " + fullPath + "\n" + e);
    }

    string fullPath2 = Path.Combine(_dataDirPath, _nameFile2);
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(fullPath2));

      string dataToStore = JsonUtility.ToJson(data);

      // if (true)
      // {
      //   dataToStore = CompressString(dataToStore);
      // }
      var compressDataToStore = CompressString(dataToStore);
      File.WriteAllBytes(fullPath2, compressDataToStore);
      // using (FileStream stream = new FileStream(fullPath2, FileMode.Create))
      // {
      //   using (StreamWriter writer = new StreamWriter(stream))
      //   {
      //     writer.Write(compressDataToStore);
      //   }
      // }
    }
    catch (Exception e)
    {
      Debug.LogError("Error Save file::: " + fullPath2 + "\n" + e);
    }

  }

  private string EncryptDecrypt(string data)
  {
    string modifierData = "";

    for (int i = 0; i < data.Length; i++)
    {
      modifierData += (char)(data[i] ^ _encryptionCodeWord[i % _encryptionCodeWord.Length]);
    }

    return modifierData;
  }
  
    // Helper method to compress a string using GZip
    private byte[] CompressString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }
    }

    // Helper method to decompress a GZip byte array into a string
    private string DecompressString(byte[] bytes)
    {
        string jsonString = Encoding.UTF8.GetString(bytes);

        return jsonString;

        // using (var memoryStream = new MemoryStream(bytes))
        // {
        //     using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
        //     {
        //         using (var streamReader = new StreamReader(gzipStream, Encoding.UTF8))
        //         {
        //             return streamReader.ReadToEnd();
        //         }
        //     }
        // }
    }
}
