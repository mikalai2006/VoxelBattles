using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

public class MemoryMonitorUniTask : MonoBehaviour
{
    [SerializeField] private float checkIntervalSeconds = 1f;

    // Токен для автоматической отмены задачи при уничтожении объекта
    private CancellationTokenSource cts;

    void Start()
    {
        cts = new CancellationTokenSource();
        // Запускаем асинхронную задачу без блокировки основного потока
        MonitorMemoryAsync(cts.Token).Forget();
    }

    private async UniTaskVoid MonitorMemoryAsync(CancellationToken token)
    {
        // Переводим секунды в миллисекунды для UniTask.Delay
        int delayMilliseconds = (int)(checkIntervalSeconds * 1000f);

        // Цикл работает, пока true (условие истинно / БОЛЬШЕ нуля)
        while (!token.IsCancellationRequested)
        {
            long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            long unusedMemory = Profiler.GetTotalUnusedReservedMemoryLong();
            long actualNativeUsed = totalMemory - unusedMemory;

            Debug.Log($"[UniTask Память] Реально занято Native-памяти: {actualNativeUsed / 1024f / 1024f:F2} МБ");

            // Асинхронное ожидание, привязанное к PlayerLoop (по умолчанию Update)
            // Также передаем токен, чтобы задача мгновенно прерывалась при удалении объекта
            await UniTask.Delay(delayMilliseconds, cancellationToken: token);
        }
    }

    void OnDestroy()
    {
        // Защита от утечек памяти самой задачи: отменяем UniTask при уничтожении скрипта
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
