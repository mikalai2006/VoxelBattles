using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent] // Помечает компонент для генератора Netcode
public struct AAA_InputComponent : IInputComponentData
{
    [GhostField] // Этот атрибут заставит Netcode синхронизировать ввод!
    public float2 MoveInput;
}


// Глобальный синглтон для хранения текущего состояния ввода
public struct InputStateSingleton : IComponentData
{
    public float2 MoveInput;
    public bool SwitchTargetTriggered;
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
