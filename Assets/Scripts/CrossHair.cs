using System;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public VehicleAssembler baseMachine;
    public Parts[] parts;
    private RectTransform rectTransform;

    [SerializeField] float currentSpread = 0;
    float t = 0;
    float curSpread;
    [SerializeField] private float speedSpread;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.position = new Vector3(rectTransform.position.x, Screen.height * 0.7f, rectTransform.position.z);
    }

    //void Update()
    //{
    //    if (baseMachine != null) {
    //        // float diffAngle = Mathf.Abs(Mathf.Abs(baseMachine.Towers[0].Data.angleTower) - Mathf.Abs(baseMachine.Towers[0].Data.currentAngleTower));
    //        var rotationA = Quaternion.Euler(0, baseMachine.Towers[0].Data.angleTower, 0); // Identity rotation
    //        var rotationB = Quaternion.Euler(0, baseMachine.Towers[0].Data.currentAngleTower, 0);
    //        float diffAngle = Quaternion.Angle(rotationA, rotationB);
    //        if (diffAngle > 1)
    //        {
    //            currentSpread = diffAngle * 20;
    //        }
    //        else
    //        {
    //            currentSpread = 20;
    //        }

    //        CrossUpdate();
    //    }
    //}

    public void OnSetTarget(VehicleAssembler _baseMachine)
    {
        baseMachine = _baseMachine;
    }

    public void CrossUpdate()
    {
        t = Time.deltaTime * speedSpread;
        curSpread = Mathf.Lerp(curSpread, currentSpread, t);

        for (int i = 0; i < parts.Length; i++)
        {
            Parts p = parts[i];
            p.trans.anchoredPosition = p.pos * curSpread;
        }
    }

    [Serializable]
    public class Parts
    {
        public RectTransform trans;
        public Vector2 pos;
    }
}
