using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

// 1. Создаем вариацию для трансформа (Квантуем Позицию)
[GhostComponentVariation(typeof(LocalTransform))]
public struct OptimizedTransformVariant
{
    // GhostField(Quantization = 100) превращает float в целое число с шагом 0.01 (1 см)
    // Smoothing Action заставляет клиент плавно дорисовывать движение между тиками
    [GhostField(Quantization = 100, Smoothing = SmoothingAction.Interpolate)]
    public float3 Position;

    // Вращение (кватернион) квантуем до 1000 для идеальной точности углов
    [GhostField(Quantization = 1000, Smoothing = SmoothingAction.Interpolate)]
    public quaternion Rotation;

    // Масштаб чанкам и моделям обычно штамповать не нужно, отключаем передачу
    [GhostField(SendData = false)]
    public float Scale;
}

// 2. Создаем вариацию для физической скорости
[GhostComponentVariation(typeof(PhysicsVelocity))]
public struct OptimizedVelocityVariant
{
    // Линейную скорость квантуем до 10 (шаг 0.1 м/с) — микровибрации отсекаются!
    [GhostField(Quantization = 10, Smoothing = SmoothingAction.Clamp)]
    public float3 Linear;

    // Угловую скорость тоже грубо квантуем, чтобы заглушить шум
    [GhostField(Quantization = 10, Smoothing = SmoothingAction.Clamp)]
    public float3 Angular;
}
