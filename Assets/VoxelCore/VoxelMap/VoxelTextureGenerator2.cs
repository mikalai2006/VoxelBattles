//using UnityEngine;
//using System.Collections.Generic;
//using Mikalai2006.VoxelBase;
//using Mikalai2006.VoxelMap; // Пространство имен для вашего TypeEntity

//public class VoxelTextureGenerator : MonoBehaviour
//{
//    [Header("Ссылки")]
//    [SerializeField] private WFCManager wfcManager;

//    [Header("Размеры игрового поля")]
//    public int textureWidth = 24;
//    public int textureHeight = 32;

//    [System.Serializable]
//    public class LevelRule
//    {
//        public TypeEntity entityType;
//        public int count = 2;
//        public int minSize = 3;
//        public int maxSize = 6;
//        [Range(0f, 1f)] public float brightness = 1f; // 1.0 = белый (макс. высота), меньше = серый
//    }

//    [Header("Правила генерации")]
//    public List<LevelRule> rules;

//    [Header("Превью для разработчика")]
//    public Texture2D debugVisualTexture;

//    private Dictionary<TypeEntity, Texture2D> textures = new Dictionary<TypeEntity, Texture2D>();
//    private TypeEntity[,] occupiedGrid; // Сетка для проверки наложений объектов

//    public void GenerateTexturesForLevel(int seed)
//    {
//        Random.InitState(seed);
//        textures.Clear();

//        // Создаем чистую сетку занятых ячеек и текстуру превью
//        occupiedGrid = new TypeEntity[textureWidth, textureHeight];
//        debugVisualTexture = CreateEmptyTexture();

//        // Подготавливаем чистые текстуры в памяти для всех сущностей из списка правил
//        foreach (var rule in rules)
//        {
//            if (rule.entityType != TypeEntity.None && !textures.ContainsKey(rule.entityType))
//            {
//                textures[rule.entityType] = CreateEmptyTexture();
//            }
//        }

//        // Основной цикл генерации объектов по правилам
//        foreach (var rule in rules)
//        {
//            if (rule.entityType == TypeEntity.None) continue;

//            int currentSpawned = 0;
//            int attempts = 0;

//            // Генерируем, пока не наберем нужное количество или не кончатся попытки
//            while (currentSpawned < rule.count && attempts < 300)
//            {
//                attempts++;

//                int w = Random.Range(rule.minSize, rule.maxSize + 1);
//                int h = Random.Range(rule.minSize, rule.maxSize + 1);

//                // Безопасная логика забора: сжимаем его в линию шириной в 1 пиксель
//                if (rule.entityType == TypeEntity.Zabor)
//                {
//                    // Рандомим 0 или 1 без использования знаков сравнения, чтобы текст не ломался
//                    int randomDirection = Random.Range(0, 2);
//                    if (randomDirection == 0)
//                    {
//                        w = 1;
//                    }
//                    else
//                    {
//                        h = 1;
//                    }
//                }

//                // Случайные координаты с защитой зоны ракетки (нижние 3 ряда всегда свободны)
//                int x = Random.Range(0, textureWidth - w + 1);
//                int y = Random.Range(0, textureHeight - 3 - h + 1);

//                // ПРОВЕРКА НАЛОЖЕНИЙ: Свободна ли зона (с буфером в 1 пиксель вокруг прямоугольника)
//                bool isAreaFree = true;

//                int checkXMin = Mathf.Max(0, x - 1);
//                int checkXMax = Mathf.Min(textureWidth - 1, x + w);
//                int checkYMin = Mathf.Max(0, y - 1);
//                int checkYMax = Mathf.Min(textureHeight - 1, y + h);

//                for (int cx = checkXMin; cx <= checkXMax; cx++)
//                {
//                    for (int cy = checkYMin; cy <= checkYMax; cy++)
//                    {
//                        if (occupiedGrid[cx, cy] != TypeEntity.None)
//                        {
//                            isAreaFree = false;
//                            break;
//                        }
//                    }
//                    if (!isAreaFree) break;
//                }

//                // Если область полностью свободна — занимаем её и закрашиваем в текстурах
//                if (isAreaFree)
//                {
//                    Texture2D targetTex = textures[rule.entityType];
//                    Color solidColor = new Color(rule.brightness, rule.brightness, rule.brightness, 1f);

//                    // Цвета маркеров для отладочного превью в инспекторе (Дом - синий, Пещера - зеленый, Забор - желтый)
//                    Color dColor = rule.entityType == TypeEntity.House ? Color.blue : (rule.entityType == TypeEntity.Cave ? Color.green : Color.yellow);

//                    for (int rx = x; rx < x + w; rx++)
//                    {
//                        for (int ry = y; ry < y + h; ry++)
//                        {
//                            occupiedGrid[rx, ry] = rule.entityType;
//                            targetTex.SetPixel(rx, ry, solidColor);
//                            debugVisualTexture.SetPixel(rx, ry, dColor * rule.brightness);
//                        }
//                    }

//                    currentSpawned++;
//                }
//            }
//        }

//        // Загружаем измененные массивы пикселей в видеопамять Unity
//        foreach (var tex in textures.Values)
//        {
//            tex.Apply();
//        }
//        debugVisualTexture.Apply();

//        // Напрямую передаем сгенерированные маски в ваш WFCManager
//        if (wfcManager != null)
//        {
//            if (textures.ContainsKey(TypeEntity.Cave)) wfcManager.settingMapCaves.texture = textures[TypeEntity.Cave];
//            if (textures.ContainsKey(TypeEntity.Zabor)) wfcManager.settingMapZabor.texture = textures[TypeEntity.Zabor];
//            if (textures.ContainsKey(TypeEntity.House)) wfcManager.settingMapHouses.texture = textures[TypeEntity.House];
//        }
//    }

//    private Texture2D CreateEmptyTexture()
//    {
//        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
//        tex.filterMode = FilterMode.Point; // Четкие пиксельные границы, как на референсе

//        // Заполняем текстуру абсолютно прозрачным черным цветом (0,0,0,0)
//        Color transparentBlack = new Color(0f, 0f, 0f, 0f);
//        for (int x = 0; x < textureWidth; x++)
//        {
//            for (int y = 0; y < textureHeight; y++)
//            {
//                tex.SetPixel(x, y, transparentBlack);
//            }
//        }
//        tex.Apply();
//        return tex;
//    }
//}


////using Mikalai2006.VoxelBase; // Пространство оставляем, если оно нужно для TypeEntity
////using Mikalai2006.VoxelMap;
////using System.Collections.Generic;
////using UnityEngine;

////public class VoxelTextureGenerator : MonoBehaviour
////{
////    [Header("Ссылки")]
////    [SerializeField] private WFCManager wfcManager;

////    [Header("Настройки сетки")]
////    public int textureWidth = 24;
////    public int textureHeight = 32;

////    [System.Serializable]
////    public class LevelRule
////    {
////        public TypeEntity entityType;
////        public int count = 2;
////        public int minSize = 3;
////        public int maxSize = 6;
////        [Range(0f, 1f)] public float brightness = 1f; // 1.0 = белый (макс. высота), меньше = серый
////    }

////    [Header("Правила генерации")]
////    public List<LevelRule> rules;

////    [Header("Превью")]
////    public Texture2D debugVisualTexture;

////    private Dictionary<TypeEntity, Texture2D> textures = new Dictionary<TypeEntity, Texture2D>();

////    private struct RoomRect
////    {
////        public int xMin, xMax, yMin, yMax;
////        public bool Overlaps(RoomRect other)
////        {
////            // Зазор в 1 пиксель, чтобы прямоугольники не слипались
////            return xMin - 1 <= other.xMax && xMax + 1 >= other.xMin && yMin - 1 <= other.yMax && yMax + 1 >= other.yMin;
////        }
////    }

////    public void GenerateTexturesForLevel(int seed)
////    {
////        Random.InitState(seed);
////        textures.Clear();
////        debugVisualTexture = CreateEmptyTexture();

////        // Создаем чистые текстуры для всех типов из правил
////        foreach (var rule in rules)
////        {
////            if (rule.entityType != TypeEntity.None && !textures.ContainsKey(rule.entityType))
////            {
////                textures[rule.entityType] = CreateEmptyTexture();
////            }
////        }

////        List<RoomRect> spawnedRooms = new List<RoomRect>();

////        foreach (var rule in rules)
////        {
////            if (rule.entityType == TypeEntity.None) continue;

////            int currentSpawned = 0;
////            int attempts = 0;

////            while (currentSpawned < rule.count && attempts < 200)
////            {
////                attempts++;

////                int w = Random.Range(rule.minSize, rule.maxSize + 1);
////                int h = Random.Range(rule.minSize, rule.maxSize + 1);

////                int x = Random.Range(0, textureWidth - w + 1);
////                int y = Random.Range(0, textureHeight - 3 - h + 1); // Нижние 3 ряда под ракетку не трогаем

////                RoomRect newRoom = new RoomRect { xMin = x, xMax = x + w - 1, yMin = y, yMax = y + h - 1 };

////                bool overlap = false;
////                foreach (var room in spawnedRooms)
////                {
////                    if (newRoom.Overlaps(room))
////                    {
////                        overlap = true;
////                        break;
////                    }
////                }

////                if (!overlap)
////                {
////                    spawnedRooms.Add(newRoom);
////                    DrawRect(newRoom, rule.entityType, rule.brightness);
////                    currentSpawned++;
////                }
////            }
////        }

////        // Применяем текстуры
////        foreach (var tex in textures.Values) tex.Apply();
////        debugVisualTexture.Apply();

////        // Передаем напрямую в ваш менеджер
////        if (wfcManager != null)
////        {
////            if (textures.ContainsKey(TypeEntity.Cave)) wfcManager.settingMapCaves.texture = textures[TypeEntity.Cave];
////            if (textures.ContainsKey(TypeEntity.Zabor)) wfcManager.settingMapZabor.texture = textures[TypeEntity.Zabor];
////            if (textures.ContainsKey(TypeEntity.House)) wfcManager.settingMapHouses.texture = textures[TypeEntity.House];

////        }
////    }

////    private void DrawRect(RoomRect room, TypeEntity type, float brightness)
////    {
////        Texture2D targetTex = textures[type];
////        Color solidColor = new Color(brightness, brightness, brightness, 1f);

////        // Цвет для дебаг-превью в инспекторе
////        Color dColor = type == TypeEntity.House ? Color.blue : (type == TypeEntity.Cave ? Color.green : Color.yellow);

////        for (int x = room.xMin; x <= room.xMax; x++)
////        {
////            for (int y = room.yMin; y <= room.yMax; y++)
////            {
////                targetTex.SetPixel(x, y, solidColor);
////                debugVisualTexture.SetPixel(x, y, dColor * brightness);
////            }
////        }
////    }

////    private Texture2D CreateEmptyTexture()
////    {
////        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
////        tex.filterMode = FilterMode.Point;
////        for (int x = 0; x < textureWidth; x++)
////            for (int y = 0; y < textureHeight; y++)
////                tex.SetPixel(x, y, Color.clear);
////        tex.Apply();
////        return tex;
////    }
////}
