using UnityEngine;
using Mikalai2006.VoxelBase;
using Mikalai2006.VoxelMap;
using System;
using System.Collections.Generic;

public class VoxelTextureGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private WFCManager wfcManager;

    [Header("Настройки выходной текстуры")]
    [Tooltip("Ширина карты в чанках (каждый чанк nхn пикселей)")]
    [SerializeField] private int worldSizeInChunksX = 3; // 3 * 8 = 24 пикселя
    [Tooltip("Высота карты в чанках (каждый чанк nхn пикселей)")]
    [SerializeField] private int worldSizeInChunksY = 4; // 4 * 8 = 32 пикселя

    private const int CHUNK_SIZE = 8;

    [Header("Входные шаблоны чанков (Картинки 8x8)")]
    [Tooltip("Текстуры nхn, где R = ID сущности (TypeEntity), G = Высота")]
    [SerializeField] private Texture2D[] chunkTemplates;

    [Header("Ограничения генерации")]
    [Tooltip("Сколько рядов чанков СНИЗУ нужно оставить абсолютно пустыми?")]
    [SerializeField] private int ignoreBottomChunkRows = 1; // 1 ряд чанков = 8 пикселей снизу


    [Header("Превью общей карты")]
    public Texture2D debugGlobalMapTexture;

    // Динамический словарь: под каждую сущность текстура создается сама
    private Dictionary<TypeEntity, Texture2D> outputTextures = new Dictionary<TypeEntity, Texture2D>();

    public void GenerateWorldTexture(int seed)
    {
        System.Random prng = new System.Random(seed);

        if (chunkTemplates == null || chunkTemplates.Length == 0)
        {
            Debug.LogError("[VoxelBase] Нет картинок-шаблонов nхn для сборки карты!");
            return;
        }

        int finalWidth = worldSizeInChunksX * CHUNK_SIZE;
        int finalHeight = worldSizeInChunksY * CHUNK_SIZE;

        // 1. Динамически инициализируем текстуры под ВСЕ типы из вашего enum
        InitTextures(finalWidth, finalHeight);

        // 2. Сборка карты из случайных чанков
        for (int chunkX = 0; chunkX < worldSizeInChunksX; chunkX++)
        {
            for (int chunkY = 0; chunkY < worldSizeInChunksY; chunkY++)
            {
                if (chunkY < ignoreBottomChunkRows) continue;
                // Выбираем случайный чанк через System.Random (гарантия работы сида)
                Texture2D randomChunk = chunkTemplates[prng.Next(chunkTemplates.Length)];
                Color32[] chunkPixels = randomChunk.GetPixels32();

                int startPixelX = chunkX * CHUNK_SIZE;
                int startPixelY = chunkY * CHUNK_SIZE;

                // Перенос пикселей на глобальное полотно
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        int chunkIndex = z * CHUNK_SIZE + x;
                        Color32 pixelColor = chunkPixels[chunkIndex];

                        // Извлекаем тип сущности из R-канала
                        TypeEntity entityType = (TypeEntity)pixelColor.r;


                        if (pixelColor.r > 0 && entityType == TypeEntity.None)
                        {
                            Debug.LogWarning($"[VoxelBase] Обнаружен пиксель с R={pixelColor.r}, но он распознался как None! Проверьте Compression = None в настройках картинки {randomChunk.name}.");
                        }

                        int globalX = startPixelX + x;
                        int globalY = startPixelY + z;

                        // 1. Проверяем, является ли текущая ячейка физическим левым, правым или верхним краем карты
                        bool isLeftEdge = (chunkX == 0 && x == 0);
                        bool isRightEdge = (chunkX == (worldSizeInChunksX - 1) && x == (CHUNK_SIZE - 1));
                        bool isTopEdge = (chunkY == (worldSizeInChunksY - 1) && z == (CHUNK_SIZE - 1));

                        // 2. Если пиксель попал строго на границу периметра — принудительно делаем его пещерой
                        if (isLeftEdge || isRightEdge || isTopEdge)
                        {
                            entityType = TypeEntity.Cave;
                            pixelColor.r = (byte)TypeEntity.Cave;
                            pixelColor.g = 255; // Высота пещеры (от 0 до 255)
                        }

                        // Отрисовка на дебаг-карте для инспектора
                        debugGlobalMapTexture.SetPixel(globalX, globalY, pixelColor);

                        // Универсальный сплиттер: если пиксель принадлежит сущности, 
                        // записываем его высоту (G-канал) в соответствующую монохромную текстуру
                        if (entityType != TypeEntity.None && outputTextures.ContainsKey(entityType))
                        {
                            // Сохраняем чистую высоту (G) в виде белого/серого пикселя нужного слоя
                            float heightValue = pixelColor.g / 255f;
                            Color heightColor = new Color(heightValue, heightValue, heightValue, 1f);

                            outputTextures[entityType].SetPixel(globalX, globalY, heightColor);
                        }
                    }
                }
            }
        }

        // 3. Применяем изменения для всех созданных текстур
        debugGlobalMapTexture.Apply();
        foreach (var tex in outputTextures.Values)
        {
            tex.Apply();
        }

        // 4. Универсальная отправка данных в WFCManager
        SendToWFCManager();
    }

    private void InitTextures(int width, int height)
    {
        outputTextures.Clear();

        // Создаем дебаг-текстуру
        debugGlobalMapTexture = CreateEmptyTexture(width, height);

        // Рефлексия находит абсолютно все элементы вашего TypeEntity автоматически
        foreach (TypeEntity type in Enum.GetValues(typeof(TypeEntity)))
        {
            if (type == TypeEntity.None) continue;

            // Создаем чистую текстуру под каждый тип сущности
            outputTextures[type] = CreateEmptyTexture(width, height);
        }
    }

    private Texture2D CreateEmptyTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Заполняем прозрачным цветом
        Color32[] clearPixels = new Color32[width * height];
        tex.SetPixels32(clearPixels);
        return tex;
    }

    private void SendToWFCManager()
    {
        if (wfcManager == null) return;

        // Полностью автоматическая привязка к WFCManager, если типы совпали
        if (outputTextures.ContainsKey(TypeEntity.Cave) && wfcManager.settingMapCaves != null)
            wfcManager.settingMapCaves.texture = outputTextures[TypeEntity.Cave];

        if (outputTextures.ContainsKey(TypeEntity.Zabor) && wfcManager.settingMapZabor != null)
            wfcManager.settingMapZabor.texture = outputTextures[TypeEntity.Zabor];

        if (outputTextures.ContainsKey(TypeEntity.House) && wfcManager.settingMapHouses != null)
            wfcManager.settingMapHouses.texture = outputTextures[TypeEntity.House];

        if (outputTextures.ContainsKey(TypeEntity.Tree) && wfcManager.settingMapTrees != null)
            wfcManager.settingMapTrees.texture = outputTextures[TypeEntity.Tree];

        // Примечание: для новых сущностей (например, Machine, Gun), которых еще нет в полях 
        // WFCManager, текстуры уже лежат готовыми в словаре `outputTextures`. 
        // Как только вы добавите их поля в WFCManager, просто допишите сюда строчку связи.
    }

    // Публичный геттер, чтобы любой другой скрипт мог забрать текстуру любой сущности
    public Texture2D GetTextureForEntity(TypeEntity type)
    {
        if (outputTextures.TryGetValue(type, out Texture2D tex))
        {
            return tex;
        }
        return null;
    }
}
