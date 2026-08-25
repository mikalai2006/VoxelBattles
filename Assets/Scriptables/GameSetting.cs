using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu]
public class GameSetting : ScriptableObject
{
  public GameAudio Audio;
  // // [SerializeField] public AssetReferenceGameObject prefabHiddenChar;
  public GameTheme ThemeDefault;
  // public List<GamePlayerSetting> PlayerSettings;
  // public List<GameType> GamesTypes;
  // public ArcadaItemLayout rebusItemPrefab;
  // public ArcadaItemLayout rebusItemBigPrefab;
  // public GameObject rainbowPrefab;
  // public TextEffect textEffectPrefab;
  // public BottleStar bottleStarPrefab;
  // public BottleEyes bottleEyesPrefab;
  // public GameObject demandStarPrefab;
  // public GameBonus[] throphs;

  [Space(5)]
  [Header("Настройки игры")]
  public List<ColorModifyItem> colorsModify;
  public bool simpleMove;
  public PlayerOptions playerOptions;
  [Tooltip("Размер масштаба игровых объектов")]
  public float scaleObjects;
    [Tooltip("Размер вокселей")]
    public float scaleVoxels;
    [Tooltip("Количество метров в единице Unity")]
  public float meterPerUnit;
  [Tooltip("Максимальный радиус создания вокселей")]
  [Range(6,10)] public int maxRadiusCreateVoxels;
  [Tooltip("Количество вокселей, которые будут созданы за кадр")]
  [Range(10,1000)] public int countMaxCreateVoxelsByStep;
  [Tooltip("Количество вокселей, которые будут созданы за кадр (для мобильных игр)")]
  [Range(10,1000)] public int countMaxCreateVoxelsByStepMobile;
    [Tooltip("Время жизни вокселей осколков")]
    public Vector2 voxelLifeTime;

    [Space(5)]
  [Header("Настройки отладки")]
  public DebugSettings DebugSettings;
  
  [Space(5)]
  [Header("Плейгейм")]
  [Tooltip("Поворачивать башню по ходу машины, если нет врага")]
  public bool rotateTowerByBody;
  [Tooltip("Время обнаружения противника от и до, сек")]
  public Vector2 timeBeforeAddTarget;
  [Tooltip("Расстояние на котором запрещена атака, слишком близко")]
  [Range(0,3f)] public float distanceDisableAttack;
  [Tooltip("Показывать ли зоны поиска и атаки для ботов")]
  public bool drawAreaForBot;
  [Tooltip("Коэффициент смещения угла поворота башни при попадании")]
  [Range(0,10f)] public float koofChangeAngleTower;
  [Tooltip("Скорость изменения размеров зон поиска и сектора атаки")]
  [Range(1f,20f)] public float speedChangeAreaSize;
  [Tooltip("Минимальный угол на котором стартует стрельба")]
  [Range(5f, 30f)] public float angleStartShot;
  [Tooltip("Захватывать ли ближайшего противника")]
  public bool takeNearEnemy;
  [Tooltip("Автонаведение на цель (не для ботов)")]
  public bool autoTakeEnemy;
  [Tooltip("Автовыстрелы при наведении на цель (не для ботов)")]
  public bool autoShot;
  [Tooltip("Смещение маркера HUD")]
  public Vector3 offsetHud;


  [Space(5)]
  [Header("System")]
  public List<Sprite> gerbs;
  public List<GameLevel> levels;
  public List<VehiclePartAsset> machines;
  public List<GameRank> ranks;
  public List<Color> commandColor;
  // public TypeGame typeGame;
  // [Range(0.1f, 1f)] public float lineWidth;
  // [Range(0.5f, 3f)] public float radius;
  // // [Range(0, 3)] public int addinitiallyRow;
  // [Range(10, 200)] public int maxCountHiddenChar;
  // [Tooltip("Количество ячеек для создания пульсирующей сущности")]
  // [Range(3, 6)] public int minCellForPulse;
  // [Tooltip("Сколько секунд ждать до подсказки, что совпадают ячейки")]
  // [Range(0, 5f)] public float delayCheckBoom;
  // [Tooltip("Задержка в секундах до изменения ячеек сетки для сущностей при драге")]
  // [Range(0, 1f)] public float delayChangeNodes;
  // [Tooltip("Задержка в милисекундах для очереди создания советов")]
  // public int debounceTimeHints;
  // [Tooltip("Задержка в милисекундах до заполнения нод сущностями после boom")]
  // [Range(0f, 1000f)] public int delayMoveNodes;
  // public int countHoursDailyGift;

  [Space(5)]
  [Header("Настройки области атаки")]
  public Color colorAreaAttackDefault;
  public Color colorAreaAttackViewed;
  public Color colorAreaAttackAttack;
  public Color colorSectorPlayerAttack;
  public Color colorSectorAttack;

  [Space(5)]
  [Header("Глобальные префабы")]
  public GameObject prefabVoxel;
  public GameObject sectorVoxel;
  public Material materialTransparent;
  public Sprite icoDefault;

  // [Space(5)]
  // [Header("Конфигурации тайлов")]
  // [Tooltip("Конфигурации тайлов, используются для изменения цветов на лету")]
  // public List<SOVoxelData> tailsConfigs;

  [Space(5)]
  [Header("Текст")]
  //[Tooltip("Префаб всплывающего текста")]
  //public TextDamage prefabTextDamage;
  [Tooltip("Цвет текста при получении урона")]
  public Color colorTextDamage;
  [Tooltip("Цвет текста при пополнении HP")]
  public Color colorTextDamagePlus;

  // [Space(5)]
  // [Header("Эффекты tilemap")]
  // public GameObject boomEffect;

  
  [Space(5)]
  [Header("Маркеры HUD")]
  [Tooltip("Смещение маркеров по краям экрана (ед. - экранные единицы)")]
  public Vector2 offsetMarkerEdge;
  [Tooltip("Цвет фона маркера")]
  public Color colorMarkerBg;
  [Tooltip("Цвет уровня здоровья на маркере")]
  public Color colorMarkerProgress;

  // [Space(5)]
  // [Header("Редактор")]
  // [Tooltip("Рисовать вспомогательные линии, которые показывают направление выстрела")]
  // public bool drawLineAttack;
  // public Sprite spriteArc5px;

  // [Space(5)]
  // [Header("Particle System")]
  // public GameObject PopParticle;
  // public ParticleSystem PopBig;
  // public ParticleSystem PulseParticle;

  [Space(5)]
  [Header("Save&Load")]
  [Tooltip("Задержка в милисекундах для очереди сохранения")]
  public int debounceTimeSave;
  public string nameSaveData;
  public string nameSaveUserInfo;
  public string pathFileColors;

  [Space(5)]
  [Header("Texts")]
  public TextLocalize noName;
  public List<string> names;

  [Space(5)]
  [Header("UI")]
  public Sprite spriteClose;

  [Space(5)]
  [Header("Shop")]
  public GameObject prefabShopItemMachine;
  public GameObject prefabGarageItemTower;
    // public List<ShopItem<GameEntity>> ShopItems;
    // public List<ShopItem<GameBonus>> ShopItemsBonus;

    // [Space(5)]
    // [Header("API Directory")]
    // public APIDirectory APIDirectory;


    // [Space(5)]
    // [Header("Ads")]
    // public int adsPerTime;

    //   [Space(5)]
    //   [Header("Rate")]
    //   public int minRateForReview;
    //   public int countCoinForReview;
    // [Space(5)]
    // [Header("Test")]
    // [Tooltip("Ограничить переход на следующие уровни если не пройдены предыдущие в пазлах")]
    // public bool isDisableNextButton;
    // [Tooltip("Количество видимых следующих пазлов")]
    // public int countNextPuzzle;
    // public TileBase tileSquare;
}

// [System.Serializable]
// public struct APIDirectory
// {
//   // public string host;
//   public string token;
//   // public string expression;
//   public string pathExpression;
// }

[System.Serializable]
public class RootObjectColorsItemFromJSON
{
    public ColorsItemFromJSON[] colors;
}

[System.Serializable]
public struct ColorsItemFromJSON
{
  public string id;
  public string name;
}


[System.Serializable]
public struct ShopItem<T>
{
  public T entity;
  public int count;
  public int cost;
}

[System.Serializable]
public struct ShopAdvBuyItem<T>
{
  public T typeItem;
  public int count;
}

[System.Serializable]
public struct TextLocalize
{
  public LocalizedString title;
  public LocalizedString description;
}

[System.Serializable]
public struct PlayerOptions
{
  [Tooltip("Показать траекторию прицеливания мою")]
  public bool showTrajectory;
  [Tooltip("Показать траекторию прицеливания врага")]
  public bool showOtherTrajectory;
  [Tooltip("Максимальное время между щелчками, при котором щелчок считается двойным")]
  public float doubleClickThreshold;
  [Tooltip("Начальная скорость вращения камеры")]
  public Vector2 speedRotateCamera;
  [Tooltip("Шанс рикошета (1 - все рикошет, 0 - без рикошета)")]
  [Range(0f, 1)] public float chanceReflex;
  [Tooltip("FPS на моб. устройствах")]
  public int MobileFPS;
  [Tooltip("Сколько кадров ждать обновления позиции HUD маркера (1 - обновление каждый кадр)")]
  public int delayFrameRefreshHud;
}

[Serializable]
public struct DebugSettings
{
  public AppMode mode;
    public int FPS;
    public bool disableCreateTiles;
  public bool logEnabled;
  [Tooltip("Если включено, то объекты ECS будут создаваться с коллайдером сферы(более производительны, но менее эффектны), иначе BoxCollider")]
  public bool ECSColliderSphere;
  [Tooltip("Смещение точки спавна bullet")]
  public Vector3 muzzleOffsetEffectPoint;

  [Header("Gizmos")]
  public bool gizmoWheels;
  public float gizmoWheelsLength;
  public Color gizmoWheelsColor;
  public bool gizmoBodyForwards;
  public float gizmoBodyLength;
  public Color gizmoBodyColor;
  public bool gizmoTowersForwards;
  public float gizmoTowersLength;
  public Color gizmoTowersColor;
  public bool gizmoMuzzlesForwards;
  public float gizmoMuzzleLength;
  public Color gizmoMuzzleColor;
  public bool gizmoTrajectory;
  public Color gizmoTrajectoryColor;
  public bool gizmoMuzzleDistanseAttack;
  public Color gizmoMuzzleDistanseAttackColor;
  public bool lineAttackDraw;
  public Color lineAttackColor;
}

[Serializable]
public enum AppMode
{
    Desktop = 1,
    Mobile = 2
}

[Serializable]
public struct ColorModifyItem
{
  public Color32 color;
  public int cost;
  public int rank;
}