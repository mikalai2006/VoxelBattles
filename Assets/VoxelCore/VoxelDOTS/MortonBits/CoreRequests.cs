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
    public uint GhostId;
}

public struct ReplyMaskToClientRpc : IRpcCommand
{
    public uint GhostId;
    public FixedList512Bytes<byte> CompressedBytes;
    public FixedList512Bytes<byte> CompressedBytes2;
    public FixedList512Bytes<byte> CompressedBytes3;
    public FixedList512Bytes<byte> CompressedBytes4;
    public FixedList512Bytes<byte> CompressedBytes5;
    public FixedList512Bytes<byte> CompressedBytes6;
    public FixedList512Bytes<byte> CompressedBytes7;
    public FixedList512Bytes<byte> CompressedBytes8;
    public FixedList512Bytes<byte> CompressedBytes9;
}


//public struct NetworkChunkRleUpdate : IRpcCommand
//{
//    // К какому именно чанку применить изменения
//    public Entity TargetChunkEntity;

//    // Сюда мы запишем последовательность пар [повторения, значение]
//    public FixedList512Bytes<byte> CompressedBytes;
//}