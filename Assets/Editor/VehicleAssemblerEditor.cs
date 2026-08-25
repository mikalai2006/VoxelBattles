using UnityEngine;
using UnityEditor; // Обязательно для работы с редактором

[CustomEditor(typeof(VehicleAssembler))] // Привязываем к вашему скрипту
public class VehicleAssemblerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Рисуем стандартный инспектор (поля, переменные)
        DrawDefaultInspector();

        // Получаем ссылку на сам скрипт сборщика
        VehicleAssembler assembler = (VehicleAssembler)target;

        GUILayout.Space(10); // Отступ

        // Создаем кнопку в интерфейсе
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Собрать воксельную машину", GUILayout.Height(35)))
            {
                // Регистрируем действие для отмены (Ctrl+Z)
                Undo.RegisterCreatedObjectUndo(assembler.gameObject, "Assemble Vehicle");

                assembler.Setup(true);

                // Запускаем ваш метод сборки
                assembler.AssembleVehicle();
            }
        }
    }
}
