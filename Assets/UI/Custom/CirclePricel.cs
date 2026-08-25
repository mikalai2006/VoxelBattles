using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Пользовательский визуальный элемент, отображающий прогресс
/// </summary>
[UxmlElement]
public partial class CirclePricel : VisualElement, IStyles
{
    GameManager _gameManager => GameManager.Instance;
    public static class IDNames
    {
        public static string InfoBoxRowWrapper = "ProgressWrapper";
        
    }

    public Vector2[] m_disableRange;
    [UxmlAttribute("DisableRange")]
    public Vector2[] disableRange
    {
        get {
            return m_disableRange;
        }
        set
        {
            m_disableRange = value;
            MarkDirtyRepaint();
        }
    }

    public float m_maxAngle = 360;
    [UxmlAttribute("MaxAngle")]
    public float maxAngle
    {
        get {
            return m_maxAngle;
        }
        set
        {
            m_maxAngle = value;
            MarkDirtyRepaint();
        }
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


    [UxmlAttribute("Color")]
    public Color color;

    private float m_margin = 0;
    [UxmlAttribute("Margin")]
    public float margin
    {
        get {
            return m_margin;
        }
        set
        {
            m_margin = value;
            MarkDirtyRepaint();
        }
    }
    private float m_width = 3;
    [UxmlAttribute("LineWidth")]
    public float lineWidth
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
    private int m_countLines = 30;
    [UxmlAttribute("CountLines")]
    public int CountLines
    {
        get {
            return m_countLines;
        }
        set
        {
            m_countLines = value;
            MarkDirtyRepaint();
        }
    }

    private float m_length = 10;
    [UxmlAttribute("lineLength")]
    public float lineLength
    {
        get {
            return m_length;
        }
        set
        {
            m_length = value;
            MarkDirtyRepaint();
        }
    }
    private float m_radius = 100;
    [UxmlAttribute("Radius")]
    public float radius
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
    [UxmlAttribute("ColorEveryLong")]
    public Color colorEveryLong;
    private float m_everyLongLength = 5;
    [UxmlAttribute("EveryLongLength")]
    public float everyLongLength
    {
        get {
            return m_everyLongLength;
        }
        set
        {
            m_everyLongLength = value;
            MarkDirtyRepaint();
        }
    }
    private float m_longLineLength = 20f;
    [UxmlAttribute("LongLineLength")]
    public float longLength
    {
        get {
            return m_longLineLength;
        }
        set
        {
            m_longLineLength = value;
            MarkDirtyRepaint();
        }
    }
    private float m_longLinehWidth = 1f;
    [UxmlAttribute("LongLineWidth")]
    public float longLineWidth
    {
        get {
            return m_longLinehWidth;
        }
        set
        {
            m_longLinehWidth = value;
            MarkDirtyRepaint();
        }
    }
    private float m_longLine_margin = 0;
    [UxmlAttribute("longLineMargin")]
    public float longLineMargin
    {
        get {
            return m_longLine_margin;
        }
        set
        {
            m_longLine_margin = value;
            MarkDirtyRepaint();
        }
    }

    public CirclePricel()
    {
        style.alignSelf = Align.Auto;

        generateVisualContent += GenerateVisualContent;
    }

    // public void OnEnable()
    // {
    //     generateVisualContent += GenerateVisualContent; 
    // }

    private void GenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter2D = mgc.painter2D;
        Vector2 center = new Vector2(0, 0); //-125
        
        // painter2D.lineWidth = lineWidth;
        // painter2D.strokeColor = color;
        // painter2D.lineCap = lineCap;
        
        // painter2D.BeginPath();
        // painter2D.MoveTo(new Vector2(0, radius + 10));
        // painter2D.LineTo(new Vector2(0, -radius - 10));
        // painter2D.Stroke();
        // painter2D.ClosePath();

        float totalAngle = 0;
        for (int i = 0; i < CountLines + 1; i++)
        {
            float angle = i * (m_maxAngle / CountLines) * Mathf.Deg2Rad; // 360 / 60 = 6 градусов
            totalAngle += m_maxAngle / CountLines;
            
            // Определяем длину черточки (каждая 5-я длиннее)
            // ((totalAngle > 0 && totalAngle <= 95) || (totalAngle > 180 && totalAngle <= 275))
            float _lineLength = InRange(totalAngle, disableRange) ? ((i % m_everyLongLength == 0) ? m_longLineLength : lineLength) : 0.5f;
            float _lineWidth = (i % m_everyLongLength == 0) ? m_longLinehWidth : lineWidth;
            float _margin = (i % m_everyLongLength == 0) ? longLineMargin : margin;
            Color _color = (i % m_everyLongLength == 0) ? colorEveryLong : color;

            // Расчет начальной и конечной точек
            Vector2 startPoint = new Vector2(
                center.x + Mathf.Cos(angle) * (radius - _margin - _lineLength),
                center.y + Mathf.Sin(angle) * (radius - _margin - _lineLength)
            );
            Vector2 endPoint = new Vector2(
                center.x + Mathf.Cos(angle) * (radius - _margin),
                center.y + Mathf.Sin(angle) * (radius - _margin)
            );

            // Рисуем линию
            painter2D.lineWidth = _lineWidth;
            painter2D.strokeColor = _color;
            painter2D.lineCap = lineCap;
            painter2D.BeginPath();
            painter2D.MoveTo(startPoint);
            painter2D.LineTo(endPoint);
            painter2D.Stroke();
            painter2D.ClosePath();
        }

        // var center = new Vector2(0, 0);

        // var painter = mgc.painter2D;

        // painter.strokeColor = color;
        // painter.lineWidth = width;
        // painter.lineCap = LineCap.Round;
        // painter.BeginPath();
        // painter.Arc(center, radius, 0, 360, ArcDirection.Clockwise);
        // painter.Stroke();
        // painter.ClosePath();

        // // int segments = 10;       // Кол-во штрихов
        // // float stepAngle = totalAngle / (segments * 2);
        // // for (int i = 0; i < segments; i++)
        // // {
        // //     float currentStart = startAngle - i * stepAngle * 2;
        // //     float currentEnd = currentStart + stepAngle;

        // //     // if ((currentStart - stepAngle) <= (value * totalAngle))
        // //     // {
        // //     //    currentEnd = value * totalAngle;
        // //     // } else
        // //     // {
        // //     //     currentEnd = currentStart - stepAngle;
        // //     // }
            
        // //     painter.strokeColor = color;
        // //     painter.lineWidth = width;
        // //     painter.lineCap = LineCap.Round;
        // //     // painter.lineJoin = LineJoin.Miter;
            
        // //     // Рисуем штрих
        // //     painter.BeginPath();
        // //     // Метод Arc() создает дугу, ArcTo() - между двумя точками
        // //     painter.Arc(center, radius, currentStart, currentEnd, direction);
        // //     painter.Stroke();
        // //     painter.ClosePath();
        // // }


        // // float _startAngle = startAngle - 10;
        // // // float totalAngle = Mathf.Abs(startAngle - endAngle);
        // // int segments = 10;       // Кол-во штрихов
        // // for (int i = 0; i < segments; i++)
        // // {
        // //     float stepAngle = totalAngle / (segments * 2);
        // //     float currentStart = _startAngle - i * stepAngle * 2;
            
        // //     painter.strokeColor = color;
        // //     painter.lineWidth = width;
        // //     painter.lineCap = LineCap.Round;
        // //     // painter.lineJoin = LineJoin.Miter;
            
        // //     // Рисуем штрих
        // //     painter.BeginPath();
        // //     // Метод Arc() создает дугу, ArcTo() - между двумя точками
        // //     painter.Arc(center, radius, currentStart, currentStart + stepAngle);
        // //     painter.Stroke();
        // //     painter.ClosePath();
        // // }
    }

    public void SetRadius(float _value)
    {
        radius = _value;
    }

    public void UpdateStyles()
    {
    }

    public bool InRange(float value, Vector2[] list)
    {
        bool[] _result = new bool[list.Length];

        for (int i = 0; i < list.Length; i++)
        {
            if (value >= list[i].x && value <= list[i].y)
            {
                _result[i] = true;
            } else
            {
                _result[i] = false;
            }
        }

        bool hasTrue = false;
        for (int i = 0; i < _result.Length; i++) {
            if (_result[i]) {
                hasTrue = true;
                break; // Выходим, как только нашли первый true
            }
        }

        return hasTrue;
    }
}
