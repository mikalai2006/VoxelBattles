using Unity.NetCode;
using UnityEngine;

public class NetcodeBootstrapper : MonoBehaviour
{
    public ushort port = 7777;
    public string ipAddress = "127.0.0.1";

    void OnGUI()
    {
        // Рисуем простые кнопки на экране для теста
        if (GUI.Button(new Rect(10, 10, 150, 40), "Запустить Сервер"))
        {
            StartServer();
        }

        if (GUI.Button(new Rect(10, 60, 150, 40), "Подключить Клиента"))
        {
            StartClient();
        }
    }

    private void StartServer()
    {


        // Ищем серверный мир Unity DOTS
        var serverWorld = ClientServerBootstrap.ServerWorld;

        // ЕСЛИ МИР НЕ НАЙДЕН (из-за настроек редактора) — СОЗДАЕМ ЕГО ПРИНУДИТЕЛЬНО!
        if (serverWorld == null)
        {
            Debug.Log("[Bootstrapper] ServerWorld не существовал. Создаем принудительно...");
            serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        }

        if (serverWorld != null)
        {
            // Создаем сущность инициализации прямо внутри серверного мира
            var entityManager = serverWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new InitServerTag { Port = port });
        }
        else
        {
            Debug.LogWarning("[Bootstrapper] ServerWorld not found!");
        }
    }

    private void StartClient()
    {
        // Ищем клиентский мир Unity DOTS
        var clientWorld = ClientServerBootstrap.ClientWorld;
        if (clientWorld != null)
        {
            // Создаем сущность инициализации прямо внутри клиентского мира
            var entityManager = clientWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new InitClientTag { Address = ipAddress, Port = port });
        }
    }
}
