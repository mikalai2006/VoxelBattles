using Unity.NetCode;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class MyGameSpecificBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // Отключаем автоподключение из редактора для билдов
        AutoConnectPort = 0;

        CreateClientWorld(defaultWorldName + "_Client");
        CreateServerWorld(defaultWorldName + "_Server");
        //return false;

        //// 1. Создаем пустой базовый мир для инициализации систем Unity
        //var defaultWorld = new World(defaultWorldName);
        //World.DefaultGameObjectInjectionWorld = defaultWorld;

        //// 2. Добавляем в него базовые системные группы (необходимы для работы DOTS)
        //var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        //DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(defaultWorld, systems);

        //// 3. Важно: мы НЕ вызываем здесь Netcode-инициализацию миров клиента и сервера.
        //// Мы оставляем этот базовый мир пустым от сетевой логики.

        //Debug.Log("Базовый DOTS-мир успешно создан. Авто-инициализация Netcode заблокирована.");
        //CreateLocalWorld(defaultWorldName);
        Debug.Log("[ClientServerBootstrap] Инициализация сетевых миров отменена...");

        // Возвращаем true — мы сами настроили DefaultGameObjectInjectionWorld
        return true;

    }
}

public class NetcodeBootstrapper : MonoBehaviour
{
    public ushort port = 7777;
    public string portStr = "7777";
    public string ipAddress = "127.0.0.1";
    public bool isCreateClient = false;
    public bool isCreateServer = false;

    void OnGUI()
    {
        ipAddress = GUI.TextField(new Rect(10, 10, 150, 40), ipAddress);
        portStr = GUI.TextField(new Rect(10, 50, 150, 40), portStr);

        // Рисуем простые кнопки на экране для теста
        if (GUI.Button(new Rect(10, 90, 150, 40), "Запустить Сервер"))
        {
            ConfigurePacketBatching();
            StartServer();
        }

        if (GUI.Button(new Rect(10, 130, 150, 40), "Подключить Клиента"))
        {
            ConfigurePacketBatching();
            StartClient();
        }
    }

    private void ConfigurePacketBatching()
    {
        // ИСПРАВЛЕНИЕ: Теперь типы полностью совпадают, явное приведение типов не требуется
        NetworkStreamReceiveSystem.DriverConstructor = new CustomDriverConstructor();
    }

    private ushort GetPort()
    {
        // 2. Пытаемся преобразовать строку в число
        if (int.TryParse(portStr, out int parsedPort))
        {
            // Преобразование прошло успешно, записываем результат
            port = (ushort)parsedPort;
        }
        else
        {
            // Здесь можно обработать ошибку, если пользователь ввел не цифры
        }

        return port;
    }

    private void StartServer()
    {
        if (isCreateServer)
        {
            Debug.Log("[Bootstrapper] Сервер подключен уже!");
            return;
        }

        var serverWorld = ClientServerBootstrap.ServerWorld;

        if (serverWorld == null)
        {
            Debug.Log("[Bootstrapper] ServerWorld не существовал. Создаем принудительно...");
            serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        }
        else
        {
            Debug.Log("[Bootstrapper] ServerWorld уже существовал.");
        }

        if (serverWorld != null)
        {
            var entityManager = serverWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new InitServerTag { Port = GetPort() });
            isCreateServer = true;
        }
        else
        {
            Debug.LogWarning("[Bootstrapper] ServerWorld not found!");
        }

    }

    private void StartClient()
    {
        if (isCreateClient)
        {
            Debug.Log("[Bootstrapper] Клиент подключен уже!");
            return;
        }

        var clientWorld = ClientServerBootstrap.ClientWorld;

        if (clientWorld == null)
        {
            Debug.Log("[Bootstrapper] ClientWorld не существовал. Создаем принудительно...");
            clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }

        if (clientWorld != null)
        {
            var entityManager = clientWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new InitClientTag { Address = ipAddress, Port = GetPort() });
            isCreateClient = true;
        }

    }
}

//using Unity.NetCode;
//using UnityEngine;

//public class NetcodeBootstrapper : MonoBehaviour
//{
//    public ushort port = 7777;
//    public string ipAddress = "127.0.0.1";

//    void OnGUI()
//    {
//        // Рисуем простые кнопки на экране для теста
//        if (GUI.Button(new Rect(10, 10, 150, 40), "Запустить Сервер"))
//        {
//            StartServer();
//        }

//        if (GUI.Button(new Rect(10, 60, 150, 40), "Подключить Клиента"))
//        {
//            StartClient();
//        }
//    }

//    private void StartServer()
//    {


//        // Ищем серверный мир Unity DOTS
//        var serverWorld = ClientServerBootstrap.ServerWorld;

//        // ЕСЛИ МИР НЕ НАЙДЕН (из-за настроек редактора) — СОЗДАЕМ ЕГО ПРИНУДИТЕЛЬНО!
//        if (serverWorld == null)
//        {
//            Debug.Log("[Bootstrapper] ServerWorld не существовал. Создаем принудительно...");
//            serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
//        }

//        if (serverWorld != null)
//        {
//            // Создаем сущность инициализации прямо внутри серверного мира
//            var entityManager = serverWorld.EntityManager;
//            var entity = entityManager.CreateEntity();
//            entityManager.AddComponentData(entity, new InitServerTag { Port = port });
//        }
//        else
//        {
//            Debug.LogWarning("[Bootstrapper] ServerWorld not found!");
//        }
//    }

//    private void StartClient()
//    {
//        // Ищем клиентский мир Unity DOTS
//        var clientWorld = ClientServerBootstrap.ClientWorld;
//        if (clientWorld != null)
//        {
//            // Создаем сущность инициализации прямо внутри клиентского мира
//            var entityManager = clientWorld.EntityManager;
//            var entity = entityManager.CreateEntity();
//            entityManager.AddComponentData(entity, new InitClientTag { Address = ipAddress, Port = port });
//        }
//    }
//}
