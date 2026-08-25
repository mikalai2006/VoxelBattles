using System.Text;
using UnityEngine.UIElements;

public class IndicatorComponent : VisualElement
{
    private Label text;
    private StringBuilder _sbText = new StringBuilder(64);

    public IndicatorComponent()
    {
        AddToClassList("panel-primary");
        AddToClassList("flex-row");
        AddToClassList("items-center");
        AddToClassList("justify-center");

        text = new Label()
        {
            name = "TextElement",
        };
        text.BringToFront();
        text.AddToClassList("text-white");
        text.AddToClassList("text-sm");
        text.AddToClassList("text-middle-center");
        Add(text);
    }

    public void UpdateText(float _text)
    {
        _sbText.Clear(); // Очищаем буфер без выделения новой памяти
        _sbText.Append("~ ");
        _sbText.Append(_text);
        
        text.text = _sbText.ToString();  // ToString() здесь необходим
    }
}
