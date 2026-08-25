using Unity.Entities;
using Unity.Mathematics;

// Глобальный синглтон для управления камерой
// Хранит идеальные координаты, куда должна прилететь камера.
public struct AAA_CameraSettingsSingleton : IComponentData
{
    // Настройки смещения относительно цели
    public float3 Offset;          // Например, (0, 8, -10) для вида от третьего лица
    public float SmoothSpeed;      // Скорость сглаживания (Lerp)

    // Внутреннее состояние (сюда Burst запишет вычисленную позицию)
    public float3 TargetPosition;
}
