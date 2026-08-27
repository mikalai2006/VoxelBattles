using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

public class CustomDriverConstructor : INetworkStreamDriverConstructor
{
    public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

        // Настраиваем буфер под симулятор, чтобы пакеты не превышали лимит
        settings.WithSimulatorStageParameters(maxPacketSize: 65536, maxPacketCount: 1000);

        // ИСПРАВЛЕНИЕ ДЛЯ AAA: На localhost (в редакторе) задержка должна быть 0,
        // чтобы не ломать встроенную синхронизацию тиков Netcode.
        // Для реального билда в сети интернет здесь можно выставлять от 20 до 30.
        uint targetDelay = 0;

#if !UNITY_EDITOR
        // Если это реальный билд для игры через интернет, включаем легкую склейку пакетов
        targetDelay = 20; 
#endif

        settings.WithNetworkSimulatorParameters(
            receivePacketLossPercent: 0f,
            sendPacketLossPercent: 0f,
            sendDelayMS: targetDelay, // Убираем искусственный лаг в редакторе
            sendJitterMS: 0,
            sendDuplicatePercent: 0f,
            receiveMtu: 1400
        );

        DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
        settings.Dispose();
    }

    public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

        settings.WithSimulatorStageParameters(maxPacketSize: 65536, maxPacketCount: 1000);

        uint targetDelay = 0;

#if !UNITY_EDITOR
        targetDelay = 20;
#endif

        settings.WithNetworkSimulatorParameters(
            receivePacketLossPercent: 0f,
            sendPacketLossPercent: 0f,
            sendDelayMS: targetDelay,
            sendJitterMS: 0,
            sendDuplicatePercent: 0f,
            receiveMtu: 1400
        );

        DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, settings);
        settings.Dispose();
    }
}


//using Unity.Entities;
//using Unity.NetCode;
//using Unity.Networking.Transport;
//using Unity.Networking.Transport.Utilities;

//public class CustomDriverConstructor : INetworkStreamDriverConstructor
//{
//    // Метод создания драйвера для Клиента
//    public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
//    {
//        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

//        // 1. ИСПРАВЛЕНИЕ: Выделяем буфер под симулятор, чтобы пакеты не превышали лимит
//        // Задаем максимальный размер пакета 65536 байт (64 КБ) и емкость очереди в 1000 пакетов
//        settings.WithSimulatorStageParameters(maxPacketSize: 65536, maxPacketCount: 1000);

//        // 2. Настраиваем батчинг на 30мс
//        settings.WithNetworkSimulatorParameters(
//            receivePacketLossPercent: 0f,
//            sendPacketLossPercent: 0f,
//            sendDelayMS: 30,
//            sendJitterMS: 0,
//            sendDuplicatePercent: 0f,
//            receiveMtu: 1400
//        );

//        // Передаем сконфигурированные настройки в фабрику Unity
//        DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);

//        settings.Dispose();
//    }

//    // Метод создания драйвера для Сервера
//    public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
//    {
//        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

//        // 1. ИСПРАВЛЕНИЕ: Настраиваем буфер симулятора и для сервера
//        settings.WithSimulatorStageParameters(maxPacketSize: 65536, maxPacketCount: 1000);

//        // 2. Настраиваем батчинг на сервере
//        settings.WithNetworkSimulatorParameters(
//            receivePacketLossPercent: 0f,
//            sendPacketLossPercent: 0f,
//            sendDelayMS: 30,
//            sendJitterMS: 0,
//            sendDuplicatePercent: 0f,
//            receiveMtu: 1400
//        );

//        // Передаем настройки в серверную фабрику Unity
//        DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, settings);

//        settings.Dispose();
//    }
//}



////// Наш чистый конструктор, реализующий официальный интерфейс
////using Unity.Entities;
////using Unity.NetCode;
////using Unity.Networking.Transport;

////public class CustomDriverConstructor : INetworkStreamDriverConstructor
////{
////    // Метод создания драйвера для Клиента
////    public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
////    {
////        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

////        // ИСПРАВЛЕНИЕ: Используем легальный метод WithNetworkSimulatorParameters
////        // Параметры по порядку: lossPercent (0), sendLossPercent (0), sendDelayMS (30), sendJitterMS (0), sendDuplicatePercent (0), receiveMtu (1400)
////        settings.WithNetworkSimulatorParameters(
////            receivePacketLossPercent: 0f,
////            sendPacketLossPercent: 0f,
////            sendDelayMS: 30, // Собираем мелкие 32-байтные снапшоты в течение 30мс
////            sendJitterMS: 0,
////            sendDuplicatePercent: 0f,
////            receiveMtu: 1400 // Ограничиваем размер пакета до безопасного интернет-MTU
////        );

////        // Регистрируем клиентский драйвер через стандартную фабрику Unity
////        DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, settings);

////        settings.Dispose();
////    }

////    // Метод создания драйвера для Сервера
////    public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
////    {
////        var settings = new NetworkSettings(Unity.Collections.Allocator.Temp);

////        // ИСПРАВЛЕНИЕ: Настраиваем батчинг пакетов для Сервера
////        settings.WithNetworkSimulatorParameters(
////            receivePacketLossPercent: 0f,
////            sendPacketLossPercent: 0f,
////            sendDelayMS: 30, // Склеиваем тики игроков в монолитные отправки раз в 30мс
////            sendJitterMS: 0,
////            sendDuplicatePercent: 0f,
////            receiveMtu: 1400
////        );

////        // Регистрируем серверный драйвер через стандартную фабрику Unity
////        DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, settings);

////        settings.Dispose();
////    }
////}

