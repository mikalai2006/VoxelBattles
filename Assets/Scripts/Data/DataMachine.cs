//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;

//[Serializable]
//public class DataMachine
//{
//    // [Tooltip("Скорость передвижения")]
//    // public float speed;
//    // public float armour;
//    // [Tooltip("Здоровье")]
//    // public float hp;
//    [Tooltip("Позиция машины")]
//    public Vector3 position;
//    [Tooltip("Направление передвижения")]
//    public Vector3 directionMove;
//    [Tooltip("Угол поворота базы")]
//    public float angleBody;
//    [Tooltip("Угол поворота базы текущий")]
//    public float currentAngleBody;
//    [Tooltip("Время до обнаружения противника")]
//    public float timeBeforeAddTarget;
//    [Tooltip("Бонусы")]
//    public SerializedDictionary<TypeBonus, DataBonus> bonuses;
//    // [Tooltip("Значения бонусов")]
//    // public SerializedDictionary<TypeBonus, float> bonusesValue;
//    // public List<BaseTower> towers;
//    [Tooltip("Данные вокселей - разрушения")]
//    public ContainerData ContainerData;
//    public ContainerDataDetails ContainerDataDetails;

//    // [Tooltip("Время от последнего выстрела")]
//    // public float timeAfterLastShot;
//    // [Tooltip("Дуло, которое сделало последний выстрел")]
//    // public BaseMuzzle muzzleLastShot;

//    public DataMachine()
//    {
//        this.ContainerData = new();
//        this.ContainerDataDetails = new();
//        this.bonuses = new();
//        // towers = new();
//        // bonusesValue = new();
//    }
//}

//[Serializable]
//public struct ContainerDataDetails
//{
//    public ContainerData body;
//    public ContainerData tower;
//    public ContainerData wheels;
//    public ContainerData muzzle;
//}
//// [Serializable]
//// public class DataHP
//// {
////     public float hp;
////     public float hpBody;
////     public float hpTowers;
////     public
//// }

//[Serializable]
//public class DataMuzzle
//{
//    public int index;
//    [Tooltip("Может ли ствол стрелять")]
//    public bool isShot;
//    [Tooltip("Точка куда смотрит ствол")]
//    public Vector3 pointTarget;
//    [Tooltip("Время до перезарядки")]
//    public float timeBeforeShot;
//    [Tooltip("Время между выстрелами")]
//    public float timeBetweenShot;
//    [Tooltip("Начальная скорость полета снарядов")]
//    public float speedBullet;
//    [Tooltip("Дистанция атаки (как далеко полетят снаряды)")]
//    public float distanceAttack;
//    public ContainerData containerData;

//    // [Tooltip("Уровень разрушения")]
//    // public float levelDestruction;
//    public DataMuzzle() {
//        containerData = new();
//    }
//}

//[Serializable]
//public class DataBody
//{
//    public int index;
//    [Tooltip("Скорость")]
//    [Range(0.2f, 1000f)] public float speed;
//    [Tooltip("Угол поворота базы")]
//    public float angleBody;
//    [Tooltip("Угол поворота базы текущий")]
//    public float currentAngleBody;
//    public ContainerData containerData;
//    public DataBody() {
//        containerData = new();
//    }
//}

//[Serializable]
//public class DataCaterpillar
//{
//    public int index;
//    public ContainerData containerData;
//    public DataCaterpillar() {
//        containerData = new();
//    }
//}

//[Serializable]
//public class DataWheel
//{
//    public int index;
//    public ContainerData containerData;
//    public DataWheel() {
//        containerData = new();
//    }
//}

//[Serializable]
//public class DataTower
//{
//    public float speedRotateTower;
//    [Tooltip("Направление куда смотрит башня со стволом(ами)")]
//    public Vector3 directionTower;
//    public int index;
//    [Tooltip("Угол поворота башни")]
//    public float angleTower;
//    [Tooltip("Фактический угол поворота башни")]
//    public float currentAngleTower;
//    [Tooltip("Стволы башни")]
//    public List<BaseMuzzle> muzzles;
//    public ContainerData containerData;
//    public bool isRotate;

//    public DataTower() {
//        muzzles = new();
//        containerData = new();
//    }
//}


//[Serializable]
//public class DataBonus
//{
//    public string id;
//    [Tooltip("Время действия")]
//    public float time;
//    [Tooltip("Значение бонуса (которое добавляется к постоянному значению опред. параметра)")]
//    public float value;
//}