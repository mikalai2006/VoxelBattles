using System.Collections.Generic;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public class ParserHeight : MonoBehaviour
    {
        public TextureHeightMapperSettings _settings;
        public Dictionary<Vector2Int, int> heightMap;
        // public Dictionary<Vector2Int, Color> data;
        public Vector2Int gridSize;

        public void SetConfig(TextureHeightMapperSettings settings)
        {
            _settings = settings;
        }

        public void Init()
        {
            heightMap = new Dictionary<Vector2Int, int>();

            gridSize = new Vector2Int(_settings.texture.width, _settings.texture.height);
            
            _settings.nameMap = _settings.texture.name;

            GenerateHeightMap();
        }

        // void Start()
        // {
        //     GenerateHeightMap();
        // }

        public Dictionary<Vector2Int, int> GenerateHeightMap()
        {

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var color = _settings.texture.GetPixel(x, y);

                    var position = new Vector2Int(x, y);

                    // var height = Mathf.RoundToInt(color.r * _settings.heightSize);
                    var height = Mathf.RoundToInt(color.r * _settings.heightSize);
                    heightMap[position] = Mathf.RoundToInt(height);
                    // Debug.Log($"pos: {position}, height: {heightMap[position]}[{color.r}]");
                }
            }

            return heightMap;
        }

    }

    [System.Serializable]
    public class TextureHeightMapperSettings
    {
        public int heightSize;
        public Texture2D texture;
        public string nameMap;
    }
}