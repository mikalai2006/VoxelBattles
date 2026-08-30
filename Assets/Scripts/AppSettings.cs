using UnityEngine;

public class AppSettings : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;

        // Отключаем вертикальную синхронизацию (иначе она привяжет FPS к герцовке монитора: 60/144)
        QualitySettings.vSyncCount = 0;

        //// Жестко ограничиваем частоту кадров приложения на 45
#if UNITY_SERVER
        Application.targetFrameRate = 60; // Серверу DOTS за глаза хватит 30-60 FPS
#else
#if UNITY_EDITOR
        Application.targetFrameRate = -1;
#else
        Application.targetFrameRate = -1; // Клиенту
#endif
#endif

#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
    Debug.unityLogger.logEnabled = false;
#endif
    }
}
