public static class GameSceneEvents
{
    public static System.Action<AppInfoDamageData> AddInfoDamage;
    public static System.Action<VehicleAssembler> RefreshHP;
    public static System.Action<VehicleAssembler> SetHP;
}

/// <summary>
/// Структура данных для подготовки уведомления о нанесенном ущербе.
/// </summary>
[System.Serializable]
public struct AppInfoDamageData
{
    public VehicleAssembler kto;
    public VehicleAssembler komy;
    public string userText;
    public float duration;
}
