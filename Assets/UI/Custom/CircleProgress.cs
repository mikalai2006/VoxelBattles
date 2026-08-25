using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Пользовательский визуальный элемент, отображающий прогресс
/// </summary>
[UxmlElement]
public partial class CircleProgress : VisualElement, IStyles, INotifyValueChanged<float>
{
    GameManager _gameManager => GameManager.Instance;
    public static class IDNames
    {
        public static string InfoBoxRowWrapper = "ProgressWrapper";
        
    }
    
    float totalAngle = 0;
    private void SetTotalAngle()
    {
        totalAngle = Mathf.Abs(Mathf.Abs(startAngle - endAngle) - offset);
    }

    public LineCap m_lineCap;
    [UxmlAttribute("LineCap")]
    public LineCap lineCap
    {
        get {
            return m_lineCap;
        }
        set
        {
            m_lineCap = value;
            MarkDirtyRepaint();
        }
    }

    [UxmlAttribute("ColorBg")]
    public Color colorBg;

    [UxmlAttribute("Color")]
    public Color color;

    public void SetValueWithoutNotify(float newValue)
    {
        if (m_direction == ArcDirection.Clockwise)
        {
            var currentValue = m_StartAngle + (newValue * totalAngle); // Mathf.Clamp(newValue * 100f / totalValue, 0f, 100f);

            m_current_value = Mathf.Clamp(currentValue, m_StartAngle + offset, m_EndAngle - offset);
        } else
        {
            // var totalValue = Mathf.Abs(Mathf.Abs(m_StartAngle - m_EndAngle) - offset);

            var currentValue = m_StartAngle - (newValue * totalAngle); // Mathf.Clamp(newValue * 100f / totalValue, 0f, 100f);

            m_current_value = Mathf.Clamp(currentValue, m_EndAngle + offset, m_StartAngle - offset);
        }

        m_value = newValue;

        MarkDirtyRepaint();
    }

    private float m_current_value;
    private float m_value = 0.5f;
    [UxmlAttribute("valueProgress")]
    [Range(0, 1)]
    public float value
    {
        get {
            return m_value;
        }
        set
        {
            SetValueWithoutNotify(value);
        }
    }
    private int m_width = 10;
    [UxmlAttribute("Width")]
    public int width
    {
        get {
            return m_width;
        }
        set
        {
            m_width = value;
            MarkDirtyRepaint();
        }
    }
    private int m_radius = 100;
    [UxmlAttribute("Radius")]
    public int radius
    {
        get {
            return m_radius;
        }
        set
        {
            m_radius = value;
            MarkDirtyRepaint();
        }
    }
    private int m_offset = 3;
    [UxmlAttribute("Offset")]
    public int offset
    {
        get {
            return m_offset;
        }
        set
        {
            m_offset = value;
            SetTotalAngle();
            MarkDirtyRepaint();
        }
    }
    private int m_StartAngle = -60;
    [UxmlAttribute("StartAngle")]
    public int startAngle
    {
        get {
            return m_StartAngle;
        }
        set
        {
            m_StartAngle = value;
            SetTotalAngle();
            MarkDirtyRepaint();
        }
    }
    private int m_EndAngle = 60;
    [UxmlAttribute("EndAngle")]
    public int endAngle
    {
        get {
            return m_EndAngle;
        }
        set
        {
            m_EndAngle = value;
            SetTotalAngle();
            MarkDirtyRepaint();
        }
    }

    private ArcDirection m_direction = ArcDirection.Clockwise;
    [UxmlAttribute("Direction")]
    public ArcDirection direction
    {
        get {
            return m_direction;
        }
        set
        {
            m_direction = value;
            MarkDirtyRepaint();
        }
    }
    public CircleProgress()
    {
        // AddToClassList("w-full");
        // AddToClassList("h-full");
        // m_Wrapper = new VisualElement {name = IDNames.InfoBoxRowWrapper};
        // // m_Wrapper.usageHints = UsageHints.GroupTransform & UsageHints.DynamicColor & UsageHints.DynamicTransform;
        // m_Wrapper.pickingMode = PickingMode.Ignore;

        // m_Wrapper.style.flexDirection = FlexDirection.Row;
        // m_Wrapper.style.position = Position.Relative;
        // m_Wrapper.style.marginTop = new StyleLength(2);
        // // m_Wrapper.style.paddingLeft = new StyleLength(25);
        // // m_Wrapper.style.paddingRight = new StyleLength(25);
        // m_Wrapper.pickingMode = PickingMode.Ignore;
        // Add(m_Wrapper);

        // UpdateStyles();
        generateVisualContent += GenerateVisualContent;
    }

    // public void OnEnable()
    // {
    //     generateVisualContent += GenerateVisualContent; 
    // }

    private void GenerateVisualContent(MeshGenerationContext mgc)
    {
        var center = new Vector2(0, 0); // -125

        var painter = mgc.painter2D;

        painter.strokeColor = colorBg; // new Color(0, 0, 0, 0.2f);
        painter.lineWidth = width + (offset * 2);
        painter.lineCap = lineCap;
        painter.BeginPath();
        painter.Arc(center, radius, startAngle, endAngle, direction);
        painter.Stroke();
        painter.ClosePath();


        painter.strokeColor = color; // new Color(83f/255f, 255f/255f, 0, 1);
        painter.lineWidth = width;
        painter.lineCap = lineCap;
        painter.BeginPath();
        painter.Arc(center, radius, startAngle + (offset * (direction == ArcDirection.Clockwise ? 1 : -1)), m_current_value, direction);
        painter.Stroke();
        painter.ClosePath();

        // int segments = 10;       // Кол-во штрихов
        // float stepAngle = totalAngle / (segments * 2);
        // for (int i = 0; i < segments; i++)
        // {
        //     float currentStart = startAngle - i * stepAngle * 2;
        //     float currentEnd = currentStart + stepAngle;

        //     // if ((currentStart - stepAngle) <= (value * totalAngle))
        //     // {
        //     //    currentEnd = value * totalAngle;
        //     // } else
        //     // {
        //     //     currentEnd = currentStart - stepAngle;
        //     // }
            
        //     painter.strokeColor = color;
        //     painter.lineWidth = width;
        //     painter.lineCap = LineCap.Round;
        //     // painter.lineJoin = LineJoin.Miter;
            
        //     // Рисуем штрих
        //     painter.BeginPath();
        //     // Метод Arc() создает дугу, ArcTo() - между двумя точками
        //     painter.Arc(center, radius, currentStart, currentEnd, direction);
        //     painter.Stroke();
        //     painter.ClosePath();
        // }


        // float _startAngle = startAngle - 10;
        // // float totalAngle = Mathf.Abs(startAngle - endAngle);
        // int segments = 10;       // Кол-во штрихов
        // for (int i = 0; i < segments; i++)
        // {
        //     float stepAngle = totalAngle / (segments * 2);
        //     float currentStart = _startAngle - i * stepAngle * 2;
            
        //     painter.strokeColor = color;
        //     painter.lineWidth = width;
        //     painter.lineCap = LineCap.Round;
        //     // painter.lineJoin = LineJoin.Miter;
            
        //     // Рисуем штрих
        //     painter.BeginPath();
        //     // Метод Arc() создает дугу, ArcTo() - между двумя точками
        //     painter.Arc(center, radius, currentStart, currentStart + stepAngle);
        //     painter.Stroke();
        //     painter.ClosePath();
        // }
    }

    public void SetValue(float _value)
    {
        value = _value;
    }

    public void SetColor(Color _color)
    {
        color = _color;
    }
    public void UpdateStyles()
    {
        // Color color = Color.black;
        // color.a = 0.2f;
        // m_Wrapper.style.backgroundColor = new StyleColor(_gameManager ? _gameManager.Theme.colorBgInfoRow : color);
        // infoElLabel1.style.color = new StyleColor(_gameManager ? _gameManager.Theme.colorTextInfoRow : Color.white);
        // infoElLabel2.style.color = new StyleColor(_gameManager ? _gameManager.Theme.colorTextInfoRow : Color.white);

        // if (_gameManager)
        // {
        //     ico1.style.backgroundImage = new StyleBackground(_gameManager.Settings.icoDefault);
        // }
        // if (_gameManager)
        // {
        //     ico2.style.backgroundImage = new StyleBackground(_gameManager.Settings.icoDefault);
        // }
        // m_ProgressTime.style.backgroundColor = new StyleColor(_gameManager ? _gameManager.Theme.colorAccent : Color.blue);
    }
}
