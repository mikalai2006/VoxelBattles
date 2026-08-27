using Unity.Collections;
using Unity.Mathematics;
using Unity.NetCode;

public struct VoxelExplosionRequestRpc : IRpcCommand
{
    public float3 RayOrigin;      // Откуда стреляем (позиция камеры)
    public float3 RayDirection;  // Куда стреляем (нормализованный вектор)
    public float Radius;         // Радиус взрыва
}

public struct RequestMaskFromServerRpc : IRpcCommand
{
    public uint GhostInstance;
}

public struct ReplyMaskToClientRpc : IRpcCommand
{
    public uint GhostInstance;
    public FixedList512Bytes<byte> CompressedBytes;
}