using Unity.Entities;
using Unity.Mathematics;

public struct VoxelExplosionRequest : IComponentData
{
    public Entity TargetEntity; // В какую машину/чанк попали
    public float3 WorldPosition; // Точка клика в мире
    public float Radius;         // Радиус взрыва
}