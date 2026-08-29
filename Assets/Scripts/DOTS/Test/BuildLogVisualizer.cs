using UnityEngine;

public class BuildLogVisualizer : MonoBehaviour
{
    private string myLogBuffer = "";
    private Vector2 scrollPosition;

    void OnEnable()
    {
        // Подписываемся на системный поток логов Unity
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Ловим только ошибки и исключения
        if (type == LogType.Error || type == LogType.Exception)
        {
            // Форматируем вывод: текст ошибки + ПОЛНЫЙ СТЕК
            myLogBuffer += $"[{type}] {logString}\nSTACK TRACE:\n{stackTrace}\n-------------------\n";
        }
    }

    void OnGUI()
    {
        // Если ошибок нет, ничего не рисуем
        if (string.IsNullOrEmpty(myLogBuffer)) return;

        // Рисуем окно с прокруткой поверх всей игры
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width - 40), GUILayout.Height(Screen.height - 40));

        // Выводим накопленный стек ошибок красным цветом
        GUI.color = Color.yellow;
        GUILayout.TextArea(myLogBuffer, GUILayout.ExpandHeight(true));

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
