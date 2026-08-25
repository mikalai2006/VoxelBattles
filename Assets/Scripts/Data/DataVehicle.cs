
[System.Serializable]
public class DataVehicle
{
    
    // public float speed;
    // public float armour;
    // public float hp;
    // [Tooltip("Позиция машины")]
    // public Vector3 position;
    // [Tooltip("Направление передвижения")]
    // public Vector3 directionMove;
    // [Tooltip("Угол поворота базы")]
    // public float angleBody;
    // [Tooltip("Время до обнаружения противника")]
    // public float timeBeforeAddTarget;
    // [Tooltip("Бонусы")]
    // public SerializedDictionary<TypeBonus, DataBonus> bonuses;
    // // [Tooltip("Значения бонусов")]
    // // public SerializedDictionary<TypeBonus, float> bonusesValue;
    // public List<BaseTower> towers;

    // // [Tooltip("Время от последнего выстрела")]
    // // public float timeAfterLastShot;
    // // [Tooltip("Дуло, которое сделало последний выстрел")]
    // // public BaseMuzzle muzzleLastShot;

    public DataVehicle()
    {
        // bonuses = new();
        // towers = new();
        // // bonusesValue = new();
    }
}


[System.Serializable]
public enum VehicleDetailType
{
    Wheel = 1,
    Caterpillar = 2,
    Body = 3,
    Tower = 4,
    Muzzle = 5,
    Elements = 6
}