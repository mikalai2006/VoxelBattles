using Unity.Collections;
using Unity.Entities;

// Компонент-маркер, чтобы запустить сервер
public struct InitServerTag : IComponentData
{
    public ushort Port;
}

// Компонент-маркер, чтобы запустить клиента
public struct InitClientTag : IComponentData
{
    public FixedString64Bytes Address;
    public ushort Port;
}
