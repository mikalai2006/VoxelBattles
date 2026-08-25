using UnityEngine;

public interface IPoolable
{
    // Свойство для быстрой проверки менеджером: активен ли объект сейчас в игре
    bool IsActive { get; }

    // Метод пробуждения (вызывается при выдаче из пула)
    void Spawn(Vector3 position, Quaternion rotation);

    // Метод усыпления (вызывается при возврате в пул, принимает координату архива)
    void DeSpawn(Vector3 storagePosition);
}
