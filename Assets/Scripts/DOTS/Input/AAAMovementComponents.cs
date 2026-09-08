using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

//[GhostComponent] // Помечает компонент для генератора Netcode
//public struct AAA_InputComponent : IInputComponentData
//{
//    [GhostField(Quantization = 100)] // Этот атрибут заставит Netcode синхронизировать ввод!
//    public float2 MoveInput;
//}
//[GhostComponent]
public struct AAA_InputComponent : IInputComponentData
{
    // Оставляем только чистое поле типа byte для идеальной сетевой репликации
    //[GhostField]
    public byte ButtonsMask;
}


// Глобальный синглтон для хранения текущего состояния ввода
public struct InputStateSingleton : IComponentData
{
    public float2 MoveInput;
    public bool SwitchTargetTriggered;

    //public float3 ShootDirection;
    public bool ShootTriggered;
}

// Компонент настроек движения
[GhostComponent] // Помечаем всю структуру для генератора Netcode
public struct AAA_MovementComponent : IComponentData
{
    /// <summary>
    /// Максимальная скорость
    /// </summary>
    [GhostField] public float MaxSpeed;
    /// <summary>
    /// Скорость разгона
    /// </summary>
    [GhostField] public float Acceleration;
    /// <summary>
    /// Скорость торможения
    /// </summary>
    [GhostField] public float Deceleration;

    // Критически важное поле:
    // Если вы записываете сюда velocity.Linear для анимаций, 
    // его НЕ НАДО помечать атрибутом [GhostField]! 
    // Так как скорость меняется КАЖДЫЙ кадр, её репликация внутри этого компонента 
    // действительно начала бы забивать трафик, дублируя работу PhysicsVelocity.
    public float3 CurrentVelocity;
}
