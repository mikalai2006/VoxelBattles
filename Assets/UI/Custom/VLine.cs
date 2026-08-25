using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitLibrary {
    /// <summary>
    /// Пользовательский визуальный элемент, отображающий прогресс
    /// </summary>
    [UxmlElement]
    public partial class VLine : VisualElement
    {
        public Vector2 _pointStart;
        [UxmlAttribute("PointStart")]
        public Vector2 pointStart
        {
            get {
                return _pointStart;
            }
            set
            {
                _pointStart = value;
                MarkDirtyRepaint();
            }
        }
        public Vector2 _pointEnd;
        [UxmlAttribute("PointEnd")]
        public Vector2 pointEnd
        {
            get {
                return _pointEnd;
            }
            set
            {
                _pointEnd = value;
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
public float Radius { get; set; } = 100f;
    public float Angle { get; set; } = 90f; // Угол сектора
    public float Perspective { get; set; } = 0.1f; // 0-1, сжатие верхней части

        public VLine()
        {
            style.alignSelf = Align.Auto;

            generateVisualContent += GenerateVisualContent;
        }

        private void GenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter2D = mgc.painter2D;
            painter2D.lineWidth = lineWidth;
            painter2D.strokeColor = color;
            painter2D.lineCap = lineCap;
            
            painter2D.BeginPath();
            painter2D.MoveTo(pointStart);
            painter2D.LineTo(pointEnd);
            painter2D.Stroke();
            painter2D.ClosePath();

            // painter2D.BeginPath();
            // painter2D.MoveTo(pointStart); 
            // painter2D.Arc(pointStart, Vector2.Distance(pointEnd, pointStart), 260f, 280f, ArcDirection.Clockwise); 
            // painter2D.fillColor = Color.red;
            // painter2D.Fill();
            // painter2D.Stroke();
            // painter2D.ClosePath();
        }
    }
}
