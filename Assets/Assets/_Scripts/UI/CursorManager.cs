using UnityEngine;

/// <summary>
/// Manages cursor visibility when inventory opens/closes
/// </summary>
public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("Custom cursor sprite (leave null for system default)")]
    [SerializeField] private Texture2D cursorSprite;
    
    [Tooltip("Cursor hotspot offset")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    private bool defaultCursorVisible = false;
    
    private void Awake()
    {
        // Store initial cursor state
        defaultCursorVisible = Cursor.visible;
    }
    
    /// <summary>
    /// Show cursor (called when inventory opens)
    /// </summary>
    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        if (cursorSprite != null)
        {
            Cursor.SetCursor(cursorSprite, cursorHotspot, CursorMode.Auto);
        }
    }
    
    /// <summary>
    /// Hide cursor (called when inventory closes)
    /// </summary>
    public void HideCursor()
    {
        Cursor.visible = defaultCursorVisible;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Reset to default cursor
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}