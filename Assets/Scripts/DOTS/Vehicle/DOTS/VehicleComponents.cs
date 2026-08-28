using Unity.Entities;
using Unity.Mathematics;

public struct VehicleRootTag : IComponentData { }

public struct VehicleFlatChildData : IComponentData
{
    public Entity ParentEntity;
    public float3 LocalOffset;
    public int HierarchyLevel;   // 0 = Шасси->Деталь, 1 = Башня->Дуло
}

// Современный управляемый компонент-запрос
public class RequestVehicleAssembly : IComponentData
{
    public VehiclePresetAsset Preset;
    public float3 SpawnPosition;
    public quaternion SpawnRotation;
    public bool IsDynamic = true;
    public bool isAddMove = false;
}
public struct VehicleSuspensionComponent : IComponentData
{
    public float RideHeight;       // Желаемый ход подвески / клиренс (от корпуса до центра колеса)
    public float WheelRadius;      // Радиус колеса (динамический упор)
    public float Frequency;        // Частота колебаний
    public float DampingRatio;     // Коэффициент затухания

    // Поля для безопасного вычисления сил без накопления числовой ошибки
    public float3 ForceToApply;
    public float3 WorldApplyPosition;
}
public struct SuspensionForceComponent : IComponentData
{
    public float3 ForceVector;
    public float3 WorldApplyPosition;
}

public struct WheelVisualRotation : IComponentData
{
    public float SpinAngle;     // Текущий угол качения колеса (в радианах)
    public float SteerAngle;    // Текущий угол поворота руля (для передних колес)
    public bool IsSteerable;    // Поворачивает ли это колесо?
}