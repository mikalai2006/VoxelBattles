using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UIElements;

public class NetDebugger : MonoBehaviour
{
    private Label _rttLabel;
    private Label _tickLabel;

    void OnEnable()
    {
        // 1. Получаем корневой элемент UI документа
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // 2. Ищем текстовые метки по их именам (Name) из UI Builder
        // (Замените "ping-label" и "tick-label" на имена ваших элементов)
        _rttLabel = root.Q<Label>("ping-label");
        _tickLabel = root.Q<Label>("tick-label");
    }

    void Update()
    {
        // 1. Проверяем, существует ли мир клиента
        var clientWorld = ClientServerBootstrap.ClientWorld;
        if (clientWorld == null || !clientWorld.IsCreated) return;

        var em = clientWorld.EntityManager;

        // 2. ВЫВОД ПИНГА (RTT)
        var ackQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkSnapshotAck>());
        if (!ackQuery.IsEmptyIgnoreFilter && _rttLabel != null)
        {
            var ackComponent = ackQuery.GetSingleton<NetworkSnapshotAck>();
            float rtt = ackComponent.EstimatedRTT;
            _rttLabel.text = $"Ping: {rtt:F1} ms";
        }

        // 3. ВЫВОД ТИКОВ (Строго по аналогии с вашей TestClientSystem)
        var timeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkTime>());
        if (!timeQuery.IsEmptyIgnoreFilter && _tickLabel != null)
        {
            var networkTime = timeQuery.GetSingleton<NetworkTime>();

            // Читаем строковое значение тика и дробную часть точно так же, как в вашей системе
            var tickValue = networkTime.ServerTick.TickValue;
            float tickFrac = networkTime.ServerTickFraction;

            // Проверяем, является ли тик частичным (дробным)
            if (!networkTime.IsPartialTick)
            {
                _tickLabel.text = $"FullTick: {tickValue} | Frac: {tickFrac:F2}";
            }
            else
            {
                _tickLabel.text = $"PartialTick: {tickValue} | Frac: {tickFrac:F2}";
            }
        }

        var predictGhost = em.CreateEntityQuery(ComponentType.ReadOnly<PredictedGhost>());

    }
}
