using UnityEngine;

public class AppSettings : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;

        //// Отключаем вертикальную синхронизацию (иначе она привяжет FPS к герцовке монитора: 60/144)
        //QualitySettings.vSyncCount = 0;

        //// Жестко ограничиваем частоту кадров приложения на 45
        //Application.targetFrameRate = 45;

#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
    Debug.unityLogger.logEnabled = false;
#endif
    }
}
