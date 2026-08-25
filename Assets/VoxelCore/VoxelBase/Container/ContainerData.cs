using System;

// [Serializable]
// public class DataMachine
// {
//     // [Tooltip("Скорость передвижения")]
//     // public float speed;
//     // public float armour;
//     // [Tooltip("Здоровье")]
//     // public float hp;
//     [Tooltip("Позиция машины")]
//     public Vector3 position;
//     [Tooltip("Направление передвижения")]
//     public Vector3 directionMove;
//     [Tooltip("Угол поворота базы")]
//     public float angleBody;
//     [Tooltip("Угол поворота базы текущий")]
//     public float currentAngleBody;
//     [Tooltip("Время до обнаружения противника")]
//     public float timeBeforeAddTarget;
//     [Tooltip("Бонусы")]
//     public SerializedDictionary<TypeBonus, DataBonus> bonuses;
//     // [Tooltip("Значения бонусов")]
//     // public SerializedDictionary<TypeBonus, float> bonusesValue;
//     // public List<BaseTower> towers;
//     [Tooltip("Данные вокселей - разрушения")]
//     public ContainerData ContainerData;
//     public ContainerDataDetails ContainerDataDetails;

//     // [Tooltip("Время от последнего выстрела")]
//     // public float timeAfterLastShot;
//     // [Tooltip("Дуло, которое сделало последний выстрел")]
//     // public BaseMuzzle muzzleLastShot;

//     public DataMachine()
//     {
//         this.ContainerData = new();
//         this.ContainerDataDetails = new();
//         this.bonuses = new();
//         // towers = new();
//         // bonusesValue = new();
//     }
// }

    
[Serializable]
public struct ContainerData
{
    public int countVoxels;
    public int countVoxelsDestructible;
    public float levelDestruction;
}

//[Serializable]
//public struct ContainerDataDetails
//{
//    public ContainerData body;
//    public ContainerData tower;
//    public ContainerData wheels;
//    public ContainerData muzzle;
//}
