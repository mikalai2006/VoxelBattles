using UnityEngine;
using System.Collections.Generic;

public class VehicleWheelRotator : MonoBehaviour
{
    private LevelManager LevelManager;

    private struct WheelData
    {
        public VirtualChild virtualChild;
        public float directionSign;
    }

    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeedMultiplier = 200f;

    private WheelData[] wheels;
    private int wheelCount = 0;

    /// <summary>
    /// Инициализация ротатора на основе проектных данных сборщика.
    /// Полностью защищена от архивных координат пула.
    /// </summary>
    public void SetupWheels(List<PoolNode> activeWheelNodes, List<Vector3> assemblyPositions, LevelManager levelManager)
    {
        LevelManager = levelManager;

        wheelCount = activeWheelNodes.Count;
        wheels = new WheelData[wheelCount];

        for (int i = 0; i < wheelCount; i++)
        {
            wheels[i].virtualChild = activeWheelNodes[i].VirtualChild;

            if (wheels[i].virtualChild != null)
            {
                // Переводим колесо в режим свободного качения по оси X
                wheels[i].virtualChild.SetRotationMode(VirtualChild.RotationMode.FreeRollX);
            }

            // ЖЕСТКАЯ ЗАЩИТА: Проверяем знак 'x' из проектной mountPosition сборщика.
            // Чертеж сборщика никогда не врет, в отличие от не успевшего обновиться Transform!
            if (assemblyPositions[i].x < 0f)
            {
                wheels[i].directionSign = -1f; // Левое зеркальное колесо
            }
            else
            {
                wheels[i].directionSign = 1f;  // Правое колесо
            }
        }
    }

    public void RotateWheels(float currentSpeed)
    {
        if (wheelCount == 0) return;

        // Вычисляем базовый угол поворота для текущего кадра
        float baseRotationAngle = (currentSpeed / 10f) * rotationSpeedMultiplier * Time.deltaTime;

        for (int i = 0; i < wheelCount; i++)
        {
            if (wheels[i].virtualChild != null)
            {
                float finalAngleDelta = baseRotationAngle * wheels[i].directionSign;

                // Передаем дельту угла напрямую в VirtualChild
                wheels[i].virtualChild.AddRollRotation(finalAngleDelta);
            }
        }
    }
}

//using UnityEngine;
//using System.Collections.Generic;

///// <summary>
///// Компонент отвечает за автоматическое визуальное вращение всех сгенерированных колес.
///// Оптимизирован под систему виртуального родительства VirtualChild (Zero-Alloc, Zero-GetComponent).
///// Учитывает инверсию осей развернутых (левых) колес для воксельных моделей.
///// </summary>
//public class VehicleWheelRotator : MonoBehaviour
//{
//    // Ссылка на менеджер уровня (сохранена для совместимости и внешних вызовов)
//    private LevelManager LevelManager;

//    // Вспомогательная структура для хранения колеса и направления его вращения
//    private struct WheelData
//    {
//        // Храним прямую ссылку на VirtualChild вместо Transform,
//        // чтобы крутить колесо на уровне логики пула без конфликтов трансформаций
//        public VirtualChild virtualChild;
//        public float directionSign; // 1f для правых колес, -1f для зеркальных левых
//    }

//    [Header("Настройки вращения")]
//    [Tooltip("Множитель скорости вращения. Подбирается визуально под диаметр воксельной шины")]
//    [SerializeField] private float rotationSpeedMultiplier = 200f;

//    private WheelData[] wheels;
//    private int wheelCount = 0;

//    /// <summary>
//    /// Инициализация ротатора из сборщика машины. 
//    /// Принимает список готовых нод пула PoolNode и ссылку на LevelManager.
//    /// Выполняется за 0 вызовов GetComponent в рантайме.
//    /// </summary>
//    public void SetupWheels(List<PoolNode> activeWheelNodes, LevelManager levelManager)
//    {
//        LevelManager = levelManager;

//        wheelCount = activeWheelNodes.Count;
//        wheels = new WheelData[wheelCount];

//        for (int i = 0; i < wheelCount; i++)
//        {
//            PoolNode node = activeWheelNodes[i];
//            wheels[i].virtualChild = node.VirtualChild;

//            // Переводим VirtualChild колеса в режим свободного качения по локальной оси X
//            if (wheels[i].virtualChild != null)
//            {
//                wheels[i].virtualChild.SetRotationMode(VirtualChild.RotationMode.FreeRollX);
//            }

//            // СОХРАНЯЕМ ВАШУ ЛОГИКУ:
//            // Если локальная координата X отрицательная (левое колесо, которое сборщик развернул на 180 градусов),
//            // выставляем коэффициент -1f, чтобы скомпенсировать разворот локальной оси вращения X.
//            if (node.Transform.localPosition.x < 0f)
//            {
//                wheels[i].directionSign = -1f;
//            }
//            else
//            {
//                wheels[i].directionSign = 1f;
//            }
//        }
//    }

//    /// <summary>
//    /// Вращает все колеса на основе скорости машины с учетом направления их установки.
//    /// Передает дельты углов в логику пула (Zero-Alloc, без тригонометрии на CPU).
//    /// </summary>
//    /// <param name="currentSpeed">Текущая реальная скорость автомобиля.</param>
//    public void RotateWheels(float currentSpeed)
//    {
//        if (wheelCount == 0) return;

//        // Вычисляем базовый угол поворота для текущего кадра
//        float baseRotationAngle = (currentSpeed / 10f) * rotationSpeedMultiplier * Time.deltaTime;

//        // Проходим быстрым циклом по кэшированному массиву структур
//        for (int i = 0; i < wheelCount; i++)
//        {
//            if (wheels[i].virtualChild != null)
//            {
//                // Умножаем угол на индивидуальный знак направления (directionSign) конкретного колеса
//                float finalAngleDelta = baseRotationAngle * wheels[i].directionSign;

//                // Передаем дельту угла напрямую в VirtualChild. 
//                // Матричный AngleAxis внутри пула сам применит вращение без конфликтов и лагов.
//                wheels[i].virtualChild.AddRollRotation(finalAngleDelta);
//            }
//        }
//    }
//}




////using UnityEngine;
////using System.Collections.Generic;

/////// <summary>
/////// Компонент отвечает за автоматическое визуальное вращение всех сгенерированных колес.
/////// Оптимизирован: учитывает инверсию осей развернутых (левых) колес.
/////// </summary>
////public class VehicleWheelRotator : MonoBehaviour
////{
////    LevelManager LevelManager;
////    // Вспомогательная структура для хранения колеса и направления его вращения
////    private struct WheelData
////    {
////        public Transform transform;
////        public float directionSign; // 1f для правых колес, -1f для зеркальных левых
////    }

////    [Header("Настройки вращения")]
////    [Tooltip("Множитель скорости вращения. Подбирается визуально под диаметр воксельной шины")]
////    [SerializeField] private float rotationSpeedMultiplier = 200f;

////    private WheelData[] wheels;
////    private int wheelCount = 0;

////    /// <summary>
////    /// Инициализация ротатора из сборщика машины. 
////    /// Принимает списки колес и автоматически определяет направление вращения на основе знака координаты X.
////    /// </summary>
////    public void SetupWheels(List<Transform> activeWheels, LevelManager levelManager)
////    {
////        LevelManager = levelManager;

////        wheelCount = activeWheels.Count;
////        wheels = new WheelData[wheelCount];

////        for (int i = 0; i < wheelCount; i++)
////        {
////            wheels[i].transform = activeWheels[i];

////            // Если локальная позиция колеса по X отрицательная (левая сторона), 
////            // инвертируем его вращение (-1f), чтобы компенсировать разворот по Y на 180 градусов
////            if (activeWheels[i].localPosition.x < 0f)
////            {
////                wheels[i].directionSign = -1f;
////            }
////            else
////            {
////                wheels[i].directionSign = 1f;
////            }
////        }
////    }

////    /// <summary>
////    /// Вращает все колеса на основе скорости машины с учетом направления их установки.
////    /// </summary>
////    public void RotateWheels(float currentSpeed)
////    {
////        if (wheelCount == 0) return;

////        // Вычисляем базовый угол поворота для текущего кадра
////        float baseRotationAngle = currentSpeed / 10f * rotationSpeedMultiplier * Time.deltaTime;

////        // Проходим циклом по кэшированному массиву структур
////        for (int i = 0; i < wheelCount; i++)
////        {
////            if (wheels[i].transform != null)
////            {
////                // Умножаем угол на индивидуальный знак направления (directionSign) конкретного колеса
////                float finalAngle = baseRotationAngle * wheels[i].directionSign;

////                // Вращаем колесо вокруг ЛОКАЛЬНОЙ оси X (Space.Self)
////                wheels[i].transform.Rotate(Vector3.right * finalAngle, Space.Self);
////            }
////        }
////    }
////}
