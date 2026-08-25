#if UNITY_EDITOR
using Mikalai2006.VoxelBase;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mikalai2006.VoxelMap {
    public class WFCToolsCellQuad : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public bool isCollapsed;
        public GPUInstanceEnabler gPUInstanceEnabler;
        public Color defaultColor;
        public Color hoverColor;
        WFCToolsCell _cell;
        public DirectionCreateCell directionCreateCell;

        void Awake()
        {
            gPUInstanceEnabler = gameObject.GetComponent<GPUInstanceEnabler>();
        }

        void Start()
        {
            gPUInstanceEnabler.SetColor(defaultColor);
        }

        public void Init(WFCToolsCell cell)
        {
            _cell = cell;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            gPUInstanceEnabler.SetColor(hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            gPUInstanceEnabler.SetColor(defaultColor);
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            // Debug.Log($"{name}: OnPointerDown {eventData.button}");
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Debug.Log($"{name}: Pointer left");
                _cell.CreateCell(directionCreateCell);
            } else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Debug.Log($"{name}: Pointer right");
                _cell.wFCManager.RemoveCellTools(_cell);
            }
        }
    }

    [System.Serializable]
    public enum DirectionCreateCell
    {
        Left = 1,
        Right = 2,
        Forward = 3,
        Back = 4,
        Top = 5,
        Bottom = 6
    }
}
#endif