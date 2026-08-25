using UnityEngine;

public class FrameRate : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = GameManager.Instance.Settings.DebugSettings.FPS;// GameManager.Instance.Settings.DebugSettings.mode == AppMode.Mobile ? GameManager.Instance.Settings.playerOptions.FPS : 1000;
    }
}
