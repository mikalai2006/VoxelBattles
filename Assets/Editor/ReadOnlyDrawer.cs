using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Mikalai2006.Utils
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Создаем стандартный UI Toolkit элемент для отображения свойства
            PropertyField propertyField = new PropertyField(property);

            // Отключаем интерактивность (поле становится серым и только для чтения)
            propertyField.SetEnabled(false);

            return propertyField;
        }
    }
}
