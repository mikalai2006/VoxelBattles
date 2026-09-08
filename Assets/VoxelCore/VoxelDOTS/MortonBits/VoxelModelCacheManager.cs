using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class VoxelModelCacheManager : MonoBehaviour
{
    public static VoxelModelCacheManager Instance { get; private set; }

    //[Header("Настройки Палитры")]
    //[SerializeField] private List<Color32> colorPalette; // Ваша общая палитра цветов (индексы 1..255)

    [Header("Исходные воксельные модели")]
    [SerializeField] private List<VehiclePresetAsset> configsVehicles;
    public List<VehiclePresetAsset> ConfigsVehicles => configsVehicles;

    //[Header("Исходные воксельные модели")]
    //[SerializeField] private List<SOVoxelData> configs;

    //private Dictionary<Color32, byte> _paletteRegistry = new Dictionary<Color32, byte>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //// 1. Формируем быструю мапу палитры цветов
        //InitializePalette();
    }

    private void Start()
    {
        // ЖЕСТКИЙ ФИКС: Переносим вызов запекания в Start().
        // В Unity 6 ECS-миры (ServerWorld/ClientWorld) создаются сетевым ядром Netcode 
        // в фазе Awake/Initialization. Вызов запекания в Start гарантирует, что 
        // все мультиплеерные миры уже физически созданы в оперативной памяти и готовы принимать кэш!
        BakeAndRegisterAllModels();
    }

    private void BakeAndRegisterAllModels()
    {
        // КРИТИЧЕСКИЙ ФИКС ДЛЯ МУЛЬТИПЛЕЕРА UNITY 6 (MULTI-WORLD):
        // Извлекаем абсолютно ВСЕ активные ECS-миры, созданные сетевым движком Netcode
        var allWorlds = World.All;
        if (allWorlds.Count == 0)
        {
            Debug.LogError("[Voxel System]: Активные ECS-миры не обнаружены в оперативной памяти!");
            return;
        }

        int registeredModelsCount = 0;

        // Итерируемся по списку исходных ScriptableObject моделей
        for (int i = 0; i < configsVehicles.Count; i++)
        {
            SOVoxelData dataChassis = configsVehicles[i].chassis.meshConfig.sOVoxelData;
            if (dataChassis != null)
            {
                CreateCache(dataChassis);
                registeredModelsCount++;
            }

            SOVoxelData dataTower = configsVehicles[i].tower.meshConfig.sOVoxelData;
            if (dataTower != null)
            {
                CreateCache(dataTower);
                registeredModelsCount++;

                for (int j = 0; j < configsVehicles[i].tower.muzzles.Count; j++)
                {
                    SOVoxelData dataMuzzle = configsVehicles[i].tower.muzzles[j].meshConfig.sOVoxelData;
                    if (dataMuzzle != null)
                    {
                        CreateCache(dataMuzzle);
                        registeredModelsCount++;
                    }
                }
            }

            for (int j = 0; j < configsVehicles[i].wheelsPreset.wheelSlots.Count; j++)
            {
                SOVoxelData dataWheel = configsVehicles[i].wheelsPreset.wheelSlots[j].wheelPartAsset.meshConfig.sOVoxelData;
                if (dataWheel != null)
                {
                    CreateCache(dataWheel);
                    registeredModelsCount++;
                }
            }
        }

        Debug.Log($"[Voxel System]: Мультимирное запекание завершено. Всего зарегистрировано {registeredModelsCount} unmanaged-шаблонов во всех сетевых инстансах.");
    }


    private void CreateCache(SOVoxelData data)
    {
        var allWorlds = World.All;
        // Создаем unmanaged-строку из managed-имени ассета
        FixedString64Bytes unmanagedName = new FixedString64Bytes(data.name);
        //Debug.Log($"data.name={data.name}");
        // Используем нативный GetHashCode() и кастуем его в беззнаковый uint хэша
        uint configHashName = (uint)unmanagedName.GetHashCode();
        // ИСПРАВЛЕНО: Детерминированный хэш FNV-1a (работает везде одинаково)
        //uint configHashName = 2166136261; // Базовое смещение (FNV offset basis)
        //for (int j = 0; j < unmanagedName.Length; j++)
        //{
        //    // Умножаем на прайм-число FNV и смешиваем с байтом символа
        //    configHashName = (configHashName ^ unmanagedName[j]) * 16777619;
        //}

        //Debug.Log($"[Voxel System]: configHashName={configHashName}");
        // Бежим по всем существующим рантайм-мирам (ServerWorld, ClientWorld, ThinClientWorld)
        foreach (var world in allWorlds)
        {
            // Фильтруем миры: нам нужны только те, где крутится физическая или графическая симуляция игры.
            // Миры редактора (EditorWorld) или утилитарные миры конвертации мы отсекаем.
            var filterFlags = world.Flags;
            bool isServer = (filterFlags & WorldFlags.GameServer) != 0;
            bool isClient = (filterFlags & WorldFlags.GameClient) != 0;

            if (isServer || isClient)
            {
                // 2. Запускаем фабрику запекания для формирования плоского Morton-массива цветов.
                //                    // Мы делаем это для каждого мира отдельно (Allocator.Persistent), так как у каждого 
                //                    // мира своя изолированная unmanaged куча памяти NativeParallelHashMap!
                //                    MortonBakedModelResult bakedResult = VoxelMortonFactory.BakeMortonModel(data);

                //                    if (bakedResult.FlattenedMortonColors.IsCreated)
                //                    {
                //                        // Передаем и регистрируем unmanaged-массивы в синглтон GlobalVoxelModelCache конкретного мира!
                //                        VoxelModelRegistrar.RegisterModel(world, configHashName, bakedResult);

                //                        // Освобождаем временную нативную память фабрики
                //                        bakedResult.Dispose();

                //                        registeredModelsCount++;
                //#if UNITY_EDITOR
                //                        Debug.Log($"[Voxel Multi-World]: Модель '{data.name}' (Хэш: {configHashName}) успешно запечена в unmanaged-кэш мира: {world.Name}");
                //#endif
                //                    }

                LinearBakedModelResult bakedResult = VoxelLinearFactory.BakeLinearModel(data);

                if (bakedResult.FlattenedLinearColors.IsCreated)
                {
                    // Передаем и регистрируем unmanaged-массивы в синглтон GlobalVoxelModelCache конкретного мира!
                    VoxelModelRegistrar.RegisterModel(world, configHashName, bakedResult);

                    // Освобождаем временную нативную память фабрики
                    bakedResult.Dispose();

                    //#if UNITY_EDITOR
                    //                        Debug.Log($"[Voxel Multi-World]: Модель '{data.name}' (Хэш: {configHashName}) успешно запечена в unmanaged-кэш мира: {world.Name}");
                    //#endif
                }
            }
        }
    }

    //private void InitializePalette()
    //{
    //    _paletteRegistry.Clear();

    //    // Индекс 0 строго зарезервирован под воздух (уничтоженный воксель)
    //    int currentByteIndex = 1;

    //    for (int i = 0; i < colorPalette.Count; i++)
    //    {
    //        // Лимит байта — 255 уникальных цветов в одной палитре
    //        if (currentByteIndex > 255) break;

    //        Color32 color = colorPalette[i];
    //        if (!_paletteRegistry.ContainsKey(color))
    //        {
    //            _paletteRegistry[color] = (byte)currentByteIndex;
    //            currentByteIndex++;
    //        }
    //    }
    //}
}

//using System.Collections.Generic;
//using Unity.Collections;
//using Unity.Entities;
//using UnityEngine;

//public class VoxelModelCacheManager : MonoBehaviour
//{
//    public static VoxelModelCacheManager Instance { get; private set; }

//    [Header("Настройки Палитры")]
//    [SerializeField] private List<Color32> colorPalette; // Ваша общая палитра цветов (индексы 1..255)

//    [Header("Исходные воксельные модели")]
//    [SerializeField] private List<SOVoxelData> configs;

//    private Dictionary<Color32, byte> _paletteRegistry = new Dictionary<Color32, byte>();

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }

//        // 1. Формируем быструю мапу палитры цветов
//        InitializePalette();

//        // 2. Запускаем автоматическое запекание всех моделей в unmanaged-память ECS
//        BakeAndRegisterAllModels();
//    }
//    private void InitializePalette()
//    {
//        _paletteRegistry.Clear();

//        // Индекс 0 строго зарезервирован под воздух (уничтоженный воксель)
//        int currentByteIndex = 1;

//        for (int i = 0; i < colorPalette.Count; i++)
//        {
//            // Лимит байта — 255 уникальных цветов в одной палитре
//            if (currentByteIndex > 255) break;

//            Color32 color = colorPalette[i];
//            if (!_paletteRegistry.ContainsKey(color))
//            {
//                _paletteRegistry[color] = (byte)currentByteIndex;
//                currentByteIndex++;
//            }
//        }
//    }

//    private void BakeAndRegisterAllModels()
//    {
//        var world = World.DefaultGameObjectInjectionWorld;
//        if (world == null)
//        {
//            Debug.LogError("[Voxel System]: ECS Мир не найден! Запекание отложено.");
//            return;
//        }

//        for (int i = 0; i < configs.Count; i++)
//        {
//            SOVoxelData data = configs[i];
//            if (data == null) continue;

//            // 1. Создаем unmanaged-строку из managed-имени
//            FixedString64Bytes unmanagedName = new FixedString64Bytes(data.name);

//            // ИСПРАВЛЕНО: Используем нативный GetHashCode() и кастуем его в uint
//            uint configHashName = (uint)unmanagedName.GetHashCode();

//            // 2. Запекаем модель в плоский Morton-массив цветов
//            MortonBakedModelResult bakedResult = VoxelMortonFactory.BakeMortonModel(data, _paletteRegistry);

//            if (bakedResult.FlattenedMortonColors.IsCreated)
//            {
//                // Передаем и регистрируем unmanaged-массивы в синглтон GlobalVoxelModelCache
//                VoxelModelRegistrar.RegisterModel(world, configHashName, bakedResult);

//                // Освобождаем временную нативную память фабрики
//                bakedResult.Dispose();
//            }
//        }

//        Debug.Log($"[Voxel System]: Успешно запечено и перенесено в unmanaged-кэш {configs.Count} моделей.");
//    }
//}