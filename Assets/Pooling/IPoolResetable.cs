public interface IPoolResetable
{
    // Вызывается при спавне из архива. Скрипт САМ решает, включать ли свой enabled
    void OnPoolSpawn();

    // Вызывается при деспавне в архив
    void OnPoolDespawn();
}
