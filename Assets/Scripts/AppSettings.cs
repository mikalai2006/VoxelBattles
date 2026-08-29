using UnityEngine;

public class AppSettings : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 45;
    }
}
