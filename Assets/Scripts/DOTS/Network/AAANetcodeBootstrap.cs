using Unity.Entities;
using Unity.Networking.Transport;

[UnityEngine.Scripting.Preserve]
public class AAANetcodeBootstrap : NetCodeBootstrap
{
    // Этот метод автоматически перехватит инициализацию как серверного, так и клиентского драйвера
    public override bool CreateAndStartDriver(World world, int id, ref NetworkSettings settings)
    {
        // Включаем пакетный батчинг для сбора мелких 32-байтных снапшотов
        // Драйвер ждет до 30мс, склеивая сообщения в один большой пакет до 1400 байт.
        settings.WithSimulatedBufferParameters(
            maxPacketSize: 1400,
            sendDelayMs: 30
        );

        // Вызываем базовую логику Unity, которая создаст драйвер с нашими AAA-настройками
        return base.CreateAndStartDriver(world, id, ref settings);
    }
}
