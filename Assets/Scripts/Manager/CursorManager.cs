using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    public InputAction pressEscape; // Assign this in the Inspector
    [SerializeField] private Texture2D cursorTextureDefault;
    [SerializeField] private Texture2D cursorTextureFill;
    [SerializeField] private Vector2 clickPosition = Vector2.zero;

    void Start()
    {
        Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);

        // Блокируем курсор в центре экрана и скрываем его
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnEnable()
    {
        pressEscape.Enable();
        pressEscape.performed += OnJumpPerformed; // Subscribe to the performed event
    }

    void OnDisable()
    {
        pressEscape.Disable();
        pressEscape.performed -= OnJumpPerformed; // Unsubscribe
    }

    public void SetMode(ModeOfCursor modeOfCursor)
    {
        switch (modeOfCursor)
        {
            case ModeOfCursor.Default:
            Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);
            break;
            case ModeOfCursor.Fill:
            Cursor.SetCursor(cursorTextureFill, clickPosition, CursorMode.Auto);
            break;
            default:
            Cursor.SetCursor(cursorTextureDefault, clickPosition, CursorMode.Auto);
            break;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    // void Update()
    // {
    //     // Для выхода из блокировки можно использовать, например, клавишу Escape
    //     if (Keyboard.current.escapeKey.isPressed)
    //     {
    //         Cursor.lockState = CursorLockMode.None;
    //     }
    // }
}

[System.Serializable]
public enum ModeOfCursor
{
    Default = 1,
    Fill = 2
}