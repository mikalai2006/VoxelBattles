#if UNITY_EDITOR
using Mikalai2006.VoxelBase;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public class WFCToolsCell : MonoBehaviour
    {
        public Vector3 position;
        public int height;
        public bool isCollapsed;
        public GPUInstanceEnabler gPUInstanceEnabler;
        public Color defaultColor;
        public Color hoverColor;
        public WFCToolsCellQuad Left;
        public WFCToolsCellQuad Right;
        public WFCToolsCellQuad Forward;
        public WFCToolsCellQuad Back;
        public WFCToolsCellQuad Top;
        public WFCToolsCellQuad Bottom;
        public WFCBuilder wFCManager;

        void Awake()
        {
            gPUInstanceEnabler = gameObject.GetComponent<GPUInstanceEnabler>();
            gPUInstanceEnabler.SetColor(defaultColor);
            Left.Init(this);
            Right.Init(this);
            Forward.Init(this);
            Back.Init(this);
            Top.Init(this);
            Bottom.Init(this);
        }

        void Start()
        {
            position = transform.localPosition;
        }

        // public void OnPointerEnter(PointerEventData eventData)
        // {
        //     gPUInstanceEnabler.SetColor(hoverColor);
        // }

        // public void OnPointerExit(PointerEventData eventData)
        // {
        //     gPUInstanceEnabler.SetColor(defaultColor);
        // }


        // public void OnPointerDown(PointerEventData eventData)
        // {
        //     Debug.Log($"OnPointerDown {position}, {eventData.button}");
            
        //     if (eventData.button == PointerEventData.InputButton.Left)
        //     {
        //         Debug.Log($"Pointer left");
        //     } else if (eventData.button == PointerEventData.InputButton.Right)
        //     {
        //         Debug.Log($"Pointer right");
        //     }
        // }

        public void CreateCell(DirectionCreateCell dir)
        {
            Vector3 newPosition = Vector3.zero;
            switch (dir)
            {
                case DirectionCreateCell.Left:
                    newPosition = position + new Vector3(-1,0,0);
                break;
                case DirectionCreateCell.Right:
                    newPosition = position + new Vector3(1,0,0);
                break;
                case DirectionCreateCell.Forward:
                    newPosition = position + new Vector3(0,0,-1);
                break;
                case DirectionCreateCell.Back:
                    newPosition = position + new Vector3(0,0,1);
                break;
                case DirectionCreateCell.Top:
                    newPosition = position + new Vector3(0,1,0);
                break;
                case DirectionCreateCell.Bottom:
                    newPosition = position + new Vector3(0,-1,0);
                break;
            }
            // Debug.Log($"{name}: CreateCell {dir}, {newPosition}");
            wFCManager.CreateCellTools(newPosition);
        }

        public void Init(WFCBuilder _wFCManager)
        {
            wFCManager = _wFCManager;
        }
    }
}
#endif